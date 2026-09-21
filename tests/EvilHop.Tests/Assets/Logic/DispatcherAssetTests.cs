using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class DispatcherAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Dispatcher;
        header.Debug = debug;

        return (header, debug);
    }

    private static DispatcherAsset Read(byte[] data)
    {
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), Endianness.Big);
        return (DispatcherAsset)AssetCodecs.Read(reader, header, debug, N100FSerializer.DefaultProfile);
    }

    private static byte[] Write(DispatcherAsset asset)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true))
            AssetCodecs.Write(asset, writer, N100FSerializer.DefaultProfile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x16,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    [Fact]
    public void Read_Dispatcher_ProducesDispatcherAsset() =>
        Assert.IsType<DispatcherAsset>(Read(Prefix(0)));

    [Fact]
    public void Read_Dispatcher_ReadsLinksAtTheDocumentedOffset()
    {
        byte[] data =
        [
            .. Prefix(2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];

        var asset = Read(data);

        Assert.Equal(2, asset.Links.Count);
        Assert.Equal(1, asset.Links[0].SourceEvent);
        Assert.Equal(2, asset.Links[0].DestinationEvent);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Links[0].DestinationAssetId);
        Assert.Equal(3, asset.Links[1].SourceEvent);
    }

    [Fact]
    public void Read_Dispatcher_LinkCountKeepsDerivingAfterLinksAreMutated()
    {
        // Regression test for IPhysicalBaseAsset.LinkCount: a codec that parses links into Links
        // must hand LinkCount back to deriving rather than leaving it pinned at whatever BaseAssetPrefix
        // read from the fixed header, or a caller adding/removing links afterward would silently
        // serialize a stale count.
        byte[] data = [.. Prefix(1), .. LinkBytes(0, 0, 0)];

        var asset = Read(data);
        Assert.Equal(1, asset.Physical.LinkCount);

        asset.Links.Add(new Link());

        Assert.Equal(2, asset.Physical.LinkCount);
    }

    [Fact]
    public void Read_ThenWrite_DispatcherWithNoLinks_ReproducesInputBytes()
    {
        byte[] data = Prefix(0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_DispatcherWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_DispatcherWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(0), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }
}

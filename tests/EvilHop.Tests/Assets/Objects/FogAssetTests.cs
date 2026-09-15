using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class FogAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Fog;
        header.Debug = debug;

        return (header, debug);
    }

    private static FogAsset Read(byte[] data)
    {
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), Endianness.Big);
        return (FogAsset)AssetCodecs.Read(reader, header, debug, N100FSerializer.DefaultProfile);
    }

    private static byte[] Write(FogAsset asset)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true))
            AssetCodecs.Write(asset, writer, N100FSerializer.DefaultProfile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x24,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] Body(byte fogType = 0) =>
    [
        0x68, 0x59, 0x59, 0xFF, // BackgroundColor
        0x68, 0x59, 0xA6, 0xFF, // Color
        0x3F, 0x80, 0x00, 0x00, // Density = 1.0
        0x42, 0x48, 0x00, 0x00, // StartDistance = 50.0
        0x43, 0x16, 0x00, 0x00, // StopDistance = 150.0
        0x00, 0x00, 0x00, 0x00, // TransitionTime = 0.0
        fogType,
        0x00, 0x00, 0x00,       // padding
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
    public void Read_Fog_ProducesFogAsset() =>
        Assert.IsType<FogAsset>(Read([.. Prefix(0), .. Body()]));

    [Fact]
    public void Read_Fog_PopulatesFields()
    {
        byte[] data = [.. Prefix(0), .. Body()];

        var asset = Read(data);

        Assert.Equal(new Rgba(0x68 / 255f, 0x59 / 255f, 0x59 / 255f, 0xFF / 255f), asset.BackgroundColor);
        Assert.Equal(new Rgba(0x68 / 255f, 0x59 / 255f, 0xA6 / 255f, 0xFF / 255f), asset.Color);
        Assert.Equal(1.0f, asset.Density);
        Assert.Equal(50.0f, asset.StartDistance);
        Assert.Equal(150.0f, asset.StopDistance);
        Assert.Equal(0.0f, asset.TransitionTime);
    }

    [Fact]
    public void Read_Fog_ReadsLinksAtTheDocumentedOffset()
    {
        byte[] data =
        [
            .. Prefix(2),
            .. Body(),
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
    public void Read_Fog_LinkCountKeepsDerivingAfterLinksAreMutated()
    {
        byte[] data =
        [
            .. Prefix(1),
            .. Body(),
            .. LinkBytes(0, 0, 0),
        ];

        var asset = Read(data);
        Assert.Equal(1, asset.Physical.LinkCount);

        asset.Links.Add(new Link());

        Assert.Equal(2, asset.Physical.LinkCount);
    }

    [Fact]
    public void Read_ThenWrite_FogWithNoLinks_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(0), .. Body(fogType: 0)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_FogWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(2),
            .. Body(),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_FogWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(0), .. Body(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }
}

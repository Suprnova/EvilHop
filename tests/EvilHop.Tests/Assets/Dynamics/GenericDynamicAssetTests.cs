using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class GenericDynamicAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Dynamic;
        header.Debug = debug;

        return (header, debug);
    }

    private static DynamicAsset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), Endianness.Big);
        return (DynamicAsset)AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(DynamicAsset asset, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount, uint kind = (uint)DynamicKind.UIText, short version = 2) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x00,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
        (byte)(kind >> 24), (byte)(kind >> 16), (byte)(kind >> 8), (byte)kind,
        (byte)(version >> 8), (byte)version,
        0x00, 0x00,             // Handle
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

    private static readonly byte[] Body = [0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02];

    [Fact]
    public void Read_UntypedKind_ProducesGenericDynamicAsset() =>
        Assert.IsType<GenericDynamicAsset>(Read([.. Prefix(0), .. Body]));

    [Theory]
    [InlineData((uint)DynamicKind.UIBox)]
    [InlineData((uint)DynamicKind.UIText)]
    [InlineData(0x0BADF00Du)]
    public void Read_Dynamic_TakesKindFromThePrefix(uint kind)
    {
        var asset = Read([.. Prefix(0, kind), .. Body]);

        Assert.Equal((DynamicKind)kind, asset.Kind);
        Assert.Equal((DynamicKind)kind, asset.Physical.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Read_Dynamic_PopulatesVersion(short version) =>
        Assert.Equal(version, Read([.. Prefix(0, version: version), .. Body]).Physical.Version);

    [Fact]
    public void Read_Dynamic_ParsesLinksFromTheFinalBytes()
    {
        byte[] data = [.. Prefix(2), .. Body, .. LinkBytes(1, 2, 0xAABBCCDD), .. LinkBytes(3, 4, 0x11223344)];

        var asset = Read(data);

        Assert.Equal(2, asset.Links.Count);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Links[0].DestinationAssetId);
        Assert.Equal(3, asset.Links[1].SourceEvent);
    }

    [Fact]
    public void Read_Dynamic_PreservesTheBodyBeforeTheLinksAsTheUnparsedTail()
    {
        byte[] data = [.. Prefix(1), .. Body, .. LinkBytes(1, 2, 0xAABBCCDD)];

        var asset = Read(data);

        Assert.Equal(Body, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Read_Dynamic_LinkCountKeepsDerivingAfterLinksAreMutated()
    {
        var asset = Read([.. Prefix(1), .. Body, .. LinkBytes(1, 2, 0xAABBCCDD)]);

        asset.Links.Add(new Link());

        Assert.Equal(2, asset.Physical.LinkCount);
    }

    [Fact]
    public void Read_LinkCountLargerThanTheData_LeavesLinksUnparsed()
    {
        var asset = Read([.. Prefix(3), .. Body]);

        Assert.Empty(asset.Links);
        Assert.Equal(3, asset.Physical.LinkCount);
        Assert.Equal(Body, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Read_UnsupportedGame_DegradesWithoutParsingLinks()
    {
        byte[] data = [.. Prefix(1), .. Body, .. LinkBytes(1, 2, 0xAABBCCDD)];

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsType<GenericDynamicAsset>(asset);
        Assert.Empty(asset.Links);
        Assert.Equal(1, asset.Physical.LinkCount);
    }

    [Fact]
    public void Read_ThenWrite_DynamicWithLinks_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(2), .. Body, .. LinkBytes(1, 2, 0xAABBCCDD), .. LinkBytes(3, 4, 0x11223344)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_LinkCountLargerThanTheData_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(3), .. Body];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_UnsupportedGame_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(1), .. Body, .. LinkBytes(1, 2, 0xAABBCCDD)];

        Assert.Equal(data, Write(Read(data, N100FSerializer.DefaultProfile), N100FSerializer.DefaultProfile));
    }

    [Fact]
    public void Write_AddedLink_IsWrittenAfterTheBody()
    {
        var asset = Read([.. Prefix(0), .. Body]);

        asset.Links.Add(new Link { SourceEvent = 1, DestinationEvent = 2, DestinationAssetId = new AssetId(0xAABBCCDD) });

        Assert.Equal([.. Prefix(1), .. Body, .. LinkBytes(1, 2, 0xAABBCCDD)], Write(asset));
    }

    [Fact]
    public void Write_OverriddenPhysicalKind_WritesTheOverride()
    {
        var asset = new GenericDynamicAsset(DynamicKind.UIText) { Id = new AssetId(0x1234) };

        asset.Physical.Kind = DynamicKind.UIBox;

        Assert.Equal(DynamicKind.UIText, asset.Kind);
        Assert.Equal(Prefix(0, (uint)DynamicKind.UIBox, 0)[8..12], Write(asset)[8..12]);
    }
}

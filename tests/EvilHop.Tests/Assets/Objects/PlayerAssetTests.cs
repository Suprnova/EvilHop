using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class PlayerAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Player;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile profile)
    {
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile profile)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x03,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix(bool hasPadding) =>
    [
        0x01, 0x00, 0x00, 0x00,   // EntityFlags, Subtype, PFlags, CollisionFlags
        .. hasPadding ? new byte[4] : [],
        0x00, 0x00, 0x00, 0x00,   // SurfaceId
        .. new byte[36],          // Angle/Position/Scale
        .. new byte[16],          // ColorMultiplier
        0x00, 0x00, 0x00, 0x00,   // SeeThroughSpeed
        0x00, 0x00, 0x00, 0x00,   // ModelId
        0x00, 0x00, 0x00, 0x00,   // AnimListId
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

    private static byte[] LightKitId(uint value) =>
    [
        (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value,
    ];

    private static byte[] BfbbData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: true),
    ];

    private static byte[] N100FData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: false),
    ];

    [Fact]
    public void Read_Player_ProducesPlayerAsset() =>
        Assert.IsType<PlayerAsset>(Read([.. BfbbData(), .. LightKitId(0x4E24E022)], BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_Player_UnderBfbb_PopulatesLightKitId()
    {
        byte[] data = [.. BfbbData(), .. LightKitId(0x4E24E022)];

        var asset = (PlayerAsset)Read(data, BFBBSerializer.DefaultProfile);

        Assert.Equal(new AssetId(0x4E24E022), asset.LightKitId);
    }

    [Fact]
    public void Read_Player_UnderN100F_HasNoLightKitId()
    {
        byte[] data = N100FData();

        var asset = (PlayerAsset)Read(data, N100FSerializer.DefaultProfile);

        Assert.Equal(default, asset.LightKitId);
    }

    [Fact]
    public void Read_Player_ReadsLinksBeforeLightKitId()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
            .. LightKitId(0x99999999),
        ];

        var asset = (PlayerAsset)Read(data, BFBBSerializer.DefaultProfile);

        Assert.Equal(2, asset.Links.Count);
        Assert.Equal(1, asset.Links[0].SourceEvent);
        Assert.Equal(3, asset.Links[1].SourceEvent);
        Assert.Equal(new AssetId(0x99999999), asset.LightKitId);
    }

    [Fact]
    public void Read_ThenWrite_PlayerUnderBfbb_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), .. LightKitId(0x4E24E022)];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_PlayerUnderN100F_ReproducesInputBytes()
    {
        byte[] data = N100FData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_PlayerWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
            .. LightKitId(0x4E24E022),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_PlayerWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), .. LightKitId(0x4E24E022), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }
}

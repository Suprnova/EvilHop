using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class EnvironmentAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Environment;
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
        0x05,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EnvFields(uint bspId, uint startCameraId, uint climateFlags, float climateMin, float climateMax,
        uint bspLightKit, uint objectLightKit, uint envFlags, uint bspCollisionId, uint bspFxId, uint bspCameraId,
        uint bspMapperId, uint bspMapperCollisionId, uint bspMapperFxId) =>
    [
        .. BitConverter.GetBytes(bspId).Reverse(),
        .. BitConverter.GetBytes(startCameraId).Reverse(),
        .. BitConverter.GetBytes(climateFlags).Reverse(),
        .. BitConverter.GetBytes(climateMin).Reverse(),
        .. BitConverter.GetBytes(climateMax).Reverse(),
        .. BitConverter.GetBytes(bspLightKit).Reverse(),
        .. BitConverter.GetBytes(objectLightKit).Reverse(),
        .. BitConverter.GetBytes(envFlags).Reverse(),
        .. BitConverter.GetBytes(bspCollisionId).Reverse(),
        .. BitConverter.GetBytes(bspFxId).Reverse(),
        .. BitConverter.GetBytes(bspCameraId).Reverse(),
        .. BitConverter.GetBytes(bspMapperId).Reverse(),
        .. BitConverter.GetBytes(bspMapperCollisionId).Reverse(),
        .. BitConverter.GetBytes(bspMapperFxId).Reverse(),
    ];

    // loldHeight is stored little-endian regardless of platform - always these 4 bytes for 10.0f.
    private static readonly byte[] LoldHeightTen = [0x00, 0x00, 0x20, 0x41];

    private static byte[] Vector3Bytes(float x, float y, float z) =>
    [
        .. BitConverter.GetBytes(x).Reverse(),
        .. BitConverter.GetBytes(y).Reverse(),
        .. BitConverter.GetBytes(z).Reverse(),
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

    private static byte[] N100FData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EnvFields(0x11111111, 0x22222222, 1, 0.25f, 0.75f, 0x33333333, 0x44444444, 0, 0x55555555, 0x66666666, 0x77777777, 0x88888888, 0x99999999, 0xAAAAAAAA),
    ];

    private static byte[] BfbbData(byte linkCount = 0) =>
    [
        .. N100FData(linkCount),
        .. LoldHeightTen,
    ];

    private static byte[] RatatouilleData(byte linkCount = 0) =>
    [
        .. BfbbData(linkCount),
        .. Vector3Bytes(-200f, -50f, -200f),
        .. Vector3Bytes(200f, 50f, 200f),
    ];

    [Fact]
    public void Read_Environment_ProducesEnvironmentAsset() =>
        Assert.IsType<EnvironmentAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_Environment_UnderRatatouille_PopulatesEveryField()
    {
        var asset = (EnvironmentAsset)Read(RatatouilleData(), RatatouilleSerializer.DefaultProfile);

        Assert.Equal(new AssetId(0x11111111), asset.BspId);
        Assert.Equal(new AssetId(0x22222222), asset.StartCameraId);
        Assert.Equal(ClimateFlags.Rain, asset.ClimateFlags);
        Assert.Equal(0.25f, asset.ClimateStrengthMin);
        Assert.Equal(0.75f, asset.ClimateStrengthMax);
        Assert.Equal(new AssetId(0x33333333), asset.BspLightKitId);
        Assert.Equal(new AssetId(0x44444444), asset.ObjectLightKitId);
        Assert.Equal(new AssetId(0x55555555), asset.BspCollisionId);
        Assert.Equal(new AssetId(0x66666666), asset.BspFxId);
        Assert.Equal(new AssetId(0x77777777), asset.BspCameraId);
        Assert.Equal(new AssetId(0x88888888), asset.BspMapperId);
        Assert.Equal(new AssetId(0x99999999), asset.BspMapperCollisionId);
        Assert.Equal(new AssetId(0xAAAAAAAA), asset.BspMapperFxId);
        Assert.Equal(10f, asset.Physical.LoldHeight);
        Assert.Equal(new System.Numerics.Vector3(-200f, -50f, -200f), asset.MinBounds);
        Assert.Equal(new System.Numerics.Vector3(200f, 50f, 200f), asset.MaxBounds);
    }

    [Fact]
    public void Read_Environment_UnderN100F_LeavesLoldHeightAndBoundsAtDefault()
    {
        var asset = (EnvironmentAsset)Read(N100FData(), N100FSerializer.DefaultProfile);

        Assert.Equal(new AssetId(0x11111111), asset.BspId);
        Assert.Equal(default, asset.Physical.LoldHeight);
        Assert.Equal(default, asset.MinBounds);
        Assert.Equal(default, asset.MaxBounds);
    }

    [Fact]
    public void Read_Environment_UnderBfbb_LeavesBoundsAtDefaultButPopulatesLoldHeight()
    {
        var asset = (EnvironmentAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(10f, asset.Physical.LoldHeight);
        Assert.Equal(default, asset.MinBounds);
        Assert.Equal(default, asset.MaxBounds);
    }

    [Fact]
    public void Read_ThenWrite_EnvironmentUnderN100F_ReproducesInputBytes()
    {
        byte[] data = N100FData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_EnvironmentUnderBfbb_ReproducesInputBytes()
    {
        byte[] data = BfbbData();
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_EnvironmentUnderRatatouille_ReproducesInputBytes()
    {
        byte[] data = RatatouilleData();
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_EnvironmentWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. RatatouilleData(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_EnvironmentWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. RatatouilleData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Theory]
    [InlineData(Platform.Xbox)]
    [InlineData(Platform.PlayStation2)]
    public void Read_ThenWrite_EnvironmentUnderLittleEndianPlatform_LoldHeightStaysLittleEndian(Platform platform)
    {
        // loldHeight is stored little-endian on disk regardless of the platform's own byte order -
        // every other field in this asset flips with the platform, but this one field never does.
        byte[] data = BfbbData();
        var profile = BFBBSerializer.DefaultProfile with { Platform = platform };

        var asset = (EnvironmentAsset)Read(data, profile);

        Assert.Equal(10f, asset.Physical.LoldHeight);
        Assert.Equal(data, Write(asset, profile));
    }
}

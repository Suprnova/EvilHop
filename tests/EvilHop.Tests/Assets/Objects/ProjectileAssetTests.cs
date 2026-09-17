using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class ProjectileAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Projectile;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x22,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] I32(int value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] Data(int effectType, uint modelId, uint animId, uint atRestModelId, uint atRestAnimId,
        int destructEnabled, float destructTime, float destructDistance, int oriented, byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. I32(effectType),
        .. U32(modelId),
        .. U32(animId),
        .. U32(atRestModelId),
        .. U32(atRestAnimId),
        .. I32(destructEnabled),
        .. F32(destructTime),
        .. F32(destructDistance),
        .. I32(oriented),
        .. new byte[24], // Reserved
    ];

    [Fact]
    public void Read_Projectile_ProducesProjectileAsset() =>
        Assert.IsType<ProjectileAsset>(Read(Data(0, 0, 0, 0, 0, 0, 0f, 0f, 0)));

    [Fact]
    public void Read_Projectile_PopulatesEveryField()
    {
        byte[] data = Data(
            effectType: 3, modelId: 0x0E056267, animId: 0x11223344, atRestModelId: 0x55667788, atRestAnimId: 0x99AABBCC,
            destructEnabled: 1, destructTime: 100.0f, destructDistance: 50.0f, oriented: 1);

        var asset = (ProjectileAsset)Read(data);

        Assert.Equal(3, asset.EffectType);
        Assert.Equal(new AssetId(0x0E056267), asset.ModelId);
        Assert.Equal(new AssetId(0x11223344), asset.AnimId);
        Assert.Equal(new AssetId(0x55667788), asset.AtRestModelId);
        Assert.Equal(new AssetId(0x99AABBCC), asset.AtRestAnimId);
        Assert.True(asset.DestructEnabled);
        Assert.Equal(100.0f, asset.DestructTime);
        Assert.Equal(50.0f, asset.DestructDistance);
        Assert.True(asset.Oriented);
    }

    [Fact]
    public void Read_ThenWrite_Projectile_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(0, 0x0E056267, 0, 0, 0, 1, 100.0f, 50.0f, 0, linkCount: 1),
            .. LinkBytes(1, 2, 0x55667788),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ProjectileWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(0, 0, 0, 0, 0, 0, 0f, 0f, 0), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_Projectile_UnderBFBB_DegradesToGenericBaseAsset()
    {
        byte[] data = Data(0, 0, 0, 0, 0, 0, 0f, 0f, 0);

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<ProjectileAsset>(asset);
    }

    [Fact]
    public void Read_ThenWrite_RealN100FExemplar_ReproducesInputBytes()
    {
        // n100f/prototype_2003-07-08/XBOX/NTSC-U/US/b0/b001.HIP, AHDR id=0x4974022F
        byte[] data =
        [
            0x2F, 0x02, 0x74, 0x49, 0x22, 0x00, 0x1D, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x0E, 0x05, 0x62, 0xE7,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0xC8, 0x42,
            0x00, 0x00, 0x48, 0x42,
            0x00, 0x00, 0x00, 0x00,
            .. new byte[24],
        ];
        var profile = N100FSerializer.DefaultProfile with { Platform = Platform.Xbox };

        var asset = (ProjectileAsset)Read(data, profile);

        Assert.Equal(new AssetId(0x4974022F), asset.Physical.BaseId);
        Assert.Equal(0x22, asset.Physical.BaseType);
        Assert.Equal(0, asset.EffectType);
        Assert.Equal(new AssetId(0xE762050E), asset.ModelId);
        Assert.True(asset.DestructEnabled);
        Assert.Equal(100.0f, asset.DestructTime);
        Assert.Equal(50.0f, asset.DestructDistance);
        Assert.False(asset.Oriented);

        Assert.Equal(data, Write(asset, profile));
    }
}

using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class ElectricArcGeneratorAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.ElectricArcGenerator;
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
        0x29,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix(bool hasPadding) =>
    [
        0x00, 0x00, 0x00, 0x00,   // EntityFlags, Subtype, PFlags, CollisionFlags
        .. hasPadding ? new byte[4] : [],
        0x00, 0x00, 0x00, 0x00,   // SurfaceId
        .. new byte[36],          // Angle/Position/Scale
        .. new byte[16],          // ColorMultiplier
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        0x11, 0x11, 0x11, 0x11,   // ModelId
        0x00, 0x00, 0x00, 0x00,   // AnimListId
    ];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] EGenFields(float srcX, float srcY, float srcZ, byte damageType, byte flags, float activeTime, uint onAnimationId) =>
    [
        .. F32(srcX), .. F32(srcY), .. F32(srcZ),
        damageType,
        flags,
        0x00, 0x00, // padding
        .. F32(activeTime),
        (byte)(onAnimationId >> 24), (byte)(onAnimationId >> 16), (byte)(onAnimationId >> 8), (byte)onAnimationId,
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

    private static byte[] BfbbData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: true),
        .. EGenFields(1.0f, 2.0f, 3.0f, damageType: 6, flags: 1, activeTime: 2.5f, onAnimationId: 0xCE7F8131),
    ];

    private static byte[] TSSMData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: false),
        .. EGenFields(1.0f, 2.0f, 3.0f, damageType: 6, flags: 1, activeTime: 2.5f, onAnimationId: 0xCE7F8131),
    ];

    [Fact]
    public void Read_ElectricArcGenerator_ProducesElectricArcGeneratorAsset() =>
        Assert.IsType<ElectricArcGeneratorAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_ElectricArcGenerator_UnderBfbb_PopulatesEveryField()
    {
        var asset = (ElectricArcGeneratorAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(new System.Numerics.Vector3(1.0f, 2.0f, 3.0f), asset.SourceOffset);
        Assert.Equal(6, asset.DamageType);
        Assert.Equal(ElectricArcGeneratorFlags.StartsOn, asset.Flags);
        Assert.Equal(2.5f, asset.ActiveTime);
        Assert.Equal(new AssetId(0xCE7F8131), asset.OnAnimationId);
        Assert.Equal(new AssetId(0x11111111), asset.Physical.ModelId);
    }

    [Fact]
    public void Read_ElectricArcGenerator_UnderTSSM_PopulatesEveryField()
    {
        var asset = (ElectricArcGeneratorAsset)Read(TSSMData(), TSSMSerializer.DefaultProfile);

        Assert.Equal(new System.Numerics.Vector3(1.0f, 2.0f, 3.0f), asset.SourceOffset);
        Assert.Equal(6, asset.DamageType);
        Assert.Equal(ElectricArcGeneratorFlags.StartsOn, asset.Flags);
        Assert.Equal(2.5f, asset.ActiveTime);
        Assert.Equal(new AssetId(0xCE7F8131), asset.OnAnimationId);
    }

    [Fact]
    public void Read_ThenWrite_ElectricArcGeneratorUnderBfbb_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_ElectricArcGeneratorUnderTSSM_ReproducesInputBytes()
    {
        byte[] data = [.. TSSMData(linkCount: 1), .. LinkBytes(1, 2, 0xAABBCCDD)];
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_ElectricArcGeneratorUnderN100F_ReproducesInputBytes()
    {
        byte[] data = [.. TSSMData(linkCount: 1), .. LinkBytes(1, 2, 0xAABBCCDD)];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_ElectricArcGeneratorWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ElectricArcGenerator_UnderIncredibles_DegradesToGenericEntityAsset()
    {
        byte[] data = BfbbData();
        var profile = IncrediblesSerializer.DefaultProfile;

        var asset = Read(data, profile);

        Assert.IsNotType<ElectricArcGeneratorAsset>(asset);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void ModelId_SetThroughIHasModel_ProjectsOntoPhysicalModelId()
    {
        var asset = new ElectricArcGeneratorAsset();

        ((IHasModel)asset).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.ModelId);
    }

    [Fact]
    public void SurfaceId_SetThroughIHasSurface_ProjectsOntoPhysicalSurfaceId()
    {
        var asset = new ElectricArcGeneratorAsset();

        ((IHasSurface)asset).SurfaceId = new AssetId(0xCAFEF00D);

        Assert.Equal(new AssetId(0xCAFEF00D), asset.Physical.SurfaceId);
    }
}

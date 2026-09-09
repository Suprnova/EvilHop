using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class DestructibleObjectAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.DestructibleObject;
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
        0x1B,                   // BaseType
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
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        0x00, 0x00, 0x00, 0x00,   // ModelId
        0x00, 0x00, 0x00, 0x00,   // AnimListId
    ];

    private static byte[] DstrFields(float animSpeed, uint initAnimState, uint health, uint spawnItemId, uint hitFlags, byte collType, byte fxType, float blastRadius, float blastStrength) =>
    [
        .. BitConverter.GetBytes(animSpeed).Reverse(),
        .. BitConverter.GetBytes(initAnimState).Reverse(),
        .. BitConverter.GetBytes(health).Reverse(),
        .. BitConverter.GetBytes(spawnItemId).Reverse(),
        .. BitConverter.GetBytes(hitFlags).Reverse(),
        collType,
        fxType,
        0x00, 0x00, // padding
        .. BitConverter.GetBytes(blastRadius).Reverse(),
        .. BitConverter.GetBytes(blastStrength).Reverse(),
    ];

    private static byte[] BfbbOnlyFields(uint destroyShrapnelId, uint hitShrapnelId, uint destroySfxId, uint hitSfxId, uint hitModelId, uint destroyModelId) =>
    [
        .. BitConverter.GetBytes(destroyShrapnelId).Reverse(),
        .. BitConverter.GetBytes(hitShrapnelId).Reverse(),
        .. BitConverter.GetBytes(destroySfxId).Reverse(),
        .. BitConverter.GetBytes(hitSfxId).Reverse(),
        .. BitConverter.GetBytes(hitModelId).Reverse(),
        .. BitConverter.GetBytes(destroyModelId).Reverse(),
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
        .. DstrFields(1.5f, 2, 1, 0xAABBCCDD, 0xC00, 2, 1, 4.0f, 2.5f),
        .. BfbbOnlyFields(0x11111111, 0x22222222, 0x33333333, 0x44444444, 0x55555555, 0x66666666),
    ];

    private static byte[] N100FData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: false),
        .. DstrFields(1.5f, 2, 1, 0xAABBCCDD, 0xC00, 2, 1, 4.0f, 2.5f),
    ];

    [Fact]
    public void Read_DestructibleObject_ProducesDestructibleObjectAsset() =>
        Assert.IsType<DestructibleObjectAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_DestructibleObject_UnderBfbb_PopulatesEveryField()
    {
        var asset = (DestructibleObjectAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(1.5f, asset.AnimationSpeed);
        Assert.Equal(2u, asset.InitialAnimationState);
        Assert.Equal(1u, asset.Health);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.SpawnItemId);
        Assert.Equal(DestructibleHitFlags.PatrickSlam | DestructibleHitFlags.Throw, asset.HitFlags);
        Assert.Equal(2, asset.CollisionType);
        Assert.Equal(DestructibleFxType.Dust, asset.FxType);
        Assert.Equal(4.0f, asset.BlastRadius);
        Assert.Equal(2.5f, asset.BlastStrength);
        Assert.Equal(new AssetId(0x11111111), asset.DestroyShrapnelId);
        Assert.Equal(new AssetId(0x22222222), asset.HitShrapnelId);
        Assert.Equal(new AssetId(0x33333333), asset.DestroySfxId);
        Assert.Equal(new AssetId(0x44444444), asset.HitSfxId);
        Assert.Equal(new AssetId(0x55555555), asset.HitModelId);
        Assert.Equal(new AssetId(0x66666666), asset.DestroyModelId);
    }

    [Fact]
    public void Read_DestructibleObject_UnderN100F_LeavesBfbbOnlyFieldsAtDefault()
    {
        var asset = (DestructibleObjectAsset)Read(N100FData(), N100FSerializer.DefaultProfile);

        Assert.Equal(1.5f, asset.AnimationSpeed);
        Assert.Equal(1u, asset.Health);
        Assert.Equal(4.0f, asset.BlastRadius);
        Assert.Equal(default, asset.DestroyShrapnelId);
        Assert.Equal(default, asset.HitShrapnelId);
        Assert.Equal(default, asset.DestroySfxId);
        Assert.Equal(default, asset.HitSfxId);
        Assert.Equal(default, asset.HitModelId);
        Assert.Equal(default, asset.DestroyModelId);
    }

    [Fact]
    public void Read_ThenWrite_DestructibleObjectUnderBfbb_ReproducesInputBytes()
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
    public void Read_ThenWrite_DestructibleObjectUnderN100F_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. N100FData(linkCount: 1),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_DestructibleObjectWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void ModelId_SetThroughIHasModel_ProjectsOntoPhysicalModelId()
    {
        var asset = new DestructibleObjectAsset();

        ((IHasModel)asset).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.ModelId);
    }

    [Fact]
    public void AnimListId_SetThroughIHasAnimList_ProjectsOntoPhysicalAnimListId()
    {
        var asset = new DestructibleObjectAsset();

        ((IHasAnimList)asset).AnimListId = new AssetId(0xCAFEF00D);

        Assert.Equal(new AssetId(0xCAFEF00D), asset.Physical.AnimListId);
    }
}

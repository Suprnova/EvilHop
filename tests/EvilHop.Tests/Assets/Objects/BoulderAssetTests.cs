using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class BoulderAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Boulder;
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
        0x2F,                   // BaseType
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
        0x11, 0x11, 0x11, 0x11,   // ModelId
        0x00, 0x00, 0x00, 0x00,   // AnimListId
    ];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] CommonFields(float gravity, float mass, float bounce, float friction, float maxVel,
        float maxAngVel, float stickiness, float bounceDamp, uint flags, float killtimer, uint hitpoints,
        uint bounceSoundId, float minSoundVel, float maxSoundVel) =>
    [
        .. F32(gravity), .. F32(mass), .. F32(bounce), .. F32(friction),
        .. F32(maxVel), .. F32(maxAngVel), .. F32(stickiness), .. F32(bounceDamp),
        .. U32(flags), .. F32(killtimer), .. U32(hitpoints), .. U32(bounceSoundId),
        .. F32(minSoundVel), .. F32(maxSoundVel),
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
        .. F32(15.0f), .. F32(1.0f), .. F32(0.3f), .. F32(0.1f), .. F32(0.5f), // gravity, mass, bounce, friction, statFric
        .. F32(20.0f), .. F32(18.0f), .. F32(0.15f), .. F32(0.0f), // maxVel, maxAngVel, stickiness, bounceDamp
        .. U32(0x222), .. F32(5.0f), .. U32(3u), .. U32(0xAABBCCDD), // flags, killtimer, hitpoints, soundId
        .. F32(0.77f), .. F32(2.0f), .. F32(40.0f), // volume, minSoundVel, maxSoundVel
        .. F32(10.0f), .. F32(20.0f), // innerRadius, outerRadius
    ];

    private static byte[] LaterData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: false),
        .. CommonFields(15.0f, 1.0f, 0.3f, 0.1f, 20.0f, 18.0f, 0.15f, 0.0f, 0x222, 5.0f, 3u, 0xAABBCCDD, 2.0f, 40.0f),
        .. F32(1.0f), // sphereRadius
        0x00, 0x00, 0x00, // pad0, pad1, pad2
        0x07, // boneIndex
    ];

    private static byte[] ROTUFullData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: false),
        .. CommonFields(15.0f, 1.0f, 0.3f, 0.1f, 20.0f, 18.0f, 0.15f, 0.0f, 0x222, 5.0f, 3u, 0xAABBCCDD, 2.0f, 40.0f),
        .. F32(1.0f), // sphereRadius
        0x00, 0x00, 0x00, // pad0, pad1, pad2
        0x07, // boneIndex
        .. F32(0.25f), // initNonCollideTime
    ];

    [Fact]
    public void Read_Boulder_ProducesBoulderAsset() =>
        Assert.IsType<BoulderAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_Boulder_UnderBfbb_PopulatesEveryField()
    {
        var asset = (BoulderAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(15.0f, asset.Gravity);
        Assert.Equal(1.0f, asset.Mass);
        Assert.Equal(0.3f, asset.Bounce);
        Assert.Equal(0.1f, asset.Friction);
        Assert.Equal(0.5f, asset.StaticFriction);
        Assert.Equal(20.0f, asset.MaxVelocity);
        Assert.Equal(18.0f, asset.MaxAngularVelocity);
        Assert.Equal(0.15f, asset.Stickiness);
        Assert.Equal(0.0f, asset.BounceDamping);
        Assert.Equal(BoulderFlags.DamagePlayer | BoulderFlags.DieOnOutOfBoundsSurfaces | BoulderFlags.DieAfterKillTimer, asset.Flags);
        Assert.Equal(5.0f, asset.KillTimer);
        Assert.Equal(3u, asset.Hitpoints);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.BounceSoundId);
        Assert.Equal(0.77f, asset.Volume);
        Assert.Equal(2.0f, asset.MinSoundVelocity);
        Assert.Equal(40.0f, asset.MaxSoundVelocity);
        Assert.Equal(10.0f, asset.InnerRadius);
        Assert.Equal(20.0f, asset.OuterRadius);
        Assert.Equal(default, asset.SoundRadius);
        Assert.Equal(0, asset.BoneIndex);
        Assert.Equal(new AssetId(0x11111111), asset.Physical.ModelId);
    }

    [Fact]
    public void Read_Boulder_UnderTSSM_PopulatesLaterFields()
    {
        var asset = (BoulderAsset)Read(LaterData(), TSSMSerializer.DefaultProfile);

        Assert.Equal(15.0f, asset.Gravity);
        Assert.Equal(default, asset.StaticFriction);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.BounceSoundId);
        Assert.Equal(default, asset.Volume);
        Assert.Equal(2.0f, asset.MinSoundVelocity);
        Assert.Equal(40.0f, asset.MaxSoundVelocity);
        Assert.Equal(1.0f, asset.SoundRadius);
        Assert.Equal(7, asset.BoneIndex);
        Assert.Equal(0.0f, asset.InitialNonCollideTime);
    }

    [Fact]
    public void Read_Boulder_UnderROTU_PopulatesInitialNonCollideTime()
    {
        var asset = (BoulderAsset)Read(ROTUFullData(), ROTUSerializer.DefaultProfile);

        Assert.Equal(1.0f, asset.SoundRadius);
        Assert.Equal(7, asset.BoneIndex);
        Assert.Equal(0.25f, asset.InitialNonCollideTime);
    }

    [Fact]
    public void Read_ThenWrite_BoulderUnderBfbb_ReproducesInputBytes()
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
    public void Read_ThenWrite_BoulderUnderTSSM_ReproducesInputBytes()
    {
        byte[] data = [.. LaterData(linkCount: 1), .. LinkBytes(1, 2, 0xAABBCCDD)];
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_BoulderUnderIncredibles_ReproducesInputBytes()
    {
        byte[] data = LaterData();
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_BoulderUnderROTU_ReproducesInputBytes()
    {
        byte[] data = [.. ROTUFullData(linkCount: 1), .. LinkBytes(1, 2, 0xAABBCCDD)];
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_BoulderWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_Boulder_UnderN100F_DegradesToGenericEntityAsset()
    {
        byte[] data = BfbbData();
        var profile = N100FSerializer.DefaultProfile;

        var asset = Read(data, profile);

        Assert.IsNotType<BoulderAsset>(asset);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void ModelId_SetThroughIHasModel_ProjectsOntoPhysicalModelId()
    {
        var asset = new BoulderAsset();

        ((IHasModel)asset).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.ModelId);
    }

    [Fact]
    public void AnimListId_SetThroughIHasAnimList_ProjectsOntoPhysicalAnimListId()
    {
        var asset = new BoulderAsset();

        ((IHasAnimList)asset).AnimListId = new AssetId(0xCAFEF00D);

        Assert.Equal(new AssetId(0xCAFEF00D), asset.Physical.AnimListId);
    }
}

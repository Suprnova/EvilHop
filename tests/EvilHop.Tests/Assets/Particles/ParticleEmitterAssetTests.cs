using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class ParticleEmitterAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.ParticleEmitter;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x26,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Vec3(Vector3 v) => [.. F32(v.X), .. F32(v.Y), .. F32(v.Z)];
    private static byte[] Padded(int size, byte[] fields) => [.. fields, .. new byte[size - fields.Length]];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] BFBBData(byte flags, byte kind, uint propId, byte[] shapeFields, uint attachToId,
        Vector3 pos, Vector3 vel, float velAngleVariation, uint cullMode, float cullDistSqr, byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        flags, kind, 0x00, 0x00, // emit_flags, emit_type, pad
        .. U32(propId),
        .. Padded(0x1C, shapeFields),
        .. U32(attachToId),
        .. Vec3(pos),
        .. Vec3(vel),
        .. F32(velAngleVariation),
        .. U32(cullMode),
        .. F32(cullDistSqr),
    ];

    private static byte[] N100FData(byte flags, byte kind, byte count, byte countVariation, float interval,
        byte[] shapeFields, uint attachToId, uint parSysId, Vector3 pos, Vector3 vel, float velAngleVariation,
        Rgba colorBirth, Rgba colorDeath, float sizeBirth, float sizeBirthVariation, float sizeDeath,
        float life, float lifeVariation, byte cullMode, float cullDistSqr, byte maxEmit, byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        flags, kind, count, countVariation,
        .. F32(interval),
        .. Padded(0x1C, shapeFields),
        .. U32(attachToId),
        .. U32(parSysId),
        .. Vec3(pos),
        .. Vec3(vel),
        .. F32(velAngleVariation),
        (byte)(colorBirth.R * 255), (byte)(colorBirth.G * 255), (byte)(colorBirth.B * 255), (byte)(colorBirth.A * 255),
        (byte)(colorDeath.R * 255), (byte)(colorDeath.G * 255), (byte)(colorDeath.B * 255), (byte)(colorDeath.A * 255),
        .. F32(sizeBirth), .. F32(sizeBirthVariation), .. F32(sizeDeath), .. F32(life), .. F32(lifeVariation),
        0x00, 0x00,   // pad_emit
        cullMode,
        0x00,         // alignment padding
        .. F32(cullDistSqr),
        maxEmit,
        0x00, 0x00, 0x00, // trailing alignment padding
    ];

    [Fact]
    public void Read_ParticleEmitter_ProducesParticleEmitterAsset() =>
        Assert.IsType<ParticleEmitterAsset>(Read(BFBBData(0, 0, 0, [], 0, default, default, 0, 3, 0)));

    [Fact]
    public void Read_ParticleEmitter_UnderBFBB_PopulatesCommonFields()
    {
        byte[] data = BFBBData(
            flags: 0x01, kind: (byte)ParticleEmitterKind.Point, propId: 0xAABBCCDD, shapeFields: [],
            attachToId: 0x11223344, pos: new Vector3(1, 2, 3), vel: new Vector3(4, 5, 6),
            velAngleVariation: 0.5f, cullMode: 3, cullDistSqr: 100f);

        var asset = (ParticleEmitterAsset)Read(data);

        Assert.Equal(ParticleEmitterFlags.On, asset.Flags);
        Assert.Equal(ParticleEmitterKind.Point, asset.Kind);
        Assert.IsType<PointEmitterShape>(asset.Shape);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.PropId);
        Assert.Equal(new AssetId(0x11223344), asset.AttachToId);
        Assert.Equal(new Vector3(1, 2, 3), asset.Position);
        Assert.Equal(new Vector3(4, 5, 6), asset.Velocity);
        Assert.Equal(0.5f, asset.VelocityAngleVariation);
        Assert.Equal(3u, asset.CullMode);
        Assert.Equal(100f, asset.CullDistanceSquared);
    }

    [Fact]
    public void Read_ParticleEmitter_UnderBFBB_CircleKind_PopulatesCircleShape()
    {
        byte[] shapeFields = [.. F32(5.0f), .. F32(0.25f), .. Vec3(new Vector3(0, 1, 0))];
        byte[] data = BFBBData(0, (byte)ParticleEmitterKind.Circle, 0, shapeFields, 0, default, default, 0, 3, 0);

        var asset = (ParticleEmitterAsset)Read(data);
        var shape = Assert.IsType<CircleEmitterShape>(asset.Shape);

        Assert.Equal(5.0f, shape.Radius);
        Assert.Equal(0.25f, shape.Deflection);
        Assert.Equal(new Vector3(0, 1, 0), shape.Direction);
    }

    [Fact]
    public void Read_ParticleEmitter_UnderBFBB_EntityBoneKind_PopulatesEntityBoneShape()
    {
        byte[] shapeFields =
        [
            0x01, 0x02, 0x03, 0x00, // flags, type, bone, pad1
            .. Vec3(new Vector3(1, 2, 3)),
            .. F32(4.0f), .. F32(0.5f),
        ];
        byte[] data = BFBBData(0, (byte)ParticleEmitterKind.EntityBone, 0, shapeFields, 0, default, default, 0, 3, 0);

        var asset = (ParticleEmitterAsset)Read(data);
        var shape = Assert.IsType<EntityBoneEmitterShape>(asset.Shape);

        Assert.Equal(1, shape.Flags);
        Assert.Equal(2, shape.AttachType);
        Assert.Equal(3, shape.Bone);
        Assert.Equal(new Vector3(1, 2, 3), shape.Offset);
        Assert.Equal(4.0f, shape.Radius);
        Assert.Equal(0.5f, shape.Deflection);
    }

    [Fact]
    public void Read_ParticleEmitter_UnderN100F_PopulatesInlinePropertiesAndCircleShapeWithoutDirection()
    {
        byte[] shapeFields = [.. F32(2.5f), .. F32(0.1f)];
        byte[] data = N100FData(
            flags: 0x01, kind: (byte)ParticleEmitterKind.Circle, count: 5, countVariation: 1, interval: 0.2f,
            shapeFields: shapeFields, attachToId: 0, parSysId: 0xCAFEBEEF, pos: new Vector3(1, 2, 3),
            vel: new Vector3(0, 1, 0), velAngleVariation: 0.1f,
            colorBirth: new Rgba(1f, 1f, 1f, 1f), colorDeath: new Rgba(1f, 1f, 1f, 0f),
            sizeBirth: 1.0f, sizeBirthVariation: 0.1f, sizeDeath: 0.5f, life: 2.0f, lifeVariation: 0.2f,
            cullMode: 3, cullDistSqr: 100f, maxEmit: 20);

        var asset = (ParticleEmitterAsset)Read(data, N100FSerializer.DefaultProfile);
        var shape = Assert.IsType<CircleEmitterShape>(asset.Shape);

        Assert.Equal(2.5f, shape.Radius);
        Assert.Equal(0.1f, shape.Deflection);
        Assert.Equal(Vector3.Zero, shape.Direction);

        Assert.Equal(5, asset.InlineProperties.Count);
        Assert.Equal(1, asset.InlineProperties.CountVariation);
        Assert.Equal(0.2f, asset.InlineProperties.Interval);
        Assert.Equal(new AssetId(0xCAFEBEEF), asset.InlineProperties.ParSysId);
        Assert.Equal(new Rgba(1f, 1f, 1f, 1f), asset.InlineProperties.ColorBirth);
        Assert.Equal(new Rgba(1f, 1f, 1f, 0f), asset.InlineProperties.ColorDeath);
        Assert.Equal(1.0f, asset.InlineProperties.SizeBirth);
        Assert.Equal(0.1f, asset.InlineProperties.SizeBirthVariation);
        Assert.Equal(0.5f, asset.InlineProperties.SizeDeath);
        Assert.Equal(2.0f, asset.InlineProperties.Life);
        Assert.Equal(0.2f, asset.InlineProperties.LifeVariation);
        Assert.Equal(20, asset.InlineProperties.MaxEmit);
        Assert.Equal(3u, asset.CullMode);
        Assert.Equal(100f, asset.CullDistanceSquared);
    }

    [Fact]
    public void Read_ThenWrite_ParticleEmitterUnderBFBB_PointKind_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BFBBData(0x01, (byte)ParticleEmitterKind.Point, 0xAABBCCDD, [], 0x11223344,
                new Vector3(1, 2, 3), new Vector3(4, 5, 6), 0.5f, 3, 100f, linkCount: 1),
            .. LinkBytes(1, 2, 0x55667788),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ParticleEmitterUnderBFBB_CircleKind_ReproducesInputBytes()
    {
        byte[] shapeFields = [.. F32(5.0f), .. F32(0.25f), .. Vec3(new Vector3(0, 1, 0))];
        byte[] data = BFBBData(0, (byte)ParticleEmitterKind.Circle, 0, shapeFields, 0, default, default, 0, 3, 0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ParticleEmitterUnderBFBB_VolumeKind_ReproducesInputBytes()
    {
        byte[] shapeFields = [.. U32(0xDEADBEEF)];
        byte[] data = BFBBData(0, (byte)ParticleEmitterKind.Volume, 0, shapeFields, 0, default, default, 0, 3, 0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ParticleEmitterUnderBFBB_LineKind_ReproducesInputBytes()
    {
        byte[] shapeFields = [.. Vec3(new Vector3(1, 2, 3)), .. Vec3(new Vector3(4, 5, 6)), .. F32(0.5f)];
        byte[] data = BFBBData(0, (byte)ParticleEmitterKind.Line, 0, shapeFields, 0, default, default, 0, 3, 0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ParticleEmitterUnderBFBB_EntityBoundKind_ReproducesInputBytes()
    {
        byte[] shapeFields = [0x01, 0x02, 0x00, 0x00, .. F32(1.5f), .. F32(0.5f)];
        byte[] data = BFBBData(0, (byte)ParticleEmitterKind.EntityBound, 0, shapeFields, 0, default, default, 0, 3, 0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ParticleEmitterUnderN100F_ReproducesInputBytes()
    {
        byte[] shapeFields = [.. F32(2.5f), .. F32(0.1f)];
        byte[] data =
        [
            .. N100FData(0x01, (byte)ParticleEmitterKind.Circle, 5, 1, 0.2f, shapeFields, 0, 0xCAFEBEEF,
                new Vector3(1, 2, 3), new Vector3(0, 1, 0), 0.1f, new Rgba(1f, 1f, 1f, 1f), new Rgba(1f, 1f, 1f, 0f),
                1.0f, 0.1f, 0.5f, 2.0f, 0.2f, 3, 100f, 20, linkCount: 1),
            .. LinkBytes(1, 2, 0x55667788),
        ];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_ParticleEmitterWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BFBBData(0, 0, 0, [], 0, default, default, 0, 3, 0), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ParticleEmitter_UnderIncredibles_DegradesToGenericBaseAsset()
    {
        byte[] data = BFBBData(0, 0, 0, [], 0, default, default, 0, 3, 0);

        var asset = Read(data, IncrediblesSerializer.DefaultProfile);

        Assert.IsNotType<ParticleEmitterAsset>(asset);
    }
}

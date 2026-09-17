using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class ParticleEmitterPropertyAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.ParticleEmitterProperty;
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
        0x2E,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] I32(int value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Vec3(Vector3 v) => [.. F32(v.X), .. F32(v.Y), .. F32(v.Z)];

    private static byte[] Interp(float start, float end, ParticleInterpolationMode mode, float freq, float oofreq) =>
    [
        .. F32(start), .. F32(end), .. U32((uint)mode), .. F32(freq), .. F32(oofreq),
    ];

    private static readonly byte[] ConstAOne = Interp(1f, 1f, ParticleInterpolationMode.ConstA, 0f, 0f);

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] Data(byte linkCount, uint parSysId, byte[] rate, byte[] life, byte[] sizeBirth, byte[] sizeDeath,
        byte[] colorBirth, byte[] colorDeath, byte[] velScale, byte[] velAngle, Vector3 vel, int emitLimit, float emitLimitResetTime) =>
    [
        .. Prefix(linkCount),
        .. U32(parSysId),
        .. rate, .. life, .. sizeBirth, .. sizeDeath,
        .. colorBirth, .. colorDeath,
        .. velScale, .. velAngle,
        .. Vec3(vel),
        .. I32(emitLimit),
        .. F32(emitLimitResetTime),
    ];

    private static byte[] DefaultColor() => [.. ConstAOne, .. ConstAOne, .. ConstAOne, .. ConstAOne];

    private static byte[] SampleData(byte linkCount = 0) => Data(
        linkCount, parSysId: 0xAABBCCDD,
        rate: Interp(5f, 5f, ParticleInterpolationMode.ConstA, 0f, 0f),
        life: Interp(1f, 2f, ParticleInterpolationMode.Linear, 0.5f, 0f),
        sizeBirth: Interp(0.5f, 0.5f, ParticleInterpolationMode.ConstA, 0f, 0f),
        sizeDeath: Interp(1f, 1f, ParticleInterpolationMode.ConstA, 0f, 0f),
        colorBirth: DefaultColor(), colorDeath: DefaultColor(),
        velScale: ConstAOne, velAngle: ConstAOne,
        vel: Vector3.Zero, emitLimit: -1, emitLimitResetTime: 0f);

    [Fact]
    public void Read_ParticleEmitterProperty_ProducesParticleEmitterPropertyAsset() =>
        Assert.IsType<ParticleEmitterPropertyAsset>(Read(SampleData()));

    [Fact]
    public void Read_ParticleEmitterProperty_PopulatesEveryField()
    {
        var asset = (ParticleEmitterPropertyAsset)Read(SampleData());

        Assert.Equal(new AssetId(0xAABBCCDD), asset.ParSysId);

        Assert.Equal(5f, asset.Rate.Start);
        Assert.Equal(5f, asset.Rate.End);
        Assert.Equal(ParticleInterpolationMode.ConstA, asset.Rate.Mode);

        Assert.Equal(1f, asset.Life.Start);
        Assert.Equal(2f, asset.Life.End);
        Assert.Equal(ParticleInterpolationMode.Linear, asset.Life.Mode);
        Assert.Equal(0.5f, asset.Life.Frequency);

        Assert.Equal(0.5f, asset.SizeBirth.Start);
        Assert.Equal(1f, asset.SizeDeath.Start);

        Assert.Equal(1f, asset.ColorBirth.Red.Start);
        Assert.Equal(ParticleInterpolationMode.ConstA, asset.ColorBirth.Alpha.Mode);
        Assert.Equal(1f, asset.ColorDeath.Blue.End);

        Assert.Equal(Vector3.Zero, asset.Velocity);
        Assert.Equal(-1, asset.EmitLimit);
        Assert.Equal(0f, asset.EmitLimitResetTime);
    }

    [Fact]
    public void Read_ParticleEmitterProperty_EmitLimitPreservesNonNegativeOneValues()
    {
        byte[] data = Data(0, 0, ConstAOne, ConstAOne, ConstAOne, ConstAOne, DefaultColor(), DefaultColor(),
            ConstAOne, ConstAOne, Vector3.Zero, emitLimit: 35, emitLimitResetTime: 0f);

        var asset = (ParticleEmitterPropertyAsset)Read(data);

        Assert.Equal(35, asset.EmitLimit);
    }

    [Fact]
    public void Read_ThenWrite_ParticleEmitterPropertyWithNoLinks_ReproducesInputBytes()
    {
        byte[] data = SampleData();

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ParticleEmitterPropertyWithLinks_ReproducesInputBytes()
    {
        byte[] data = [.. SampleData(linkCount: 2), .. LinkBytes(1, 2, 0xAABBCCDD), .. LinkBytes(3, 4, 0x11223344)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ParticleEmitterPropertyWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. SampleData(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Theory]
    [InlineData(ParticleInterpolationMode.ConstA)]
    [InlineData(ParticleInterpolationMode.ConstB)]
    [InlineData(ParticleInterpolationMode.Random)]
    [InlineData(ParticleInterpolationMode.Linear)]
    [InlineData(ParticleInterpolationMode.Sine)]
    [InlineData(ParticleInterpolationMode.Cosine)]
    [InlineData(ParticleInterpolationMode.Step)]
    public void Read_ThenWrite_ParticleEmitterPropertyWithEachInterpolationMode_ReproducesInputBytes(ParticleInterpolationMode mode)
    {
        byte[] rate = Interp(1f, 2f, mode, 0.25f, 0.75f);
        byte[] data = Data(0, 0, rate, ConstAOne, ConstAOne, ConstAOne, DefaultColor(), DefaultColor(),
            ConstAOne, ConstAOne, Vector3.Zero, -1, 0f);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ParticleEmitterProperty_UnderIncredibles_DegradesToGenericBaseAsset()
    {
        byte[] data = SampleData();

        var asset = Read(data, IncrediblesSerializer.DefaultProfile);

        Assert.IsNotType<ParticleEmitterPropertyAsset>(asset);
    }
}

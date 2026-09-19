using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class SoundFXAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.SoundFX;
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
        0x13,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
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

    private static byte[] U16(ushort value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Data(
        ushort flags = 0, ushort frequency = 0, float frequencyMultiplier = 1.0f,
        uint soundId = 0, uint attachId = 0, byte loopCount = 0, byte priority = 0, byte volume = 0,
        float x = 0, float y = 0, float z = 0, float innerRadius = 0, float outerRadius = 0, byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. U16(flags),
        .. U16(frequency),
        .. F32(frequencyMultiplier),
        .. U32(soundId),
        .. U32(attachId),
        loopCount,
        priority,
        volume,
        0x00, // pad
        .. F32(x),
        .. F32(y),
        .. F32(z),
        .. F32(innerRadius),
        .. F32(outerRadius),
    ];

    [Fact]
    public void Read_SoundFX_ProducesSoundFXAsset() =>
        Assert.IsType<SoundFXAsset>(Read(Data()));

    [Fact]
    public void Read_SoundFX_PopulatesFields()
    {
        byte[] data = Data(
            frequency: 100, frequencyMultiplier: 2.0f, soundId: 0x11223344, attachId: 0x55667788,
            loopCount: 3, priority: 128, volume: 75, x: 1.0f, y: 2.0f, z: 3.0f, innerRadius: 10.0f, outerRadius: 40.0f);

        var asset = (SoundFXAsset)Read(data);

        Assert.Equal(100, asset.Frequency);
        Assert.Equal(2.0f, asset.FrequencyMultiplier);
        Assert.Equal(new AssetId(0x11223344), asset.SoundId);
        Assert.Equal(new AssetId(0x55667788), asset.AttachId);
        Assert.Equal(3, ((IPhysicalSoundFXAsset)asset).LoopCount);
        Assert.Equal(128, asset.Priority);
        Assert.Equal(75, asset.Volume);
        Assert.Equal(1.0f, asset.Position.X);
        Assert.Equal(2.0f, asset.Position.Y);
        Assert.Equal(3.0f, asset.Position.Z);
        Assert.Equal(10.0f, asset.InnerRadius);
        Assert.Equal(40.0f, asset.OuterRadius);
    }

    [Theory]
    [InlineData((ushort)0x2, true, false, false)]
    [InlineData((ushort)0x4, false, true, false)]
    [InlineData((ushort)0x8, false, false, true)]
    public void Read_SoundFX_PopulatesFlags(ushort flags, bool positional, bool loop, bool playFromEntity)
    {
        var asset = (SoundFXAsset)Read(Data(flags: flags));

        Assert.Equal(positional, asset.Positional);
        Assert.Equal(loop, asset.Loop);
        Assert.Equal(playFromEntity, asset.PlayFromEntity);
    }

    [Fact]
    public void Positional_SetTrue_PreservesOtherFlagBits()
    {
        var asset = (SoundFXAsset)Read(Data(flags: 0x400));

        asset.Positional = true;

        Assert.Equal((SFXFlags)0x402, ((IPhysicalSoundFXAsset)asset).SFXFlags);
    }

    [Fact]
    public void Positional_SetFalse_PreservesOtherFlagBits()
    {
        var asset = (SoundFXAsset)Read(Data(flags: 0x402));

        asset.Positional = false;

        Assert.Equal((SFXFlags)0x400, ((IPhysicalSoundFXAsset)asset).SFXFlags);
    }

    [Fact]
    public void SFXFlags_Values_MatchDocumentedBits()
    {
        Assert.Equal((ushort)0, (ushort)SFXFlags.None);
        Assert.Equal((ushort)0x2, (ushort)SFXFlags.Positional);
        Assert.Equal((ushort)0x4, (ushort)SFXFlags.Loop);
        Assert.Equal((ushort)0x8, (ushort)SFXFlags.PlayFromEntity);
    }

    [Fact]
    public void Read_ThenWrite_SoundFXWithNoLinks_ReproducesInputBytes()
    {
        byte[] data = Data(flags: 0x2, soundId: 0x11223344, priority: 128, volume: 100, outerRadius: 40.0f);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SoundFXWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(flags: 0xA, attachId: 0x55667788, linkCount: 1),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SoundFXWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_SoundFX_UnderTSSM_DegradesToGenericBaseAsset()
    {
        byte[] data = Data();

        var asset = Read(data, TSSMSerializer.DefaultProfile);

        Assert.IsNotType<SoundFXAsset>(asset);
    }

    [Fact]
    public void Read_ThenWrite_RealN100FExemplar_ReproducesInputBytes()
    {
        // n100f/prototype_2003-07-08/XBOX/NTSC-U/US/b0/b001.HIP, AHDR id=0x03992F2A
        byte[] data =
        [
            0x2A, 0x2F, 0x99, 0x03, 0x13, 0x00, 0x1D, 0x00,
            0xC0, 0x01, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F,
            0x6D, 0x88, 0x04, 0x8C, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x80, 0x64, 0x00,
            0x9A, 0x90, 0x80, 0x42, 0xA1, 0x05, 0xEF, 0x41, 0x9A, 0x90, 0x80, 0x42,
            0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0x00, 0x00,
        ];
        var profile = N100FSerializer.DefaultProfile with { Platform = Platform.Xbox };

        var asset = (SoundFXAsset)Read(data, profile);

        Assert.Equal(new AssetId(0x03992F2A), asset.Physical.BaseId);
        Assert.Equal(0x13, asset.Physical.BaseType);
        Assert.False(asset.Positional);
        Assert.False(asset.PlayFromEntity);
        Assert.Equal(new AssetId(0x8C04886D), asset.SoundId);
        Assert.Equal(AssetId.None, asset.AttachId);
        Assert.Equal(1.0f, asset.FrequencyMultiplier);
        Assert.Equal(128, asset.Priority);
        Assert.Equal(100, asset.Volume);
        Assert.Equal(10.0f, asset.InnerRadius);

        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_RealBFBBExemplar_ReproducesInputBytesWithAttachFlag()
    {
        // bfbb/prototype_2003-10-01/GC/NTSC-U/US/b1/b101.HIP, AHDR id=0xBC6F4033
        byte[] data =
        [
            0xBC, 0x6F, 0x40, 0x33, 0x13, 0x00, 0x00, 0x1D,
            0x01, 0xCA, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00,
            0x0D, 0xA0, 0xF4, 0x20, 0x77, 0x5C, 0x10, 0x65,
            0x00, 0x80, 0x2D, 0x00,
            0xC1, 0x9B, 0x81, 0xA3, 0x40, 0x92, 0xB9, 0xE0, 0x40, 0xB9, 0xF8, 0x23,
            0x41, 0x20, 0x00, 0x00, 0x42, 0x20, 0x00, 0x00,
        ];
        var profile = BFBBSerializer.DefaultProfile;

        var asset = (SoundFXAsset)Read(data, profile);

        Assert.Equal(new AssetId(0x0DA0F420), asset.SoundId);
        Assert.Equal(new AssetId(0x775C1065), asset.AttachId);
        Assert.True(asset.Positional);
        Assert.True(asset.PlayFromEntity);
        Assert.False(asset.Loop);
        Assert.Equal(128, asset.Priority);
        Assert.Equal(45, asset.Volume);
        Assert.Equal(10.0f, asset.InnerRadius);
        Assert.Equal(40.0f, asset.OuterRadius);

        Assert.Equal(data, Write(asset, profile));
    }
}

using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using static EvilHop.Assets.SoundEffectAsset;

namespace EvilHop.Tests.Serialization;

public class SoundEffectAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new TSSMSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.SoundEffect;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= TSSMSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= TSSMSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x4B,                   // BaseType
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

    private static byte[] Data(uint soundGroupId = 0, uint attachId = 0, float x = 0, float y = 0, float z = 0, uint flags = 0, byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        (byte)(soundGroupId >> 24), (byte)(soundGroupId >> 16), (byte)(soundGroupId >> 8), (byte)soundGroupId,
        (byte)(attachId >> 24), (byte)(attachId >> 16), (byte)(attachId >> 8), (byte)attachId,
        .. BitConverter.GetBytes(x).Reverse(),
        .. BitConverter.GetBytes(y).Reverse(),
        .. BitConverter.GetBytes(z).Reverse(),
        (byte)(flags >> 24), (byte)(flags >> 16), (byte)(flags >> 8), (byte)flags,
    ];

    [Fact]
    public void Read_SoundEffect_ProducesSoundEffectAsset() =>
        Assert.IsType<SoundEffectAsset>(Read(Data()));

    [Fact]
    public void Read_SoundEffect_PopulatesFields()
    {
        byte[] data = Data(soundGroupId: 0xFF1451F6, attachId: 0x11223344, x: 1.5f, y: -2.5f, z: 3.0f);

        var asset = (SoundEffectAsset)Read(data);

        Assert.Equal(new AssetId(0xFF1451F6), asset.SoundGroupId);
        Assert.Equal(new AssetId(0x11223344), asset.AttachId);
        Assert.Equal(1.5f, asset.Position.X);
        Assert.Equal(-2.5f, asset.Position.Y);
        Assert.Equal(3.0f, asset.Position.Z);
    }

    [Fact]
    public void Read_SoundEffect_WithAttachFlagBit_PopulatesPlayFromEntity()
    {
        byte[] data = Data(flags: 0x4);

        var asset = (SoundEffectAsset)Read(data);

        Assert.True(asset.PlayFromEntity);
    }

    [Fact]
    public void Read_SoundEffect_WithoutAttachFlagBit_PlayFromEntityIsFalse()
    {
        byte[] data = Data(flags: 0);

        var asset = (SoundEffectAsset)Read(data);

        Assert.False(asset.PlayFromEntity);
    }

    [Fact]
    public void PlayFromEntity_SetTrue_PreservesOtherFlagBits()
    {
        var asset = (SoundEffectAsset)Read(Data(flags: 0x1));

        asset.PlayFromEntity = true;

        Assert.Equal((Behavior)0x5, ((Physical.ISoundEffectAsset)asset).SoundFlags);
    }

    [Fact]
    public void PlayFromEntity_SetFalse_PreservesOtherFlagBits()
    {
        var asset = (SoundEffectAsset)Read(Data(flags: 0x5));

        asset.PlayFromEntity = false;

        Assert.Equal((Behavior)0x1, ((Physical.ISoundEffectAsset)asset).SoundFlags);
    }

    [Fact]
    public void SoundFlags_Values_MatchDocumentedBits()
    {
        Assert.Equal(0u, (uint)Behavior.None);
        Assert.Equal(0x4u, (uint)Behavior.PlayFromEntity);
    }

    [Fact]
    public void Read_ThenWrite_SoundEffectWithNoLinks_ReproducesInputBytes()
    {
        byte[] data = Data(soundGroupId: 0xFF1451F6, attachId: 0, x: 1.0f, y: 2.0f, z: 3.0f, flags: 0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SoundEffectWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(soundGroupId: 0xFF1451F6, attachId: 0x11223344, flags: 0x4, linkCount: 1),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SoundEffectWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_RealTSSMExemplar_ReproducesInputBytes()
    {
        // tssm/release/GC/NTSC-U/US-r1/AM/am01.HIP, AHDR id=0x7A21D851
        byte[] data =
        [
            0x7A, 0x21, 0xD8, 0x51, 0x4B, 0x00, 0x00, 0x1D,
            0x46, 0xE9, 0x95, 0xEB, 0x00, 0x00, 0x00, 0x00,
            0xC0, 0xB0, 0xB8, 0x52, 0x41, 0xCD, 0x86, 0x25, 0xC2, 0x14, 0xEF, 0x1B,
            0x00, 0x00, 0x00, 0x00,
        ];

        var asset = (SoundEffectAsset)Read(data);

        Assert.Equal(new AssetId(0x7A21D851), asset.Physical.BaseId);
        Assert.Equal(0x4B, asset.Physical.BaseType);
        Assert.Equal(new AssetId(0x46E995EB), asset.SoundGroupId);
        Assert.Equal(AssetId.None, asset.AttachId);
        Assert.False(asset.PlayFromEntity);

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_ThenWrite_RealIncrediblesExemplar_ReproducesInputBytes()
    {
        // incredibles/prototype_2004-07-19/GC/NTSC-U/US/BM/bm01.HIP, AHDR id=0x386E653F
        byte[] data =
        [
            0x38, 0x6E, 0x65, 0x3F, 0x4B, 0x00, 0x00, 0x1D,
            0xFF, 0x14, 0x51, 0xF6, 0x00, 0x00, 0x00, 0x00,
            0xC2, 0x6B, 0x98, 0x45, 0x41, 0x12, 0xEC, 0xC0, 0x41, 0xDA, 0xE8, 0x3E,
            0x00, 0x00, 0x00, 0x00,
        ];
        var profile = IncrediblesSerializer.DefaultProfile;

        var asset = (SoundEffectAsset)Read(data, profile);

        Assert.Equal(new AssetId(0xFF1451F6), asset.SoundGroupId);
        Assert.False(asset.PlayFromEntity);

        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_RealROTUExemplar_ReproducesInputBytesWithAttachFlag()
    {
        // rotu/prototype_2005-09-15/GC/NTSC-J/JP/A1/a101.HIP, AHDR id=0x0242146C
        byte[] data =
        [
            0x02, 0x42, 0x14, 0x6C, 0x4B, 0x00, 0x00, 0x1D,
            0x27, 0xD0, 0xB3, 0x8B, 0x9B, 0x4B, 0x7B, 0x8D,
            0xC1, 0xC3, 0xEE, 0x2F, 0x42, 0x0F, 0x35, 0x0B, 0xC2, 0x97, 0x08, 0x73,
            0x00, 0x00, 0x00, 0x04,
        ];
        var profile = ROTUSerializer.DefaultProfile;

        var asset = (SoundEffectAsset)Read(data, profile);

        Assert.Equal(new AssetId(0x27D0B38B), asset.SoundGroupId);
        Assert.Equal(new AssetId(0x9B4B7B8D), asset.AttachId);
        Assert.True(asset.PlayFromEntity);

        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_RealRatatouilleExemplar_ReproducesInputBytes()
    {
        // rat/prototype_2006-01-18/GC/NTSC-U/US/FP/fp01.HIP, AHDR id=0x021E7045
        byte[] data =
        [
            0x02, 0x1E, 0x70, 0x45, 0x4B, 0x00, 0x00, 0x1D,
            0x15, 0x93, 0xB3, 0x96, 0x00, 0x00, 0x00, 0x00,
            0xC2, 0xAF, 0x0A, 0xF5, 0x40, 0x12, 0x00, 0xD2, 0x42, 0x54, 0x4E, 0x3C,
            0x00, 0x00, 0x00, 0x00,
        ];
        var profile = RatatouilleSerializer.DefaultProfile;

        var asset = (SoundEffectAsset)Read(data, profile);

        Assert.Equal(new AssetId(0x1593B396), asset.SoundGroupId);
        Assert.False(asset.PlayFromEntity);

        Assert.Equal(data, Write(asset, profile));
    }
}

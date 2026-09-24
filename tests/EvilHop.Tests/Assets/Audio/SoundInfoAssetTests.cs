using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using static EvilHop.Assets.SoundInfoAsset;
using static EvilHop.Assets.SoundInfoAsset.Sound;
using static EvilHop.Assets.SoundInfoAsset.WaveHeader;

namespace EvilHop.Tests.Serialization;

public class SoundInfoAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.SoundInfo;
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

    private static byte[] BigEndian(uint value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] BigEndian(ushort value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] DspHeader(uint sampleCount, uint nibbleCount, uint sampleRate, bool looped, uint loopStart, uint loopEnd, uint soundAssetId) =>
    [
        .. BigEndian(sampleCount),
        .. BigEndian(nibbleCount),
        .. BigEndian(sampleRate),
        .. BigEndian((ushort)(looped ? 1 : 0)), // loop_flag
        0x00, 0x00,                             // format
        .. BigEndian(loopStart),                // sa
        .. BigEndian(loopEnd),                  // ea
        .. BigEndian(2u),                       // ca, always 2
        .. new byte[32],                        // coef
        0x00, 0x00,                             // gain
        0x00, 0x00,                             // pred_scale
        0x00, 0x00,                             // yn1
        0x00, 0x00,                             // yn2
        0x00, 0x00,                             // loop_pred_scale
        0x00, 0x00,                             // loop_yn1
        0x00, 0x00,                             // loop_yn2
        .. new byte[22],                        // pad
        .. BigEndian(soundAssetId),
    ];

    private static byte[] N100FData(int effectCount, int streamCount, params byte[][] entries) =>
    [
        .. BigEndian((uint)effectCount),
        0xCD, 0xCD, 0xCD, 0xCD, // padding
        .. BigEndian((uint)streamCount),
        .. entries.SelectMany(entry => entry),
    ];

    private static byte[] BfbbData(int effectCount, int streamCount, int cutsceneCount, params byte[][] entries) =>
    [
        .. BigEndian((uint)effectCount),
        0xCD, 0xCD, 0xCD, 0xCD, // padding
        .. BigEndian((uint)streamCount),
        .. BigEndian((uint)cutsceneCount),
        .. entries.SelectMany(entry => entry),
    ];

    [Fact]
    public void Read_SoundInfo_UnderN100F_ProducesSoundInfoAsset() =>
        Assert.IsType<SoundInfoAsset>(Read(N100FData(0, 0), N100FSerializer.DefaultProfile));

    [Fact]
    public void Read_SoundInfo_UnderN100F_PopulatesEffectsAndStreams()
    {
        byte[] data = N100FData(1, 1,
            DspHeader(1000, 2020, 16000, looped: false, 0, 2020, 0x11111111),
            DspHeader(2000, 4040, 22050, looped: true, 10, 4030, 0x22222222));

        var asset = (SoundInfoAsset)Read(data, N100FSerializer.DefaultProfile);

        Assert.Single(asset.Effects);
        Assert.Single(asset.Streams);
        Assert.Empty(asset.Cutscenes);

        var effect = Assert.IsType<DspHeader>(asset.Effects[0]);
        Assert.Equal(1000u, effect.SampleCount);
        Assert.Equal(2020u, effect.NibbleCount);
        Assert.Equal(16000u, effect.SampleRate);
        Assert.False(effect.IsLooped);
        Assert.Equal(0u, effect.LoopStart);
        Assert.Equal(2020u, effect.LoopEnd);
        Assert.Equal(2u, effect.InitialOffset);
        Assert.Equal(new AssetId(0x11111111), effect.SoundAssetId);

        var stream = Assert.IsType<DspHeader>(asset.Streams[0]);
        Assert.Equal(2000u, stream.SampleCount);
        Assert.True(stream.IsLooped);
        Assert.Equal(10u, stream.LoopStart);
        Assert.Equal(new AssetId(0x22222222), stream.SoundAssetId);
    }

    [Fact]
    public void Read_SoundInfo_UnderBfbb_PopulatesCutscenes()
    {
        byte[] data = BfbbData(0, 0, 1, DspHeader(500, 1010, 22050, looped: false, 0, 1010, 0x33333333));

        var asset = (SoundInfoAsset)Read(data, BFBBSerializer.DefaultProfile);

        Assert.Empty(asset.Effects);
        Assert.Empty(asset.Streams);
        Assert.Single(asset.Cutscenes);
        Assert.Equal(new AssetId(0x33333333), asset.Cutscenes[0].SoundAssetId);
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoUnderN100F_ReproducesInputBytes()
    {
        byte[] data = N100FData(1, 1,
            DspHeader(1000, 2020, 16000, looped: false, 0, 2020, 0x11111111),
            DspHeader(2000, 4040, 22050, looped: true, 10, 4030, 0x22222222));
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoUnderBfbb_ReproducesInputBytes()
    {
        byte[] data = BfbbData(1, 0, 1,
            DspHeader(1000, 2020, 16000, looped: false, 0, 2020, 0x11111111),
            DspHeader(500, 1010, 22050, looped: false, 0, 1010, 0x33333333));
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = N100FData(0, 0);
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. N100FData(0, 0), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    private static byte[] Fsb3Header(uint soundInfoId, uint footerOffset, ushort soundCount, ushort ramSoundCount, ushort streamedSoundCount, byte soundBankCount, byte cutsceneCount) =>
    [
        .. BigEndian(soundInfoId),
        .. BigEndian(footerOffset),
        .. new byte[16], // pFMusicMod/pFSBFileArray/pWavInfoArray/pCutsceneAudioHeaders
        .. BigEndian(soundCount),
        .. BigEndian(ramSoundCount),
        .. BigEndian(streamedSoundCount),
        soundBankCount,
        cutsceneCount,
    ];

    private static byte[] SoundBankEntry(uint soundAssetId, byte flags, byte sampleIndex, byte soundBankIndex, byte soundInfoIndex) =>
    [
        .. BigEndian(soundAssetId),
        flags,
        sampleIndex,
        soundBankIndex,
        soundInfoIndex,
    ];

    private static byte[] Fsb3Data(byte[][] banks, byte[][] soundEntries, byte[][] cutsceneEntries)
    {
        var offsets = new uint[banks.Length];
        uint offset = 0;
        for (int i = 0; i < banks.Length; i++)
        {
            offsets[i] = offset;
            offset += (uint)banks[i].Length;
        }

        int ramSoundCount = soundEntries.Count(entry => (entry[4] & (byte)Identity.Streaming) == 0); // flags byte
        int streamedSoundCount = soundEntries.Length - ramSoundCount;

        return
        [
            .. Fsb3Header(0, offset, (ushort)soundEntries.Length, (ushort)ramSoundCount, (ushort)streamedSoundCount, (byte)banks.Length, (byte)cutsceneEntries.Length),
            .. banks.SelectMany(bank => bank),
            .. offsets.SelectMany(BigEndian),
            .. soundEntries.SelectMany(entry => entry),
            .. cutsceneEntries.SelectMany(entry => entry),
        ];
    }

    [Fact]
    public void Read_SoundInfo_UnderTSSM_ProducesSoundInfoAsset() =>
        Assert.IsType<SoundInfoAsset>(Read(Fsb3Data([], [], []), TSSMSerializer.DefaultProfile));

    [Fact]
    public void Read_SoundInfo_UnderTSSM_PopulatesSoundBanksAndSounds()
    {
        byte[] bank0 = [0x01, 0x02, 0x03, 0x04];
        byte[] bank1 = [0x05, 0x06, 0x07, 0x08, 0x09, 0x0A];
        byte[] data = Fsb3Data(
            [bank0, bank1],
            [SoundBankEntry(0x11111111, (byte)Identity.None, 0, 0, 0),
             SoundBankEntry(0x22222222, (byte)Identity.Streaming, 0, 1, 0)],
            []);

        var asset = (SoundInfoAsset)Read(data, TSSMSerializer.DefaultProfile);

        Assert.Equal(2, asset.SoundBanks.Count);
        Assert.Equal(bank0, asset.SoundBanks[0]);
        Assert.Equal(bank1, asset.SoundBanks[1]);

        Assert.Equal(2, asset.Sounds.Count);
        Assert.Equal(new AssetId(0x11111111), asset.Sounds[0].SoundAssetId);
        Assert.Equal(Identity.None, asset.Sounds[0].Flags);
        Assert.Equal(0, asset.Sounds[0].SoundBankIndex);
        Assert.Equal(new AssetId(0x22222222), asset.Sounds[1].SoundAssetId);
        Assert.Equal(Identity.Streaming, asset.Sounds[1].Flags);
        Assert.Equal(1, asset.Sounds[1].SoundBankIndex);
    }

    [Fact]
    public void Read_SoundInfo_UnderIncredibles_PopulatesCutsceneHeaders()
    {
        byte[] data = Fsb3Data([], [], [DspHeader(500, 1010, 22050, looped: false, 0, 1010, 0x44444444)]);

        var asset = (SoundInfoAsset)Read(data, IncrediblesSerializer.DefaultProfile);

        Assert.Single(asset.Cutscenes);
        Assert.Equal(new AssetId(0x44444444), asset.Cutscenes[0].SoundAssetId);
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoUnderTSSM_ReproducesInputBytes()
    {
        byte[] bank0 = [0x01, 0x02, 0x03, 0x04];
        byte[] bank1 = [0x05, 0x06, 0x07, 0x08, 0x09, 0x0A];
        byte[] data = Fsb3Data(
            [bank0, bank1],
            [SoundBankEntry(0x11111111, (byte)Identity.None, 0, 0, 0),
             SoundBankEntry(0x22222222, (byte)Identity.Streaming, 0, 1, 0)],
            []);
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    /// <summary>
    /// Real archives carry a sound in bank 0 flagged <see cref="Identity.Streaming"/>
    /// (TSSM's <c>mnus.HOP</c>) - <c>nSounds</c>/<c>nStreams</c> must classify it by
    /// <see cref="Sound.Flags"/>, not by <see cref="Sound.SoundBankIndex"/>, or the
    /// counts land one off in each direction.
    /// </summary>
    [Fact]
    public void Read_ThenWrite_SoundInfoUnderTSSM_ClassifiesCountsByFlagsNotBankIndex()
    {
        byte[] bank0 = [0x01, 0x02, 0x03, 0x04];
        byte[] data = Fsb3Data(
            [bank0],
            [SoundBankEntry(0x11111111, (byte)Identity.Streaming, 0, 0, 0)],
            []);
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoUnderIncredibles_ReproducesInputBytes()
    {
        byte[] data = Fsb3Data([[0xAA, 0xBB]], [SoundBankEntry(0x11111111, 0, 0, 0, 0)],
            [DspHeader(500, 1010, 22050, looped: false, 0, 1010, 0x44444444)]);
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoUnderROTU_ReproducesInputBytes()
    {
        byte[] data = Fsb3Data([], [], []);
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoUnderRatatouille_ReproducesInputBytes()
    {
        byte[] data = Fsb3Data([], [], []);
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoFsb3WithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Fsb3Data([], [], []), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    private static readonly FormatProfile XboxProfile = TSSMSerializer.DefaultProfile with { Platform = Platform.Xbox };

    private static readonly FormatProfile PS2Profile = BFBBSerializer.DefaultProfile with { Platform = Platform.PlayStation2 };

    private static readonly FormatProfile N100FPS2Profile = N100FSerializer.DefaultProfile with { Platform = Platform.PlayStation2 };

    private static byte[] LittleEndian(uint value) => BitConverter.GetBytes(value);

    private static byte[] LittleEndian(ushort value) => BitConverter.GetBytes(value);

    private static byte[] WaveHeader(ushort format, ushort channels, uint sampleRate, uint dataSize, uint soundAssetId, uint playback) =>
    [
        .. LittleEndian(format),
        .. LittleEndian(channels),
        .. LittleEndian(sampleRate),
        .. LittleEndian(sampleRate * 36 / 64), // nAvgBytesPerSec
        .. LittleEndian((ushort)36),           // nBlockAlign
        .. LittleEndian((ushort)4),            // wBitsPerSample
        .. LittleEndian((ushort)2),            // cbSize
        .. LittleEndian((ushort)64),           // NibblesPerBlock
        .. LittleEndian(dataSize),
        .. LittleEndian(soundAssetId),
        .. LittleEndian(playback),
        .. new byte[12],                       // padding
    ];

    private static byte[] XboxData(int effectCount, int streamCount, int cutsceneCount, params byte[][] entries) =>
    [
        .. LittleEndian((uint)effectCount),
        .. LittleEndian((uint)streamCount),
        .. LittleEndian((uint)cutsceneCount),
        .. entries.SelectMany(entry => entry),
    ];

    private static byte[] VagHeader(uint version, uint soundAssetId, uint dataSize, uint sampleRate, bool bigEndianFields = false)
    {
        Func<uint, byte[]> field = bigEndianFields ? BigEndian : LittleEndian;
        return
        [
            .. "VAGp"u8,
            .. field(version),
            .. LittleEndian(soundAssetId),
            .. field(dataSize),
            .. field(sampleRate),
            .. new byte[12],                                    // reserved
            .. "sound.vag\0"u8, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, // name, with leftover bytes after the terminator
        ];
    }

    private static byte[] PS2Data(int effectCount, int streamCount, params byte[][] entries) =>
    [
        .. LittleEndian((uint)effectCount),
        .. LittleEndian((uint)streamCount),
        .. entries.SelectMany(entry => entry),
    ];

    [Fact]
    public void Read_SoundInfo_UnderXbox_PopulatesWaveHeaders()
    {
        byte[] data = XboxData(1, 1, 0,
            WaveHeader(0x69, 1, 22050, 4000, 0x11111111, 1),
            WaveHeader(0x01, 2, 44100, 8000, 0x22222222, 2));

        var asset = (SoundInfoAsset)Read(data, XboxProfile);

        var effect = Assert.IsType<WaveHeader>(Assert.Single(asset.Effects));
        Assert.Equal(WaveFormat.XboxAdpcm, effect.Format);
        Assert.Equal(1, effect.ChannelCount);
        Assert.Equal(22050u, effect.SampleRate);
        Assert.Equal(22050u * 36 / 64, effect.AverageBytesPerSecond);
        Assert.Equal(36, effect.BlockAlign);
        Assert.Equal(4, effect.BitsPerSample);
        Assert.Equal(2, effect.ExtraSize);
        Assert.Equal(64, effect.SamplesPerBlock);
        Assert.Equal(4000u, effect.DataSize);
        Assert.Equal(new AssetId(0x11111111), effect.SoundAssetId);
        Assert.Equal(PlaybackMode.Looped, effect.Playback);

        var stream = Assert.IsType<WaveHeader>(Assert.Single(asset.Streams));
        Assert.Equal(WaveFormat.Pcm, stream.Format);
        Assert.Equal(2, stream.ChannelCount);
        Assert.Equal(PlaybackMode.Unknown2, stream.Playback);
        Assert.Empty(asset.Cutscenes);
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoUnderXbox_ReproducesInputBytes()
    {
        byte[] data = XboxData(1, 1, 1,
            WaveHeader(0x69, 1, 22050, 4000, 0x11111111, 1),
            WaveHeader(0x01, 2, 44100, 8000, 0x22222222, 2),
            WaveHeader(0x69, 1, 32000, 1234, 0x33333333, 0));

        Assert.Equal(data, Write(Read(data, XboxProfile), XboxProfile));
    }

    [Fact]
    public void Read_SoundInfo_UnderPS2_PopulatesVagHeaders()
    {
        byte[] data = PS2Data(1, 1,
            VagHeader(32, 0x11111111, 4000, 22050),
            VagHeader(4, 0x22222222, 8000, 44100));

        var asset = (SoundInfoAsset)Read(data, PS2Profile);

        var effect = Assert.IsType<VagHeader>(Assert.Single(asset.Effects));
        Assert.Equal(0x70474156u, effect.Magic);
        Assert.Equal(32u, effect.Version);
        Assert.Equal(new AssetId(0x11111111), effect.SoundAssetId);
        Assert.Equal(4000u, effect.DataSize);
        Assert.Equal(22050u, effect.SampleRate);
        Assert.Equal(4u, Assert.IsType<VagHeader>(Assert.Single(asset.Streams)).Version);
        Assert.Empty(asset.Cutscenes);
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoUnderPS2_ReproducesInputBytes()
    {
        byte[] data = PS2Data(1, 1,
            VagHeader(32, 0x11111111, 4000, 22050),
            VagHeader(4, 0x22222222, 8000, 44100));

        Assert.Equal(data, Write(Read(data, PS2Profile), PS2Profile));
    }

    [Fact]
    public void Read_SoundInfo_UnderN100FPS2_ReadsVersionSizeAndRateBigEndian()
    {
        byte[] data = PS2Data(1, 0, VagHeader(3, 0x11111111, 4000, 22050, bigEndianFields: true));

        var effect = Assert.IsType<VagHeader>(Assert.Single(((SoundInfoAsset)Read(data, N100FPS2Profile)).Effects));

        Assert.Equal(3u, effect.Version);
        Assert.Equal(new AssetId(0x11111111), effect.SoundAssetId);
        Assert.Equal(4000u, effect.DataSize);
        Assert.Equal(22050u, effect.SampleRate);
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoUnderN100FPS2_ReproducesInputBytes()
    {
        byte[] data = PS2Data(1, 0, VagHeader(3, 0x11111111, 4000, 22050, bigEndianFields: true));

        Assert.Equal(data, Write(Read(data, N100FPS2Profile), N100FPS2Profile));
    }

    [Theory]
    [InlineData(GameVersion.Incredibles)]
    [InlineData(GameVersion.ROTU)]
    public void Read_ThenWrite_SoundInfoUnderPS2WithLeadingId_ReproducesInputBytes(GameVersion game)
    {
        byte[] data = [.. LittleEndian(0x12345678u), .. PS2Data(1, 0, VagHeader(32, 0x11111111, 4000, 22050))];
        var profile = Serializer.DefaultProfileFor(game) with { Platform = Platform.PlayStation2 };

        var asset = (SoundInfoAsset)Read(data, profile);

        Assert.Equal(new AssetId(0x12345678), asset.Physical.SoundInfoId);
        Assert.Single(asset.Effects);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Write_HeaderForAnotherPlatform_Throws()
    {
        var asset = new SoundInfoAsset();
        asset.Effects.Add(new DspHeader());

        Assert.Throws<InvalidOperationException>(() => Write(asset, XboxProfile));
    }

    [Fact]
    public void EffectCount_DisagreeingWithEffects_IsStoredIndependently()
    {
        var asset = new SoundInfoAsset();
        asset.Effects.Add(new DspHeader());

        asset.Physical.EffectCount = 5;

        Assert.Equal(5, asset.Physical.EffectCount);
        Assert.Single(asset.Effects);
    }

    [Fact]
    public void EffectCount_MatchingEffects_DerivesFromEffects()
    {
        var asset = new SoundInfoAsset();
        asset.Effects.Add(new DspHeader());
        asset.Effects.Add(new DspHeader());

        asset.Physical.EffectCount = 2;
        asset.Effects.Add(new DspHeader());

        Assert.Equal(3, asset.Physical.EffectCount);
    }

    [Fact]
    public void SoundInfoId_DisagreeingWithId_IsStoredIndependently()
    {
        var asset = new SoundInfoAsset { Id = new AssetId(0x11111111) };

        asset.Physical.SoundInfoId = new AssetId(0x22222222);

        Assert.Equal(new AssetId(0x22222222), asset.Physical.SoundInfoId);
        Assert.Equal(new AssetId(0x11111111), asset.Id);
    }
}

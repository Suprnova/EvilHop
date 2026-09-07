using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

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

        var effect = asset.Effects[0];
        Assert.Equal(1000u, effect.SampleCount);
        Assert.Equal(2020u, effect.NibbleCount);
        Assert.Equal(16000u, effect.SampleRate);
        Assert.False(effect.IsLooped);
        Assert.Equal(0u, effect.LoopStart);
        Assert.Equal(2020u, effect.LoopEnd);
        Assert.Equal(2u, effect.InitialOffset);
        Assert.Equal(new AssetId(0x11111111), effect.SoundAssetId);

        var stream = asset.Streams[0];
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

        int ramSoundCount = soundEntries.Count(entry => entry[6] == 0); // SoundBankIndex byte
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
            [SoundBankEntry(0x11111111, (byte)SoundBankEntryFlags.None, 0, 0, 0),
             SoundBankEntry(0x22222222, (byte)SoundBankEntryFlags.Streaming, 0, 1, 0)],
            []);

        var asset = (SoundInfoAsset)Read(data, TSSMSerializer.DefaultProfile);

        Assert.Equal(2, asset.SoundBanks.Count);
        Assert.Equal(bank0, asset.SoundBanks[0]);
        Assert.Equal(bank1, asset.SoundBanks[1]);

        Assert.Equal(2, asset.Sounds.Count);
        Assert.Equal(new AssetId(0x11111111), asset.Sounds[0].SoundAssetId);
        Assert.Equal(SoundBankEntryFlags.None, asset.Sounds[0].Flags);
        Assert.Equal(0, asset.Sounds[0].SoundBankIndex);
        Assert.Equal(new AssetId(0x22222222), asset.Sounds[1].SoundAssetId);
        Assert.Equal(SoundBankEntryFlags.Streaming, asset.Sounds[1].Flags);
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
            [SoundBankEntry(0x11111111, (byte)SoundBankEntryFlags.None, 0, 0, 0),
             SoundBankEntry(0x22222222, (byte)SoundBankEntryFlags.Streaming, 0, 1, 0)],
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

    [Fact]
    public void Read_SoundInfo_UnderXbox_DegradesToUnparsedTail()
    {
        byte[] data = N100FData(0, 0);
        var profile = N100FSerializer.DefaultProfile with { Platform = Platform.Xbox };

        var asset = (SoundInfoAsset)Read(data, profile);

        Assert.Empty(asset.Effects);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Read_ThenWrite_SoundInfoUnderXbox_ReproducesInputBytes()
    {
        byte[] data = N100FData(1, 0, DspHeader(1000, 2020, 16000, looped: false, 0, 2020, 0x11111111));
        var profile = N100FSerializer.DefaultProfile with { Platform = Platform.Xbox };

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void EffectCount_DisagreeingWithEffects_IsStoredIndependently()
    {
        var asset = new SoundInfoAsset();
        asset.Effects.Add(new DspSoundHeader());

        asset.Physical.EffectCount = 5;

        Assert.Equal(5, asset.Physical.EffectCount);
        Assert.Single(asset.Effects);
    }

    [Fact]
    public void EffectCount_MatchingEffects_DerivesFromEffects()
    {
        var asset = new SoundInfoAsset();
        asset.Effects.Add(new DspSoundHeader());
        asset.Effects.Add(new DspSoundHeader());

        asset.Physical.EffectCount = 2;
        asset.Effects.Add(new DspSoundHeader());

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

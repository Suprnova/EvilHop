using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Describes every sound in a level: sample rate, ADPCM decoder state, and loop points for each
/// <see cref="AssetType.Sound"/>/<see cref="AssetType.StreamingSound"/> asset, or - on platforms where
/// sound data lives in this asset instead - the sounds themselves.
/// </summary>
/// <remarks>
/// Only the GameCube layout is modeled; every other platform round-trips through
/// <see cref="Asset.GetUnparsedTail"/>. On <see cref="GameVersion.N100F"/> and
/// <see cref="GameVersion.BFBB"/>, this asset holds one <see cref="DspSoundHeader"/> per SND/SNDS
/// asset. On every other GameCube-supported game, sounds are instead stored directly in this asset as
/// FMOD "FSB3" sample banks - see <see cref="SoundBanks"/> and <see cref="Sounds"/>.
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Sound_Format">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SoundInfoAsset() : Asset(AssetType.SoundInfo), IPhysicalSoundInfoAsset
{
    /// <summary>
    /// Headers for this level's sound effects, one per <see cref="AssetType.Sound"/> asset.
    /// </summary>
    /// <remarks>Populated for <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/> only.</remarks>
    public Collection<DspSoundHeader> Effects { get; } = [];

    /// <summary>
    /// Headers for this level's streaming sounds (voice lines and music), one per
    /// <see cref="AssetType.StreamingSound"/> asset.
    /// </summary>
    /// <remarks>Populated for <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/> only.</remarks>
    public Collection<DspSoundHeader> Streams { get; } = [];

    /// <summary>
    /// Headers linking to this level's cutscene audio, one per <see cref="AssetType.CutsceneStreamingSound"/>
    /// asset.
    /// </summary>
    /// <remarks>
    /// Populated for <see cref="GameVersion.BFBB"/>'s header-table layout, and for every game using
    /// the FSB3-embedded layout - both store cutscene headers in this same shape.
    /// </remarks>
    public Collection<DspSoundHeader> Cutscenes { get; } = [];

    /// <summary>
    /// The raw FMOD "FSB3" sample bank files embedded directly in this asset. The first bank is
    /// loaded into RAM in full and may hold multiple sounds; every subsequent bank is streamed from
    /// disk and holds exactly one sound.
    /// </summary>
    /// <remarks>
    /// EvilHop does not parse FSB3 - each entry is exactly the bytes of one bank file, byte-exact but
    /// opaque. Populated for every GameCube-supported game other than <see cref="GameVersion.N100F"/>
    /// and <see cref="GameVersion.BFBB"/>.
    /// </remarks>
    public Collection<byte[]> SoundBanks { get; } = [];

    /// <summary>
    /// Metadata for every sound stored across <see cref="SoundBanks"/>.
    /// </summary>
    /// <remarks>Populated alongside <see cref="SoundBanks"/>.</remarks>
    public Collection<SoundBankEntry> Sounds { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalSoundInfoAsset Physical => this;

    private AssetId? _overriddenSoundInfoId;
    AssetId IPhysicalSoundInfoAsset.SoundInfoId
    {
        get => _overriddenSoundInfoId ?? Id;
        set => _overriddenSoundInfoId = value == Id ? null : value;
    }

    private int? _overriddenEffectCount;
    int IPhysicalSoundInfoAsset.EffectCount
    {
        get => _overriddenEffectCount ?? Effects.Count;
        set => _overriddenEffectCount = value == Effects.Count ? null : value;
    }

    private int? _overriddenStreamCount;
    int IPhysicalSoundInfoAsset.StreamCount
    {
        get => _overriddenStreamCount ?? Streams.Count;
        set => _overriddenStreamCount = value == Streams.Count ? null : value;
    }

    private int? _overriddenCutsceneCount;
    int IPhysicalSoundInfoAsset.CutsceneCount
    {
        get => _overriddenCutsceneCount ?? Cutscenes.Count;
        set => _overriddenCutsceneCount = value == Cutscenes.Count ? null : value;
    }

    private int? _overriddenSoundBankCount;
    int IPhysicalSoundInfoAsset.SoundBankCount
    {
        get => _overriddenSoundBankCount ?? SoundBanks.Count;
        set => _overriddenSoundBankCount = value == SoundBanks.Count ? null : value;
    }

    private int? _overriddenSoundCount;
    int IPhysicalSoundInfoAsset.SoundCount
    {
        get => _overriddenSoundCount ?? Sounds.Count;
        set => _overriddenSoundCount = value == Sounds.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SoundInfo"/> is known to be read by.
    /// </summary>
    /// <remarks>
    /// Every one of these games also ships on Xbox and/or PlayStation 2, which use entirely different
    /// layouts EvilHop does not yet model - <see cref="Read"/> falls back to an unparsed blob unless
    /// <see cref="FormatProfile.Platform"/> is <see cref="Platform.GameCube"/>.
    /// </remarks>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static SoundInfoAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SoundInfoAsset();
        AssetFields.Populate(asset, header, debug);

        if (profile.Platform is not Platform.GameCube)
        {
            asset.SetUnparsedTail(reader.ReadRemainingBytes());
            return asset;
        }

        if (profile.Game is GameVersion.N100F or GameVersion.BFBB)
            ReadDspTable(asset, reader, profile);
        else
            ReadFsb3(asset, reader);

        return asset;
    }

    internal static void Write(SoundInfoAsset asset, EndianWriter writer, FormatProfile profile)
    {
        if (profile.Platform is not Platform.GameCube)
        {
            writer.Write(asset.GetUnparsedTail());
            return;
        }

        if (profile.Game is GameVersion.N100F or GameVersion.BFBB)
            WriteDspTable(asset, writer, profile);
        else
            WriteFsb3(asset, writer);
    }

    private static void ReadDspTable(SoundInfoAsset asset, EndianReader reader, FormatProfile profile)
    {
        int effectCount = reader.ReadInt32();
        reader.ReadInt32(); // padding, always 0xCDCDCDCD
        int streamCount = reader.ReadInt32();
        int cutsceneCount = profile.Game is GameVersion.BFBB ? reader.ReadInt32() : 0;

        for (int i = 0; i < effectCount; i++) asset.Effects.Add(ReadDspSoundHeader(reader));
        for (int i = 0; i < streamCount; i++) asset.Streams.Add(ReadDspSoundHeader(reader));
        for (int i = 0; i < cutsceneCount; i++) asset.Cutscenes.Add(ReadDspSoundHeader(reader));

        asset.Physical.EffectCount = asset.Effects.Count;
        asset.Physical.StreamCount = asset.Streams.Count;
        asset.Physical.CutsceneCount = asset.Cutscenes.Count;

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
    }

    private static void WriteDspTable(SoundInfoAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.EffectCount);
        writer.Write(unchecked((int)0xCDCDCDCD)); // padding
        writer.Write(asset.Physical.StreamCount);
        if (profile.Game is GameVersion.BFBB) writer.Write(asset.Physical.CutsceneCount);

        foreach (var effect in asset.Effects) WriteDspSoundHeader(writer, effect);
        foreach (var stream in asset.Streams) WriteDspSoundHeader(writer, stream);
        foreach (var cutscene in asset.Cutscenes) WriteDspSoundHeader(writer, cutscene);

        writer.Write(asset.GetUnparsedTail());
    }

    private static DspSoundHeader ReadDspSoundHeader(EndianReader reader)
    {
        var header = new DspSoundHeader
        {
            SampleCount = reader.ReadUInt32(),
            NibbleCount = reader.ReadUInt32(),
            SampleRate = reader.ReadUInt32(),
            IsLooped = reader.ReadInt16() != 0,
            Format = (ushort)reader.ReadInt16(),
            LoopStart = reader.ReadUInt32(),
            LoopEnd = reader.ReadUInt32(),
            InitialOffset = reader.ReadUInt32(),
        };
        for (int i = 0; i < header.Coefficients.Count; i++) header.Coefficients[i] = reader.ReadInt16();
        header.Gain = (ushort)reader.ReadInt16();
        header.PredictorScale = (ushort)reader.ReadInt16();
        header.History1 = reader.ReadInt16();
        header.History2 = reader.ReadInt16();
        header.LoopPredictorScale = (ushort)reader.ReadInt16();
        header.LoopHistory1 = reader.ReadInt16();
        header.LoopHistory2 = reader.ReadInt16();
        for (int i = 0; i < header.Unknown.Count; i++) header.Unknown[i] = reader.ReadByte();
        header.SoundAssetId = reader.ReadAssetId();
        return header;
    }

    private static void WriteDspSoundHeader(EndianWriter writer, DspSoundHeader header)
    {
        writer.Write(header.SampleCount);
        writer.Write(header.NibbleCount);
        writer.Write(header.SampleRate);
        writer.Write((short)(header.IsLooped ? 1 : 0));
        writer.Write((short)header.Format);
        writer.Write(header.LoopStart);
        writer.Write(header.LoopEnd);
        writer.Write(header.InitialOffset);
        foreach (short coefficient in header.Coefficients) writer.Write(coefficient);
        writer.Write((short)header.Gain);
        writer.Write((short)header.PredictorScale);
        writer.Write(header.History1);
        writer.Write(header.History2);
        writer.Write((short)header.LoopPredictorScale);
        writer.Write(header.LoopHistory1);
        writer.Write(header.LoopHistory2);
        foreach (byte b in header.Unknown) writer.Write(b);
        writer.Write(header.SoundAssetId);
    }

    private static void ReadFsb3(SoundInfoAsset asset, EndianReader reader)
    {
        asset.Physical.SoundInfoId = reader.ReadAssetId();
        uint footerOffset = reader.ReadUInt32(); // relative to the end of this header
        reader.ReadBytes(16); // pFMusicMod/pFSBFileArray/pWavInfoArray/pCutsceneAudioHeaders, always null
        int soundCount = (ushort)reader.ReadInt16();
        reader.ReadInt16(); // nSounds, a subset count of Sounds recomputed from Flags on write
        reader.ReadInt16(); // nStreams, ditto
        int soundBankCount = reader.ReadByte();
        int cutsceneCount = reader.ReadByte();

        long headerEnd = reader.BaseStream.Position;

        // The FSB3 offsets array, gcWavInfo array, and cutscene headers all live in the footer, at
        // the end of the asset - jump there before the FSB3 banks they describe, which come right
        // after this header.
        reader.BaseStream.Position = headerEnd + footerOffset;

        var bankOffsets = new uint[soundBankCount];
        for (int i = 0; i < soundBankCount; i++) bankOffsets[i] = reader.ReadUInt32();

        for (int i = 0; i < soundCount; i++)
        {
            asset.Sounds.Add(new SoundBankEntry
            {
                SoundAssetId = reader.ReadAssetId(),
                Flags = (SoundBankEntryFlags)reader.ReadByte(),
                SampleIndex = reader.ReadByte(),
                SoundBankIndex = reader.ReadByte(),
                SoundInfoIndex = reader.ReadByte(),
            });
        }

        for (int i = 0; i < cutsceneCount; i++) asset.Cutscenes.Add(ReadDspSoundHeader(reader));

        long footerEnd = reader.BaseStream.Position;

        for (int i = 0; i < soundBankCount; i++)
        {
            uint end = i + 1 < soundBankCount ? bankOffsets[i + 1] : footerOffset;
            reader.BaseStream.Position = headerEnd + bankOffsets[i];
            asset.SoundBanks.Add(reader.ReadBytes((int)(end - bankOffsets[i])));
        }

        asset.Physical.SoundBankCount = asset.SoundBanks.Count;
        asset.Physical.SoundCount = asset.Sounds.Count;
        asset.Physical.CutsceneCount = asset.Cutscenes.Count;

        reader.BaseStream.Position = footerEnd;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
    }

    private static void WriteFsb3(SoundInfoAsset asset, EndianWriter writer)
    {
        var bankOffsets = new uint[asset.SoundBanks.Count];
        uint footerOffset = 0;
        for (int i = 0; i < asset.SoundBanks.Count; i++)
        {
            bankOffsets[i] = footerOffset;
            footerOffset += (uint)asset.SoundBanks[i].Length;
        }

        writer.Write(asset.Physical.SoundInfoId);
        writer.Write(footerOffset);
        writer.Write(new byte[16]); // pFMusicMod/pFSBFileArray/pWavInfoArray/pCutsceneAudioHeaders, always null
        writer.Write((short)asset.Physical.SoundCount);
        writer.Write((short)asset.Sounds.Count(sound => !sound.Flags.HasFlag(SoundBankEntryFlags.Streaming))); // nSounds
        writer.Write((short)asset.Sounds.Count(sound => sound.Flags.HasFlag(SoundBankEntryFlags.Streaming))); // nStreams
        writer.Write((byte)asset.Physical.SoundBankCount);
        writer.Write((byte)asset.Physical.CutsceneCount);

        foreach (byte[] bank in asset.SoundBanks) writer.Write(bank);

        foreach (uint offset in bankOffsets) writer.Write(offset);
        foreach (var sound in asset.Sounds)
        {
            writer.Write(sound.SoundAssetId);
            writer.Write((byte)sound.Flags);
            writer.Write(sound.SampleIndex);
            writer.Write(sound.SoundBankIndex);
            writer.Write(sound.SoundInfoIndex);
        }
        foreach (var cutscene in asset.Cutscenes) WriteDspSoundHeader(writer, cutscene);

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="SoundInfoAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalSoundInfoAsset : IPhysicalAsset
{
    /// <summary>
    /// This asset's own ID, read directly from its FSB3-embedded layout's header.
    /// </summary>
    /// <remarks>
    /// Only meaningful for the FSB3-embedded layout. When disagreements with <see cref="Asset.Id"/>
    /// exist, this field wins during serialization.
    /// </remarks>
    AssetId SoundInfoId { get; set; }

    /// <summary>
    /// The number of <see cref="SoundInfoAsset.Effects"/> stored for this asset, read directly from
    /// its leading count field.
    /// </summary>
    /// <remarks>
    /// Only meaningful for the header-table layout. When disagreements with
    /// <see cref="SoundInfoAsset.Effects"/>.Count exist, this field wins during serialization.
    /// </remarks>
    int EffectCount { get; set; }

    /// <summary>
    /// The number of <see cref="SoundInfoAsset.Streams"/> stored for this asset, read directly from
    /// its leading count field.
    /// </summary>
    /// <remarks>
    /// Only meaningful for the header-table layout. When disagreements with
    /// <see cref="SoundInfoAsset.Streams"/>.Count exist, this field wins during serialization.
    /// </remarks>
    int StreamCount { get; set; }

    /// <summary>
    /// The number of <see cref="SoundInfoAsset.Cutscenes"/> stored for this asset, read directly from
    /// its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="SoundInfoAsset.Cutscenes"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    int CutsceneCount { get; set; }

    /// <summary>
    /// The number of <see cref="SoundInfoAsset.SoundBanks"/> stored for this asset, read directly
    /// from its FSB3-embedded layout's header.
    /// </summary>
    /// <remarks>
    /// Only meaningful for the FSB3-embedded layout. When disagreements with
    /// <see cref="SoundInfoAsset.SoundBanks"/>.Count exist, this field wins during serialization.
    /// </remarks>
    int SoundBankCount { get; set; }

    /// <summary>
    /// The number of <see cref="SoundInfoAsset.Sounds"/> stored for this asset, read directly from
    /// its FSB3-embedded layout's header.
    /// </summary>
    /// <remarks>
    /// Only meaningful for the FSB3-embedded layout. When disagreements with
    /// <see cref="SoundInfoAsset.Sounds"/>.Count exist, this field wins during serialization.
    /// </remarks>
    int SoundCount { get; set; }
}

/// <summary>
/// A Nintendo DSP-ADPCM sound header, describing one sound's sample rate, decoder coefficients, and
/// loop points, and linking it to its <see cref="AssetType.Sound"/>, <see cref="AssetType.StreamingSound"/>,
/// or <see cref="AssetType.CutsceneStreamingSound"/> asset.
/// </summary>
public sealed class DspSoundHeader
{
    /// <summary>The number of raw (decoded) samples in the sound.</summary>
    public uint SampleCount { get; set; }

    /// <summary>The number of ADPCM nibbles in the sound, including frame headers.</summary>
    public uint NibbleCount { get; set; }

    /// <summary>The sound's sample rate, in Hz.</summary>
    public uint SampleRate { get; set; }

    /// <summary>Whether the sound loops.</summary>
    public bool IsLooped { get; set; }

    /// <summary>Unknown. Always 0 in every sample checked so far.</summary>
    public ushort Format { get; set; }

    /// <summary>The loop start offset, in nibbles.</summary>
    public uint LoopStart { get; set; }

    /// <summary>The loop end offset, in nibbles.</summary>
    public uint LoopEnd { get; set; }

    /// <summary>The decoder's initial offset value. Always 2 in every sample checked so far.</summary>
    public uint InitialOffset { get; set; }

    /// <summary>The sound's 16 ADPCM decoder coefficients.</summary>
    public Collection<short> Coefficients { get; } = new([.. new short[16]]);

    /// <summary>Unknown gain factor. Always 0 in every sample checked so far.</summary>
    public ushort Gain { get; set; }

    /// <summary>
    /// The predictor and scale value of the sound's first ADPCM frame, used to initialize the
    /// decoder.
    /// </summary>
    public ushort PredictorScale { get; set; }

    /// <summary>Decoder history data, used to maintain decoder state during sample playback.</summary>
    public short History1 { get; set; }

    /// <inheritdoc cref="History1"/>
    public short History2 { get; set; }

    /// <summary>
    /// The predictor and scale value for the loop point's ADPCM frame. Zero if <see cref="IsLooped"/>
    /// is <see langword="false"/>.
    /// </summary>
    public ushort LoopPredictorScale { get; set; }

    /// <summary>
    /// Decoder history data for the loop point. Zero if <see cref="IsLooped"/> is
    /// <see langword="false"/>.
    /// </summary>
    public short LoopHistory1 { get; set; }

    /// <inheritdoc cref="LoopHistory1"/>
    public short LoopHistory2 { get; set; }

    /// <summary>Unknown values.</summary>
    public Collection<byte> Unknown { get; } = new([.. new byte[22]]);

    /// <summary>
    /// The <see cref="AssetType.Sound"/>, <see cref="AssetType.StreamingSound"/>, or
    /// <see cref="AssetType.CutsceneStreamingSound"/> asset this header describes.
    /// </summary>
    public AssetId SoundAssetId { get; set; }
}

/// <summary>
/// Metadata for one sound stored in a <see cref="SoundInfoAsset"/>'s FSB3-embedded
/// <see cref="SoundInfoAsset.SoundBanks"/>.
/// </summary>
public sealed class SoundBankEntry
{
    /// <summary>
    /// The <see cref="AssetType.Sound"/> or <see cref="AssetType.StreamingSound"/> asset this entry
    /// describes.
    /// </summary>
    public AssetId SoundAssetId { get; set; }

    /// <summary>Whether the sound loops, and whether it is streamed rather than RAM-resident.</summary>
    public SoundBankEntryFlags Flags { get; set; }

    /// <summary>The zero-based index of the sound within its <see cref="SoundBankIndex"/> bank.</summary>
    public byte SampleIndex { get; set; }

    /// <summary>
    /// The zero-based index, into <see cref="SoundInfoAsset.SoundBanks"/>, of the bank containing
    /// this sound.
    /// </summary>
    public byte SoundBankIndex { get; set; }

    /// <summary>Unknown purpose.</summary>
    public byte SoundInfoIndex { get; set; }
}

/// <summary>Flags for one <see cref="SoundBankEntry"/>.</summary>
[Flags]
public enum SoundBankEntryFlags : byte
{
    /// <summary>A RAM-resident, non-looping sound.</summary>
    None = 0,

    /// <summary>The sound loops.</summary>
    Looped = 1 << 0,

    /// <summary>The sound is streamed from disk rather than RAM-resident.</summary>
    Streaming = 1 << 1,
}

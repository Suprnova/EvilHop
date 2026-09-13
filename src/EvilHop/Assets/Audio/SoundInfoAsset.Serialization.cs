using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class SoundInfoAsset
{
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

    // todo: we're discarding nSounds and nStreams and recomputing them. bad?
    private static void ReadFsb3(SoundInfoAsset asset, EndianReader reader)
    {
        asset.Physical.SoundInfoId = reader.ReadAssetId();
        uint footerOffset = reader.ReadUInt32(); // relative to the end of this header
        reader.ReadBytes(16); // runtime-resolved pFMusicMod/pFSBFileArray/pWavInfoArray/pCutsceneAudioHeaders, always null
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
        writer.Write(new byte[16]); // runtime-resolved
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

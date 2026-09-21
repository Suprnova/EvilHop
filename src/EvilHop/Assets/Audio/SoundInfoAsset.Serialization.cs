using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using static EvilHop.Assets.SoundInfoAsset.SoundBankEntry;

namespace EvilHop.Assets;

public sealed partial class SoundInfoAsset
{
    internal static SoundInfoAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SoundInfoAsset();
        AssetFields.Populate(asset, header, debug);

        // TODO: Partial implementation - non-GameCube platforms are not implemented
        if (profile.Platform is not Platform.GameCube)
        {
            asset.SetUnparsedTail(reader.ReadRemainingBytes());
            return asset;
        }

        if (profile.Game is GameVersion.N100F or GameVersion.BFBB)
            ReadDspTable(asset, reader, profile);
        else
            ReadFsb3(asset, reader, profile);

        return asset;
    }

    internal static void Write(SoundInfoAsset asset, EndianWriter writer, FormatProfile profile)
    {
        // TODO: Partial implementation - non-GameCube platforms are not implemented
        if (profile.Platform is not Platform.GameCube)
        {
            writer.Write(asset.GetUnparsedTail());
            return;
        }

        if (profile.Game is GameVersion.N100F or GameVersion.BFBB)
            WriteDspTable(asset, writer, profile);
        else
            WriteFsb3(asset, writer, profile);
    }

    private static void ReadDspTable(SoundInfoAsset asset, EndianReader reader, FormatProfile profile)
    {
        int effectCount = reader.ReadInt32();
        reader.ReadInt32(); // padding, always 0xCDCDCDCD
        int streamCount = reader.ReadInt32();
        int cutsceneCount = profile.Game is GameVersion.BFBB ? reader.ReadInt32() : 0;

        for (int i = 0; i < effectCount; i++) asset.Effects.Add(DspSoundHeader.Read(reader, profile));
        for (int i = 0; i < streamCount; i++) asset.Streams.Add(DspSoundHeader.Read(reader, profile));
        for (int i = 0; i < cutsceneCount; i++) asset.Cutscenes.Add(DspSoundHeader.Read(reader, profile));

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

        foreach (var effect in asset.Effects) DspSoundHeader.Write(effect, writer, profile);
        foreach (var stream in asset.Streams) DspSoundHeader.Write(stream, writer, profile);
        foreach (var cutscene in asset.Cutscenes) DspSoundHeader.Write(cutscene, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }

    private static void ReadFsb3(SoundInfoAsset asset, EndianReader reader, FormatProfile profile)
    {
        asset.Physical.SoundInfoId = reader.ReadAssetId();
        uint footerOffset = reader.ReadUInt32(); // relative to the end of this header
        reader.ReadBytes(16); // runtime-resolved pFMusicMod/pFSBFileArray/pWavInfoArray/pCutsceneAudioHeaders, always null
        int soundCount = reader.ReadUInt16();
        reader.ReadUInt16(); // nSounds, a subset count of Sounds recomputed from Flags on write
        reader.ReadUInt16(); // nStreams, ditto
        int soundBankCount = reader.ReadByte();
        int cutsceneCount = reader.ReadByte();

        long headerEnd = reader.BaseStream.Position;

        // The FSB3 offsets array, gcWavInfo array, and cutscene headers all live in the footer, at
        // the end of the asset - jump there before the FSB3 banks they describe, which come right
        // after this header.
        reader.BaseStream.Position = headerEnd + footerOffset;

        var bankOffsets = new uint[soundBankCount];
        for (int i = 0; i < soundBankCount; i++) bankOffsets[i] = reader.ReadUInt32();

        for (int i = 0; i < soundCount; i++) asset.Sounds.Add(SoundBankEntry.Read(reader, profile));

        for (int i = 0; i < cutsceneCount; i++) asset.Cutscenes.Add(DspSoundHeader.Read(reader, profile));

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

    private static void WriteFsb3(SoundInfoAsset asset, EndianWriter writer, FormatProfile profile)
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
        writer.Write((ushort)asset.Physical.SoundCount);
        writer.Write((ushort)asset.Sounds.Count(sound => !sound.Flags.HasFlag(SoundBankEntryFlags.Streaming))); // nSounds
        writer.Write((ushort)asset.Sounds.Count(sound => sound.Flags.HasFlag(SoundBankEntryFlags.Streaming))); // nStreams
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
        foreach (var cutscene in asset.Cutscenes) DspSoundHeader.Write(cutscene, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

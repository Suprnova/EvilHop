using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using static EvilHop.Assets.SoundInfoAsset.Sound;

namespace EvilHop.Assets;

public sealed partial class SoundInfoAsset
{
    internal static SoundInfoAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SoundInfoAsset();
        AssetFields.Populate(asset, header, debug);

        if (HasSoundBanks(profile))
            ReadFsb3(asset, reader, profile);
        else
            ReadHeaderTable(asset, reader, profile);

        return asset;
    }

    internal static void Write(SoundInfoAsset asset, EndianWriter writer, FormatProfile profile)
    {
        if (HasSoundBanks(profile))
            WriteFsb3(asset, writer, profile);
        else
            WriteHeaderTable(asset, writer, profile);
    }

    private static bool HasSoundBanks(FormatProfile profile) =>
        profile.Platform is Platform.GameCube && profile.Game is not (GameVersion.N100F or GameVersion.BFBB);

    private static bool HasSoundInfoId(FormatProfile profile) =>
        profile.Platform is Platform.PlayStation2 && profile.Game is GameVersion.Incredibles or GameVersion.ROTU;

    private static bool HasCutscenes(FormatProfile profile) =>
        profile.Platform is Platform.Xbox || (profile.Platform is Platform.GameCube && profile.Game is GameVersion.BFBB);

    private static void ReadHeaderTable(SoundInfoAsset asset, EndianReader reader, FormatProfile profile)
    {
        if (HasSoundInfoId(profile)) asset.Physical.SoundInfoId = reader.ReadAssetId();
        int effectCount = reader.ReadInt32();
        if (profile.Platform is Platform.GameCube) reader.ReadInt32(); // padding, always 0xCDCDCDCD
        int streamCount = reader.ReadInt32();
        int cutsceneCount = HasCutscenes(profile) ? reader.ReadInt32() : 0;

        for (int i = 0; i < effectCount; i++) asset.Effects.Add(SoundHeader.Read(reader, profile));
        for (int i = 0; i < streamCount; i++) asset.Streams.Add(SoundHeader.Read(reader, profile));
        for (int i = 0; i < cutsceneCount; i++) asset.Cutscenes.Add(SoundHeader.Read(reader, profile));

        asset.Physical.EffectCount = asset.Effects.Count;
        asset.Physical.StreamCount = asset.Streams.Count;
        asset.Physical.CutsceneCount = asset.Cutscenes.Count;

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
    }

    private static void WriteHeaderTable(SoundInfoAsset asset, EndianWriter writer, FormatProfile profile)
    {
        if (HasSoundInfoId(profile)) writer.Write(asset.Physical.SoundInfoId);
        writer.Write(asset.Physical.EffectCount);
        if (profile.Platform is Platform.GameCube) writer.Write(unchecked((int)0xCDCDCDCD)); // padding
        writer.Write(asset.Physical.StreamCount);
        if (HasCutscenes(profile)) writer.Write(asset.Physical.CutsceneCount);

        foreach (var effect in asset.Effects) SoundHeader.Write(effect, writer, profile);
        foreach (var stream in asset.Streams) SoundHeader.Write(stream, writer, profile);
        foreach (var cutscene in asset.Cutscenes) SoundHeader.Write(cutscene, writer, profile);

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

        for (int i = 0; i < soundCount; i++) asset.Sounds.Add(Sound.Read(reader, profile));

        for (int i = 0; i < cutsceneCount; i++) asset.Cutscenes.Add(SoundHeader.Read(reader, profile));

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
        writer.Write((ushort)asset.Sounds.Count(sound => !sound.Flags.HasFlag(Identity.Streaming))); // nSounds
        writer.Write((ushort)asset.Sounds.Count(sound => sound.Flags.HasFlag(Identity.Streaming))); // nStreams
        writer.Write((byte)asset.Physical.SoundBankCount);
        writer.Write((byte)asset.Physical.CutsceneCount);

        foreach (byte[] bank in asset.SoundBanks) writer.Write(bank);

        foreach (uint offset in bankOffsets) writer.Write(offset);
        foreach (var sound in asset.Sounds) Sound.Write(sound, writer, profile);
        foreach (var cutscene in asset.Cutscenes) SoundHeader.Write(cutscene, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

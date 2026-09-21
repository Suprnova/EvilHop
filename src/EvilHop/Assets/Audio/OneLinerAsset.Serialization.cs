using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class OneLinerAsset
{
    internal static OneLinerAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new OneLinerAsset();
        AssetFields.Populate(asset, header, debug);

        uint entryCount = reader.ReadUInt32();
        for (int i = 0; i < entryCount; i++)
            asset.Entries.Add(ReadEntry(reader));

        asset.Physical.EntryCount = (uint)asset.Entries.Count;
        // TODO: Partial implementation - trailing 67-byte trailer is not modeled
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(OneLinerAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write(asset.Physical.EntryCount);

        foreach (var entry in asset.Entries)
            WriteEntry(writer, entry);

        writer.Write(asset.GetUnparsedTail());
    }

    private static OneLinerEntry ReadEntry(EndianReader reader)
    {
        var entry = new OneLinerEntry
        {
            SoundGroupId = reader.ReadAssetId(),
            SoundStartDelay = reader.ReadSingle(),
            TimeSpan = reader.ReadSingle(),
            TimeLastPlayed = reader.ReadSingle(),
            NumPlays = reader.ReadUInt32(),
            DelayBetweenPlays = reader.ReadSingle(),
            Probability = reader.ReadSingle(),
            DefaultDuration = reader.ReadSingle(),
            LastDuration = reader.ReadSingle(),
            MaxPlays = reader.ReadUInt32(),
        };
        reader.ReadUInt32(); // m_soundGroupHandle, runtime-resolved, always zero
        reader.ReadUInt32(); // m_pOLManager, runtime-resolved back-pointer, always zero
        entry.EventType = reader.ReadInt16();
        entry.PlaysInMusicChannel = reader.ReadInt16() != 0;
        reader.ReadUInt32(); // m_pData, runtime-resolved per-PlayerType data pointer, always zero
        entry.PlayerType = (OneLinerPlayerType)reader.ReadInt32();
        entry.FirstParam = reader.ReadInt32();
        entry.SecondParam = reader.ReadSingle();
        return entry;
    }

    private static void WriteEntry(EndianWriter writer, OneLinerEntry entry)
    {
        writer.Write(entry.SoundGroupId);
        writer.Write(entry.SoundStartDelay);
        writer.Write(entry.TimeSpan);
        writer.Write(entry.TimeLastPlayed);
        writer.Write(entry.NumPlays);
        writer.Write(entry.DelayBetweenPlays);
        writer.Write(entry.Probability);
        writer.Write(entry.DefaultDuration);
        writer.Write(entry.LastDuration);
        writer.Write(entry.MaxPlays);
        writer.Write(0u); // m_soundGroupHandle
        writer.Write(0u); // m_pOLManager
        writer.Write(entry.EventType);
        writer.Write((short)(entry.PlaysInMusicChannel ? 1 : 0));
        writer.Write(0u); // m_pData
        writer.Write((int)entry.PlayerType);
        writer.Write(entry.FirstParam);
        writer.Write(entry.SecondParam);
    }
}

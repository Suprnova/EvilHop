using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Serialization;

public partial class Serializer
{
    /// <summary>
    /// Reads the fields of an <see cref="AssetInf"/> (AINF) block.
    /// </summary>
    protected static void ReadAssetInf(EndianReader reader, AssetInf block, uint size)
    {
        block.Value = reader.ReadUInt32();
    }

    /// <summary>
    /// Writes the fields of an <see cref="AssetInf"/> (AINF) block.
    /// </summary>
    protected static void WriteAssetInf(EndianWriter writer, AssetInf block) =>
        writer.Write(block.Value);

    /// <summary>
    /// Reads the fields of an <see cref="AssetHeader"/> (AHDR) block.
    /// </summary>
    protected static void ReadAssetHeader(EndianReader reader, AssetHeader block, uint size)
    {
        block.Id = reader.ReadUInt32();
        block.Type = (AssetType)reader.ReadUInt32();
        block.Offset = reader.ReadUInt32();
        block.Size = reader.ReadUInt32();
        block.Plus = reader.ReadUInt32();
        block.Flags = (AssetFlags)reader.ReadUInt32();
    }

    /// <summary>
    /// Writes the fields of an <see cref="AssetHeader"/> (AHDR) block.
    /// </summary>
    protected static void WriteAssetHeader(EndianWriter writer, AssetHeader block)
    {
        writer.Write(block.Id);
        writer.Write((uint)block.Type);
        writer.Write(block.Offset);
        writer.Write(block.Size);
        writer.Write(block.Plus);
        writer.Write((uint)block.Flags);
    }

    /// <summary>
    /// Reads the fields of an <see cref="AssetDebug"/> (ADBG) block.
    /// </summary>
    protected static void ReadAssetDebug(EndianReader reader, AssetDebug block, uint size)
    {
        block.Alignment = reader.ReadInt32(); // intentionally signed
        block.Name = reader.ReadEvilString();
        block.FileName = reader.ReadEvilString();
        block.Checksum = reader.ReadUInt32();
    }

    /// <summary>
    /// Writes the fields of an <see cref="AssetDebug"/> (ADBG) block.
    /// </summary>
    protected static void WriteAssetDebug(EndianWriter writer, AssetDebug block)
    {
        writer.Write(block.Alignment);
        writer.WriteEvilString(block.Name);
        writer.WriteEvilString(block.FileName);
        writer.Write(block.Checksum);
    }

    /// <summary>
    /// Reads the fields of a <see cref="LayerInf"/> (LINF) block.
    /// </summary>
    protected static void ReadLayerInf(EndianReader reader, LayerInf block, uint size)
    {
        block.Value = reader.ReadUInt32();
    }

    /// <summary>
    /// Writes the fields of a <see cref="LayerInf"/> (LINF) block.
    /// </summary>
    protected static void WriteLayerInf(EndianWriter writer, LayerInf block) =>
        writer.Write(block.Value);

    /// <summary>
    /// Reads the fields of a <see cref="LayerHeader"/> (LHDR) block.
    /// </summary>
    protected static void ReadLayerHeader(EndianReader reader, LayerHeader block, uint size)
    {
        block.Type = (LayerType)reader.ReadUInt32();

        uint assetCount = reader.ReadUInt32();
        block.AssetCount = assetCount;

        var assetIds = new uint[assetCount];
        for (int i = 0; i < assetCount; i++)
            assetIds[i] = reader.ReadUInt32();
        block.AssetIds = assetIds;
    }

    /// <summary>
    /// Writes the fields of a <see cref="LayerHeader"/> (LHDR) block.
    /// </summary>
    protected static void WriteLayerHeader(EndianWriter writer, LayerHeader block)
    {
        writer.Write((uint)block.Type);
        writer.Write(block.AssetCount);
        foreach (uint id in block.AssetIds)
            writer.Write(id);
    }

    /// <summary>
    /// Reads the fields of a <see cref="LayerDebug"/> (LDBG) block.
    /// </summary>
    protected static void ReadLayerDebug(EndianReader reader, LayerDebug block, uint size)
    {
        block.Value = reader.ReadUInt32();
    }

    /// <summary>
    /// Writes the fields of a <see cref="LayerDebug"/> (LDBG) block.
    /// </summary>
    protected static void WriteLayerDebug(EndianWriter writer, LayerDebug block) =>
        writer.Write(block.Value);
}

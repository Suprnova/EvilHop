using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using System.Buffers.Binary;

namespace EvilHop.Serialization;

public partial class Serializer
{
    /// <summary>
    /// Reads the fields of an <see cref="AssetInf"/> (AINF) block.
    /// </summary>
    protected static void ReadAssetInf(BinaryReader reader, AssetInf block, uint size)
    {
        block.Value = reader.ReadEvilInt();
    }

    /// <summary>
    /// Writes the fields of an <see cref="AssetInf"/> (AINF) block.
    /// </summary>
    protected static void WriteAssetInf(BinaryWriter writer, AssetInf block) =>
        writer.WriteEvilInt(block.Value);

    /// <summary>
    /// Reads the fields of an <see cref="AssetHeader"/> (AHDR) block.
    /// </summary>
    protected static void ReadAssetHeader(BinaryReader reader, AssetHeader block, uint size)
    {
        block.Id = reader.ReadEvilInt();
        block.Type = (AssetType)reader.ReadEvilInt();
        block.Offset = reader.ReadEvilInt();
        block.Size = reader.ReadEvilInt();
        block.Plus = reader.ReadEvilInt();
        block.Flags = (AssetFlags)reader.ReadEvilInt();
    }

    /// <summary>
    /// Writes the fields of an <see cref="AssetHeader"/> (AHDR) block.
    /// </summary>
    protected static void WriteAssetHeader(BinaryWriter writer, AssetHeader block)
    {
        writer.WriteEvilInt(block.Id);
        writer.WriteEvilInt((uint)block.Type);
        writer.WriteEvilInt(block.Offset);
        writer.WriteEvilInt(block.Size);
        writer.WriteEvilInt(block.Plus);
        writer.WriteEvilInt((uint)block.Flags);
    }

    /// <summary>
    /// Reads the fields of an <see cref="AssetDebug"/> (ADBG) block. <see cref="AssetDebug.Alignment"/>
    /// is signed, unlike every other integer field in the block layer.
    /// </summary>
    protected static void ReadAssetDebug(BinaryReader reader, AssetDebug block, uint size)
    {
        block.Alignment = BinaryPrimitives.ReadInt32BigEndian(reader.ReadBytes(4));
        block.Name = reader.ReadEvilString();
        block.FileName = reader.ReadEvilString();
        block.Checksum = reader.ReadEvilInt();
    }

    /// <summary>
    /// Writes the fields of an <see cref="AssetDebug"/> (ADBG) block.
    /// </summary>
    protected static void WriteAssetDebug(BinaryWriter writer, AssetDebug block)
    {
        Span<byte> alignment = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(alignment, block.Alignment);
        writer.Write(alignment);

        writer.WriteEvilString(block.Name);
        writer.WriteEvilString(block.FileName);
        writer.WriteEvilInt(block.Checksum);
    }

    /// <summary>
    /// Reads the fields of a <see cref="LayerInf"/> (LINF) block.
    /// </summary>
    protected static void ReadLayerInf(BinaryReader reader, LayerInf block, uint size)
    {
        block.Value = reader.ReadEvilInt();
    }

    /// <summary>
    /// Writes the fields of a <see cref="LayerInf"/> (LINF) block.
    /// </summary>
    protected static void WriteLayerInf(BinaryWriter writer, LayerInf block) =>
        writer.WriteEvilInt(block.Value);

    /// <summary>
    /// Reads the fields of a <see cref="LayerHeader"/> (LHDR) block, including the
    /// <see cref="LayerHeader.AssetCount"/>-driven <see cref="LayerHeader.AssetIds"/> array.
    /// </summary>
    protected static void ReadLayerHeader(BinaryReader reader, LayerHeader block, uint size)
    {
        block.Type = (LayerType)reader.ReadEvilInt();

        uint assetCount = reader.ReadEvilInt();
        block.AssetCount = assetCount;

        var assetIds = new uint[assetCount];
        for (int i = 0; i < assetCount; i++)
            assetIds[i] = reader.ReadEvilInt();
        block.AssetIds = assetIds;
    }

    /// <summary>
    /// Writes the fields of a <see cref="LayerHeader"/> (LHDR) block. <see cref="LayerHeader.AssetCount"/>
    /// and <see cref="LayerHeader.AssetIds"/> are written independently, exactly as stored - a
    /// mismatch between the two is a permitted invalid state, left to <c>Validate()</c> to flag.
    /// </summary>
    protected static void WriteLayerHeader(BinaryWriter writer, LayerHeader block)
    {
        writer.WriteEvilInt((uint)block.Type);
        writer.WriteEvilInt(block.AssetCount);
        foreach (uint id in block.AssetIds)
            writer.WriteEvilInt(id);
    }

    /// <summary>
    /// Reads the fields of a <see cref="LayerDebug"/> (LDBG) block.
    /// </summary>
    protected static void ReadLayerDebug(BinaryReader reader, LayerDebug block, uint size)
    {
        block.Value = reader.ReadEvilInt();
    }

    /// <summary>
    /// Writes the fields of a <see cref="LayerDebug"/> (LDBG) block.
    /// </summary>
    protected static void WriteLayerDebug(BinaryWriter writer, LayerDebug block) =>
        writer.WriteEvilInt(block.Value);
}

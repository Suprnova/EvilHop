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
    /// Reads the fields of a <see cref="LayerInf"/> (LINF) block.
    /// </summary>
    protected static void ReadLayerInf(BinaryReader reader, LayerInf block, uint size)
    {
        block.Value = reader.ReadEvilInt();
    }

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
    /// Reads the fields of a <see cref="LayerDebug"/> (LDBG) block.
    /// </summary>
    protected static void ReadLayerDebug(BinaryReader reader, LayerDebug block, uint size)
    {
        block.Value = reader.ReadEvilInt();
    }
}

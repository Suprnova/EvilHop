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
    /// <see cref="LayerType"/>'s own numeric values, in the order N100F stores them on disk - it has
    /// no <see cref="LayerType.TextureStream"/> or <see cref="LayerType.JSPInfo"/>, so every value
    /// from <see cref="LayerType.BSP"/> on is shifted down two from <see cref="LayerType"/>'s numbering.
    /// </summary>
    private static readonly LayerType[] N100FLayerTypeOrder =
    [
        LayerType.Default, LayerType.Texture, LayerType.BSP, LayerType.Model, LayerType.Animation,
        LayerType.VRAM, LayerType.SRAM, LayerType.SoundTable, LayerType.Cutscene, LayerType.CutsceneTable
    ];

    /// <summary>
    /// BFBB's on-disk order: <see cref="N100FLayerTypeOrder"/> plus a <see cref="LayerType.JSPInfo"/>
    /// BFBB has that N100F doesn't, still with no <see cref="LayerType.TextureStream"/>.
    /// </summary>
    private static readonly LayerType[] BFBBLayerTypeOrder = [.. N100FLayerTypeOrder, LayerType.JSPInfo];

    /// <summary>
    /// The on-disk <see cref="LayerType"/> order for <paramref name="game"/>, or <see langword="null"/>
    /// for a game whose order already matches <see cref="LayerType"/>'s own numbering.
    /// </summary>
    private static LayerType[]? LayerTypeOrder(GameVersion game) => game switch
    {
        GameVersion.N100F => N100FLayerTypeOrder,
        GameVersion.BFBB => BFBBLayerTypeOrder,
        _ => null
    };

    /// <summary>
    /// Reads the fields of a <see cref="LayerHeader"/> (LHDR) block.
    /// </summary>
    protected void ReadLayerHeader(EndianReader reader, LayerHeader block, uint size)
    {
        uint raw = reader.ReadUInt32();
        var order = LayerTypeOrder(Profile.Game);
        block.Type = order != null && raw < order.Length ? order[raw] : (LayerType)raw;

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
    protected void WriteLayerHeader(EndianWriter writer, LayerHeader block)
    {
        var order = LayerTypeOrder(Profile.Game);
        int index = order != null ? Array.IndexOf(order, block.Type) : -1;
        writer.Write(index >= 0 ? (uint)index : (uint)block.Type);

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

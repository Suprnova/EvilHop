using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V1Serializer
{
    protected virtual Dictionary InitDictionary()
    {
        return new Dictionary(
            NewBlock<AssetTable>(),
            NewBlock<LayerTable>()
        );
    }

    protected virtual AssetTable InitAssetTable()
    {
        return new AssetTable(
            NewBlock<AssetInf>(),
            []
        );
    }

    protected virtual AssetInf ReadAssetInf(BinaryReader reader)
    {
        return new AssetInf(reader.ReadEvilInt());
    }

    protected virtual void WriteAssetInf(BinaryWriter writer, AssetInf inf)
    {
        writer.WriteEvilInt(inf.Value);
    }

    protected virtual AssetHeader InitAssetHeader()
    {
        return new AssetHeader(NewBlock<AssetDebug>());
    }

    protected virtual AssetHeader ReadAssetHeader(BinaryReader reader)
    {
        return new AssetHeader
        {
            AssetId = reader.ReadEvilInt(),
            Type = (AssetType)reader.ReadEvilInt(),
            Offset = reader.ReadEvilInt(),
            Size = reader.ReadEvilInt(),
            Padding = reader.ReadEvilInt(),
            Flags = (AssetFlags)reader.ReadEvilInt()
        };
    }

    protected virtual void WriteAssetHeader(BinaryWriter writer, AssetHeader header)
    {
        writer.WriteEvilInt(header.AssetId);
        writer.WriteEvilInt((uint)header.Type);
        writer.WriteEvilInt(header.Offset);
        writer.WriteEvilInt(header.Size);
        writer.WriteEvilInt(header.Padding);
        writer.WriteEvilInt((uint)header.Flags);
    }

    protected virtual AssetDebug ReadAssetDebug(BinaryReader reader)
    {
        return new AssetDebug
        {
            Alignment = reader.ReadEvilInt(),
            Name = reader.ReadEvilString(),
            FileName = reader.ReadEvilString(),
            Checksum = reader.ReadEvilInt()
        };
    }

    protected virtual void WriteAssetDebug(BinaryWriter writer, AssetDebug debug)
    {
        writer.WriteEvilInt(debug.Alignment);
        writer.WriteEvilString(debug.Name);
        writer.WriteEvilString(debug.FileName);
        writer.WriteEvilInt(debug.Checksum);
    }

    protected virtual LayerTable InitLayerTable()
    {
        return new LayerTable(
            NewBlock<LayerInf>(),
            []
        );
    }

    protected virtual LayerInf ReadLayerInf(BinaryReader reader)
    {
        return new LayerInf(reader.ReadEvilInt());
    }

    protected virtual void WriteLayerInf(BinaryWriter writer, LayerInf inf)
    {
        writer.WriteEvilInt(inf.Value);
    }

    protected virtual LayerHeader InitLayerHeader()
    {
        return new LayerHeader(NewBlock<LayerDebug>());
    }

    protected virtual LayerHeader ReadLayerHeader(BinaryReader reader)
    {
        // todo: uhhhhh drop LayerType.Unknown stuff, we shouldn't alter the stuff we read in
        uint layerValue = reader.ReadEvilInt();
        LayerType layerType = layerValue switch
        {
            0 => LayerType.Default,
            1 => LayerType.Texture,
            2 => LayerType.BSP,
            3 => LayerType.Model,
            4 => LayerType.Animation,
            5 => LayerType.VRAM,
            6 => LayerType.SRAM,
            7 => LayerType.SoundTable,
            8 => LayerType.Cutscene,
            9 => LayerType.CutsceneTable,
            _ => (LayerType)layerValue,
        };

        uint assetCount = reader.ReadEvilInt();
        uint[] assetIds = new uint[assetCount];
        for (int i = 0; i < assetCount; i++)
            assetIds[i] = reader.ReadEvilInt();

        return new LayerHeader
        {
            Type = layerType,
            AssetCount = assetCount,
            AssetIds = assetIds
        };
    }

    protected virtual void WriteLayerHeader(BinaryWriter writer, LayerHeader header)
    {
        uint layerType = header.Type switch
        {
            LayerType.Default => 0,
            LayerType.Texture => 1,
            LayerType.BSP => 2,
            LayerType.Model => 3,
            LayerType.Animation => 4,
            LayerType.VRAM => 5,
            LayerType.SRAM => 6,
            LayerType.SoundTable => 7,
            LayerType.Cutscene => 8,
            LayerType.CutsceneTable => 9,
            _ => uint.MaxValue,
        };
        writer.WriteEvilInt(layerType);

        writer.WriteEvilInt(header.AssetCount);

        foreach (var assetId in header.AssetIds)
            writer.WriteEvilInt(assetId);
    }

    protected virtual LayerDebug ReadLayerDebug(BinaryReader reader)
    {
        return new LayerDebug
        {
            Value = reader.ReadEvilInt()
        };
    }

    protected virtual void WriteLayerDebug(BinaryWriter writer, LayerDebug debug)
    {
        writer.WriteEvilInt(debug.Value);
    }
}

public partial class V2Serializer
{
}

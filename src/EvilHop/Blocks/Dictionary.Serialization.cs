using EvilHop.Blocks;
using EvilHop.Primitives;
using System.Text;

namespace EvilHop.Serialization;

public partial class V1Serializer
{
    protected virtual AssetInf ReadAssetInf(BinaryReader reader)
    {
        return new AssetInf
        {
            Value = reader.ReadEvilInt()
        };
    }

    protected virtual void WriteAssetInf(BinaryWriter writer, AssetInf inf)
    {
        writer.WriteEvilInt(inf.Value);
    }

    protected virtual AssetHeader ReadAssetHeader(BinaryReader reader)
    {
        return new AssetHeader
        {
            AssetId = reader.ReadEvilInt(),
            Type = Encoding.ASCII.GetString(reader.ReadBytes(4)),
            Offset = reader.ReadEvilInt(),
            Size = reader.ReadEvilInt(),
            Padding = reader.ReadEvilInt(),
            Flags = reader.ReadEvilInt()
        };
    }

    protected virtual void WriteAssetHeader(BinaryWriter writer, AssetHeader header)
    {
        writer.WriteEvilInt(header.AssetId);
        writer.Write(header.Type.ToEvilBytes()[..^2]);
        writer.WriteEvilInt(header.Offset);
        writer.WriteEvilInt(header.Size);
        writer.WriteEvilInt(header.Padding);
        writer.WriteEvilInt(header.Flags);
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

    protected virtual LayerInf ReadLayerInf(BinaryReader reader)
    {
        return new LayerInf
        {
            Value = reader.ReadEvilInt(),
        };
    }

    protected virtual void WriteLayerInf(BinaryWriter writer, LayerInf inf)
    {
        writer.WriteEvilInt(inf.Value);
    }

    protected virtual LayerHeader ReadLayerHeader(BinaryReader reader)
    {
        uint type = reader.ReadEvilInt();
        uint assetCount = reader.ReadEvilInt();
        uint[] assetIds = new uint[assetCount];
        for (int i = 0; i < assetCount; i++)
            assetIds[i] = reader.ReadEvilInt();

        return new LayerHeader
        {
            Type = type,
            AssetCount = assetCount,
            AssetIds = assetIds
        };
    }

    protected virtual void WriteLayerHeader(BinaryWriter writer, LayerHeader header)
    {
        writer.WriteEvilInt(header.Type);
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
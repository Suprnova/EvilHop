using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V1Serializer
{
    protected virtual AssetStream InitAssetStream()
    {
        return new AssetStream(
            NewBlock<StreamHeader>(),
            NewBlock<StreamData>()
        );
    }

    protected virtual StreamHeader ReadStreamHeader(BinaryReader reader)
    {
        return new StreamHeader
        {
            Value = reader.ReadEvilInt()
        };
    }

    protected virtual void WriteStreamHeader(BinaryWriter writer, StreamHeader header)
    {
        writer.WriteEvilInt(header.Value);
    }

    protected virtual StreamData ReadStreamData(BinaryReader reader, uint length)
    {
        if (length < sizeof(uint))
            throw new InvalidDataException($"DPAK block length {length} is too small to contain its padding amount.");

        uint padding = reader.ReadEvilInt();
        uint remaining = length - sizeof(uint);
        if (padding > remaining)
            throw new InvalidDataException($"DPAK padding amount {padding} exceeds the block length {length}.");

        ReaderGuard.EnsureAvailable(reader, remaining, "DPAK block");

        reader.ReadBytes((int)padding);
        byte[] data = reader.ReadBytes((int)(remaining - padding));

        return new StreamData
        {
            PaddingAmount = padding,
            Data = data
        };
    }

    protected virtual void WriteStreamData(BinaryWriter writer, StreamData data)
    {
        // todo: no parity with an empty DPAK (yet)
        writer.WriteEvilInt(data.PaddingAmount);
        writer.Write(Enumerable.Repeat((byte)0x33, (int)data.PaddingAmount).ToArray());
        writer.Write(data.Data);
    }
}

public partial class V2Serializer
{
}

using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V1Serializer
{
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
        uint padding = reader.ReadEvilInt();
        reader.ReadBytes((int)padding);
        byte[] data = reader.ReadBytes((int)(length - sizeof(uint) - padding));

        return new StreamData
        {
            PaddingAmount = padding,
            Data = data
        };
    }

    protected virtual void WriteStreamData(BinaryWriter writer, StreamData data)
    {
        writer.WriteEvilInt(data.PaddingAmount);
        writer.Write(Enumerable.Repeat((byte)0x33, (int)data.PaddingAmount).ToArray());
        writer.Write(data.Data);
    }
}

public partial class V2Serializer
{
}

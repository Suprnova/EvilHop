using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Serialization;

public partial class Serializer
{
    /// <summary>
    /// Reads the fields of a <see cref="StreamHeader"/> (DHDR) block.
    /// </summary>
    protected static void ReadStreamHeader(EndianReader reader, StreamHeader block, uint size)
    {
        block.Value = reader.ReadUInt32();
    }

    /// <summary>
    /// Writes the fields of a <see cref="StreamHeader"/> (DHDR) block.
    /// </summary>
    protected static void WriteStreamHeader(EndianWriter writer, StreamHeader block) =>
        writer.Write(block.Value);

    /// <summary>
    /// Reads the fields of a <see cref="StreamData"/> (DPAK) block. <paramref name="size"/> is
    /// used to compute <see cref="StreamData.Data"/>'s length, and to detect the no-assets case
    /// (<c>size == 0</c>, leaving <see cref="StreamData.PaddingAmount"/> <see langword="null"/>).
    /// </summary>
    protected void ReadStreamData(EndianReader reader, StreamData block, uint size)
    {
        if (size == 0) return;

        if (!Profile.StreamDataHasPaddingField)
        {
            block.Data = reader.ReadBytes((int)size);
            return;
        }

        uint paddingAmount = reader.ReadUInt32();
        block.PaddingAmount = paddingAmount;

        // special handling for DPAKs consisting entirely of padding
        uint remaining = size - 4;
        uint paddingToRead = Math.Min(paddingAmount, remaining);

        block.Padding = reader.ReadBytes((int)paddingToRead);
        block.Data = reader.ReadBytes((int)(remaining - paddingToRead));
    }

    /// <summary>
    /// Writes the fields of a <see cref="StreamData"/> (DPAK) block.
    /// </summary>
    protected static void WriteStreamData(EndianWriter writer, StreamData block)
    {
        if (block.PaddingAmount is uint paddingAmount)
        {
            writer.Write(paddingAmount);
            writer.Write(block.Padding);
        }
        writer.Write(block.Data);
    }
}

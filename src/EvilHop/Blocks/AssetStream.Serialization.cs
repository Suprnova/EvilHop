using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Serialization;

public partial class Serializer
{
    /// <summary>
    /// Reads the fields of a <see cref="StreamHeader"/> (DHDR) block.
    /// </summary>
    protected static void ReadStreamHeader(BinaryReader reader, StreamHeader block, uint size)
    {
        block.Value = reader.ReadEvilInt();
    }

    /// <summary>
    /// Writes the fields of a <see cref="StreamHeader"/> (DHDR) block.
    /// </summary>
    protected static void WriteStreamHeader(BinaryWriter writer, StreamHeader block) =>
        writer.WriteEvilInt(block.Value);

    /// <summary>
    /// Reads the fields of a <see cref="StreamData"/> (DPAK) block. <paramref name="size"/> is
    /// used to compute <see cref="StreamData.Data"/>'s length, and to detect the no-assets case
    /// (<c>size == 0</c>, leaving <see cref="StreamData.PaddingAmount"/> <see langword="null"/>).
    /// </summary>
    /// <remarks>
    /// Whether the content leads with a padding-amount field is governed by
    /// <see cref="FormatProfile.StreamDataHasPaddingField"/>; where it does not,
    /// <see cref="StreamData.PaddingAmount"/> and <see cref="StreamData.Padding"/> stay at their
    /// initializer values and the whole content is <see cref="StreamData.Data"/>.
    /// </remarks>
    protected void ReadStreamData(BinaryReader reader, StreamData block, uint size)
    {
        if (size == 0) return;

        if (!Profile.StreamDataHasPaddingField)
        {
            block.Data = reader.ReadBytes((int)size);
            return;
        }

        uint paddingAmount = reader.ReadEvilInt();
        block.PaddingAmount = paddingAmount;

        // A build's tooling can fill an entirely-empty DPAK's content - this length prefix included -
        // with the padding byte itself instead of writing a real length. PaddingAmount then reads
        // back larger than the block has room for; clamp both reads to what's actually left instead
        // of overrunning into Data.
        uint remaining = size - 4;
        uint paddingToRead = Math.Min(paddingAmount, remaining);
        block.Padding = reader.ReadBytes((int)paddingToRead);
        block.Data = reader.ReadBytes((int)(remaining - paddingToRead));
    }

    /// <summary>
    /// Writes the fields of a <see cref="StreamData"/> (DPAK) block. Unlike its reader, this needs
    /// no <see cref="FormatProfile"/> - whether a padding field is written is driven entirely by
    /// whether <see cref="StreamData.PaddingAmount"/> is <see langword="null"/>, which is already
    /// unambiguous model data by the time it's read. <see cref="StreamData.PaddingAmount"/> and
    /// <see cref="StreamData.Padding"/> are written independently, exactly as stored - a mismatch
    /// between the two is a permitted invalid state, left to <c>Validate()</c> to flag.
    /// </summary>
    protected static void WriteStreamData(BinaryWriter writer, StreamData block)
    {
        if (block.PaddingAmount is uint paddingAmount)
        {
            writer.WriteEvilInt(paddingAmount);
            writer.Write(block.Padding);
        }
        writer.Write(block.Data);
    }
}

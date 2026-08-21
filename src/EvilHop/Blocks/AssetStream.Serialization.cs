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
        block.Padding = reader.ReadBytes((int)paddingAmount);
        block.Data = reader.ReadBytes((int)(size - paddingAmount - 4));
    }
}

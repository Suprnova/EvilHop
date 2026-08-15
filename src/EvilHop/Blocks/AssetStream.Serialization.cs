using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Serialization;

public partial class SerializerV1
{
    /// <summary>
    /// Reads the fields of a <see cref="StreamHeader"/> (DHDR) block.
    /// </summary>
    protected virtual void ReadStreamHeader(BinaryReader reader, StreamHeader block, uint size)
    {
        block.Value = reader.ReadEvilInt();
    }

    /// <summary>
    /// Reads the fields of a <see cref="StreamData"/> (DPAK) block. <paramref name="size"/> is
    /// used to compute <see cref="StreamData.Data"/>'s length, and to detect the no-assets case
    /// (<c>size == 0</c>, leaving <see cref="StreamData.PaddingAmount"/> <see langword="null"/>).
    /// </summary>
    protected virtual void ReadStreamData(BinaryReader reader, StreamData block, uint size)
    {
        if (size == 0) return;

        uint paddingAmount = reader.ReadEvilInt();
        block.PaddingAmount = paddingAmount;
        block.Padding = reader.ReadBytes((int)paddingAmount);
        block.Data = reader.ReadBytes((int)(size - paddingAmount - 4));
    }
}

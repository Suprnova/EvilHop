using EvilHop.Blocks;

namespace EvilHop.Serialization;

public interface IFormatSerializer
{
    HipFile ReadArchive(BinaryReader reader);

    void WriteArchive(BinaryWriter writer, HipFile archive);

    Block Read(BinaryReader reader);

    T Read<T>(BinaryReader reader) where T : Block;

    void Write(BinaryWriter writer, Block block);
}

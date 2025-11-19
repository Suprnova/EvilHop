using EvilHop.Blocks;
using EvilHop.Serialization.Validation;

namespace EvilHop.Serialization;

public interface IFormatSerializer
{
    HipFile ReadArchive(BinaryReader reader, SerializerOptions? options = null);

    void WriteArchive(BinaryWriter writer, HipFile archive);

    Block Read(BinaryReader reader, SerializerOptions? options = null);

    T Read<T>(BinaryReader reader, SerializerOptions? options = null) where T : Block;

    void Write(BinaryWriter writer, Block block);

    IEnumerable<ValidationIssue> Validate(Block block);

    IEnumerable<ValidationIssue> ValidateArchive(HipFile hip);
}

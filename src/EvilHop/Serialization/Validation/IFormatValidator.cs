using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public interface IFormatValidator
{
    IEnumerable<ValidationIssue> Validate(Block block);
    IEnumerable<ValidationIssue> ValidateArchive(HipFile hipFile);
}

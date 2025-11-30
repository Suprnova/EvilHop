using EvilHop.Assets;
using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public interface IFormatValidator
{
    IEnumerable<ValidationIssue> ValidateArchive(HipFile hipFile);
    IEnumerable<ValidationIssue> ValidateBlock(Block block);
    IEnumerable<ValidationIssue> ValidateAsset(Asset asset);
}

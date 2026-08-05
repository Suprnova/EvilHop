using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidateAssetStream(AssetStream stream)
    {
        yield break;
    }

    protected virtual IEnumerable<ValidationIssue> ValidateStreamHeader(StreamHeader header)
    {
        yield break;
    }

    protected virtual IEnumerable<ValidationIssue> ValidateStreamData(StreamData data)
    {
        yield break;
    }
}

public partial class V2Validator
{
}

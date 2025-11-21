using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidateAssetStream(AssetStream stream)
    {
        if (stream.GetChild<StreamHeader>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type '{typeof(StreamHeader).Name}' is missing from {typeof(AssetStream).Name}.",
                Context = stream
            };
        }
        if (stream.GetChild<StreamData>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type '{typeof(StreamData).Name}' is missing from {typeof(AssetStream).Name}.",
                Context = stream
            };
        }
    }

    protected virtual IEnumerable<ValidationIssue> ValidateStreamHeader(StreamHeader header)
    {
        if (header.Value != 0xFFFFFFFF)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"{typeof(StreamHeader).Name} value '{header.Value}' is unknown",
                Context = header
            };
        }
    }

    protected virtual IEnumerable<ValidationIssue> ValidateStreamData(StreamData data)
    {
        yield break;
    }
}

public partial class V2Validator
{
}

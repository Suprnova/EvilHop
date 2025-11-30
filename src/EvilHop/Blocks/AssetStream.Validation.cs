using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidateAssetStream(AssetStream stream)
    {
        if (stream.GetChild<StreamHeader>() == null)
            yield return ValidationIssue.MissingChild<AssetStream, StreamHeader>(stream);

        if (stream.GetChild<StreamData>() == null)
            yield return ValidationIssue.MissingChild<AssetStream, StreamData>(stream);
    }

    protected virtual IEnumerable<ValidationIssue> ValidateStreamHeader(StreamHeader header)
    {
        if (header.Value != 0xFFFFFFFF)
            yield return ValidationIssue.UnknownValue(nameof(header.Value), header.Value, header);
    }

    protected virtual IEnumerable<ValidationIssue> ValidateStreamData(StreamData data)
    {
        yield break;
    }
}

public partial class V2Validator
{
}

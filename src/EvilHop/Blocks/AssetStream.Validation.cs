using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidateAssetStream(AssetStream stream)
    {
        foreach (var issue in ValidateChildCount(stream, GetExpectedChildCount(stream)))
            yield return issue;

        if (stream.GetChild<StreamHeader>() == null)
            yield return ValidationIssue.MissingChild<AssetStream, StreamHeader>(stream);

        if (stream.GetChild<StreamData>() == null)
            yield return ValidationIssue.MissingChild<AssetStream, StreamData>(stream);
    }

    protected virtual IEnumerable<ValidationIssue> ValidateStreamHeader(StreamHeader header)
    {
        foreach (var issue in ValidateChildCount(header, GetExpectedChildCount(header)))
            yield return issue;

        if (header.Value != 0xFFFFFFFF)
            yield return ValidationIssue.UnknownValue(nameof(header.Value), header.Value, header);
    }

    protected virtual IEnumerable<ValidationIssue> ValidateStreamData(StreamData data)
    {
        foreach (var issue in ValidateChildCount(data, GetExpectedChildCount(data)))
            yield return issue;
    }

    protected virtual int GetExpectedChildCount(AssetStream stream) => 2;
    protected virtual int GetExpectedChildCount(StreamHeader header) => 0;
    protected virtual int GetExpectedChildCount(StreamData data) => 0;
}

public partial class V2Validator
{
}

using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidateAssetStream(AssetStream stream, int expectedChildrenCount = 2)
    {
        foreach (var issue in ValidateChildCount(stream, expectedChildrenCount))
            yield return issue;

        if (stream.GetChild<StreamHeader>() == null)
            yield return ValidationIssue.MissingChild<AssetStream, StreamHeader>(stream);

        if (stream.GetChild<StreamData>() == null)
            yield return ValidationIssue.MissingChild<AssetStream, StreamData>(stream);
    }

    protected virtual IEnumerable<ValidationIssue> ValidateStreamHeader(StreamHeader header, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(header, expectedChildrenCount))
            yield return issue;

        if (header.Value != 0xFFFFFFFF)
            yield return ValidationIssue.UnknownValue(nameof(header.Value), header.Value, header);
    }

    protected virtual IEnumerable<ValidationIssue> ValidateStreamData(StreamData data, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(data, expectedChildrenCount))
            yield return issue;
    }
}

public partial class V2Validator
{
}

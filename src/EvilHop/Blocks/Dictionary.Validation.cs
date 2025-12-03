using EvilHop.Blocks;
using EvilHop.Common;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidateDictionary(Dictionary dictionary, int expectedChildrenCount = 2)
    {
        foreach (var issue in ValidateChildCount(dictionary, expectedChildrenCount))
            yield return issue;

        if (dictionary.GetChild<AssetTable>() == null)
            yield return ValidationIssue.MissingChild<Dictionary, AssetTable>(dictionary);

        if (dictionary.GetChild<LayerTable>() == null)
            yield return ValidationIssue.MissingChild<Dictionary, LayerTable>(dictionary);
    }

    protected virtual IEnumerable<ValidationIssue> ValidateAssetTable(AssetTable table)
    {
        if (table.GetChild<AssetInf>() == null)
            yield return ValidationIssue.MissingChild<AssetTable, AssetInf>(table);

        // todo: validate no conflicting AssetIds (?)
    }

    protected virtual IEnumerable<ValidationIssue> ValidateAssetInf(AssetInf inf, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(inf, expectedChildrenCount))
            yield return issue;

        if (inf.Value != 0x00000000)
            yield return ValidationIssue.UnknownValue(nameof(inf.Value), inf.Value, inf);
    }

    protected virtual IEnumerable<ValidationIssue> ValidateAssetHeader(AssetHeader header, int expectedChildrenCount = 1)
    {
        foreach (var issue in ValidateChildCount(header, expectedChildrenCount))
            yield return issue;

        if (header.Flags.HasFlag(AssetFlags.SourceFile) && header.Flags.HasFlag(AssetFlags.SourceVirtual))
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"{typeof(AssetFlags).Name} has conflicting values: {Enum.GetName(AssetFlags.SourceFile)} & {Enum.GetName(AssetFlags.SourceVirtual)}.",
                Context = header
            };
        }

        if ((uint)(header.Flags & ~AssetFlags.UnknownScooby) > 0xF)
            yield return ValidationIssue.UnknownValue(nameof(header.Flags), (uint)header.Flags, header);

        if (header.GetChild<AssetDebug>() == null)
            yield return ValidationIssue.MissingChild<AssetHeader, AssetDebug>(header);
        else
        {
            string assetName = header.GetChild<AssetDebug>()!.Name;

            // special calculation for these assets
            if (header.Type == AssetType.Animation) assetName = Path.ChangeExtension(assetName, ".anm");
            else if (header.Type == AssetType.MorphTarget) assetName = Path.ChangeExtension(assetName, ".mph");

            uint expectedHash = BKDRHash.Calculate(assetName);
            if (expectedHash != header.AssetId && header.Debug.Name.Length < 31)
            {
                yield return new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Message = $"Unexpected ID for '{assetName}' of '{header.AssetId}', expected '{expectedHash}'.",
                    Context = header
                };
            }

            if (header.Flags.HasFlag(AssetFlags.SourceFile))
            {
                string fileName = header.GetChild<AssetDebug>()!.FileName;
                if (String.IsNullOrEmpty(fileName))
                {
                    yield return new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = $"File Name for {Enum.GetName(AssetFlags.SourceFile)} enabled asset is missing from {typeof(AssetDebug).Name} child block.",
                        Context = header
                    };
                }
            }
        }

        // todo: validate asset type as enum

        // todo: validate padding via AssetDebug.Alignment
    }

    protected virtual IEnumerable<ValidationIssue> ValidateAssetDebug(AssetDebug debug, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(debug, expectedChildrenCount))
            yield return issue;
    }

    protected virtual IEnumerable<ValidationIssue> ValidateLayerTable(LayerTable table)
    {
        if (table.GetChild<LayerInf>() == null)
            yield return ValidationIssue.MissingChild<LayerTable, LayerDebug>(table);
    }

    protected virtual IEnumerable<ValidationIssue> ValidateLayerInf(LayerInf inf, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(inf, expectedChildrenCount))
            yield return issue;

        if (inf.Value != 0x00000000)
            yield return ValidationIssue.UnknownValue(nameof(inf.Value), inf.Value, inf);
    }

    protected virtual IEnumerable<ValidationIssue> ValidateLayerHeader(LayerHeader header, int expectedChildrenCount = 1)
    {
        foreach (var issue in ValidateChildCount(header, expectedChildrenCount))
            yield return issue;

        if (header.GetChild<LayerDebug>() == null)
            yield return ValidationIssue.MissingChild<LayerHeader, LayerDebug>(header);

        if (header.Type == LayerType.TextureStream || header.Type == LayerType.JSPInfo)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Layer Type '{Enum.GetName(header.Type)}' (ID = '{(uint)header.Type}') is not valid in HIP Version 1.",
                Context = header
            };
        }
        else if (!Enum.IsDefined(header.Type))
            yield return ValidationIssue.UnknownValue(nameof(header.Type), (uint)header.Type, header);

        uint expectedCount = (uint)header.AssetIds.Count();
        if (header.AssetCount != expectedCount)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"Unexpected AssetCount of '{header.AssetCount}', expected '{expectedCount}'",
                Context = header
            };
        }
    }

    protected virtual IEnumerable<ValidationIssue> ValidateLayerDebug(LayerDebug debug, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(debug, expectedChildrenCount))
            yield return issue;

        if (debug.Value != 0xFFFFFFFF)
            yield return ValidationIssue.UnknownValue(nameof(debug.Value), debug.Value, debug);
    }
}

public partial class V2Validator
{
}

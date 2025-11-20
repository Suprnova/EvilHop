using EvilHop.Blocks;
using EvilHop.Helpers;
using System.Reflection.PortableExecutable;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidateDictionary(Dictionary dictionary)
    {
        if (dictionary.GetChild<AssetTable>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type '{typeof(AssetTable).Name}' is missing from {typeof(Dictionary).Name}.",
                Context = dictionary
            };
        }
        if (dictionary.GetChild<LayerTable>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type '{typeof(LayerTable).Name}' is missing from {typeof(Dictionary).Name}.",
                Context = dictionary
            };
        }
    }

    protected virtual IEnumerable<ValidationIssue> ValidateAssetTable(AssetTable table)
    {
        if (table.GetChild<AssetInf>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type '{typeof(AssetInf).Name}' is missing from {typeof(AssetTable).Name}",
                Context = table
            };
        }

        // todo: validate no conflicting AssetIds (?)
    }

    protected virtual IEnumerable<ValidationIssue> ValidateAssetInf(AssetInf inf)
    {
        if (inf.Value != 0) {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.None,
                Message = $"{typeof(AssetInf).Name} value '{inf.Value}' is unknown, likely undocumented.",
                Context = inf
            };
        }
    }

    protected virtual IEnumerable<ValidationIssue> ValidateAssetHeader(AssetHeader header)
    {
        // todo: validate flags as enum

        if (header.GetChild<AssetDebug>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type '{typeof(AssetDebug).Name}' is missing from {typeof(AssetHeader).Name}",
                Context = header
            };
        }
        else
        {
            string assetName = header.GetChild<AssetDebug>()!.Name;
            uint expectedHash = BKDRHash.Calculate(assetName);
            if (expectedHash != header.AssetId)
            {
                yield return new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Message = $"Unexpected ID for '{assetName}' of '{header.AssetId}', expected '{expectedHash}'.",
                    Context = header
                };
            }

            // todo: validate Flags.SOURCE_FILE correlates with population of AssetDebug.FileName
        }

        // todo: validate asset type as enum

        // todo: validate padding via AssetDebug.Alignment
    }

    protected virtual IEnumerable<ValidationIssue> ValidateAssetDebug(AssetDebug debug)
    {
        yield break;
    }

    protected virtual IEnumerable<ValidationIssue> ValidateLayerTable(LayerTable table)
    {
        if (table.GetChild<LayerInf>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type '{typeof(LayerInf).Name}' is missing from {typeof(LayerTable).Name}",
                Context = table
            };
        }
    }

    protected virtual IEnumerable<ValidationIssue> ValidateLayerInf(LayerInf inf)
    {
        if (inf.Value != 0)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.None,
                Message = $"{typeof(LayerInf).Name} value '{inf.Value}' is unknown, likely undocumented.",
                Context = inf
            };
        }
    }

    protected virtual IEnumerable<ValidationIssue> ValidateLayerHeader(LayerHeader header)
    {
        if (header.GetChild<LayerDebug>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type '{typeof(LayerDebug).Name}' is missing from {typeof(LayerHeader).Name}",
                Context = header
            };
        }

        // todo: validate type as enum

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

    protected virtual IEnumerable<ValidationIssue> ValidateLayerDebug(LayerDebug debug)
    {
        if (debug.Value != uint.MaxValue)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.None,
                Message = $"{typeof(LayerDebug).Name} value '{debug.Value}' is unknown, likely undocumented",
                Context = debug
            };
        }
    }
}

public partial class V2Validator
{
}

using EvilHop.Blocks;
using EvilHop.Corpus.Archives;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary>Every <see cref="LayerHeader.AssetIds"/> entry matches an existing <see cref="AssetHeader.Id"/>.</summary>
internal sealed class LayerAssetIdsResolveInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "layerAssetIdsResolve";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        var knownIds = archive.AllBlocks.OfType<AssetHeader>().Select(h => h.Id).ToHashSet();

        foreach (var layer in archive.AllBlocks.OfType<LayerHeader>())
        {
            foreach (var assetId in layer.AssetIds)
            {
                _result.Record(knownIds.Contains(assetId), () => new JsonObject
                {
                    ["path"] = archive.RelativePath,
                    ["layerType"] = $"0x{(uint)layer.Type:X8}",
                    ["assetId"] = $"0x{assetId:X8}"
                });
            }
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

/// <summary>Σ <see cref="LayerHeader.AssetCount"/> across all layers never exceeds the archive's total asset count.</summary>
internal sealed class LayerAssetCountsSumWithinTotalInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "layerAssetCountsSumWithinTotal";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        long sum = archive.AllBlocks.OfType<LayerHeader>().Sum(l => (long)l.AssetCount);
        long total = archive.AllBlocks.OfType<AssetHeader>().LongCount();

        _result.Record(sum <= total, () => new JsonObject
        {
            ["path"] = archive.RelativePath,
            ["layerAssetCountSum"] = sum,
            ["totalAssetCount"] = total
        });
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

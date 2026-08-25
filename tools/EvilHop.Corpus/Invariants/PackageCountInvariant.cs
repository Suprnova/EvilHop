using EvilHop.Blocks;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary><see cref="PackageCount.AssetCount"/>/<see cref="PackageCount.LayerCount"/> match the actual tree counts.</summary>
internal sealed class PackageCountsMatchTreeInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "packageCountsMatchTree";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        var counts = archive.AllBlocks.OfType<PackageCount>().FirstOrDefault();
        if (counts is null) return;

        long actualAssets = archive.AllBlocks.OfType<AssetHeader>().LongCount();
        long actualLayers = archive.AllBlocks.OfType<LayerHeader>().LongCount();

        _result.Record(counts.AssetCount == actualAssets, () => new JsonObject
        {
            ["path"] = archive.RelativePath,
            ["field"] = nameof(PackageCount.AssetCount),
            ["expected"] = counts.AssetCount,
            ["actual"] = actualAssets
        });

        _result.Record(counts.LayerCount == actualLayers, () => new JsonObject
        {
            ["path"] = archive.RelativePath,
            ["field"] = nameof(PackageCount.LayerCount),
            ["expected"] = counts.LayerCount,
            ["actual"] = actualLayers
        });
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

/// <summary>
/// <see cref="PackageCount.MaxAssetSize"/>, <see cref="PackageCount.MaxLayerSize"/>, and
/// <see cref="PackageCount.MaxXFormAssetSize"/> match their computed maxima over the tree. A layer's
/// size is the sum of <c>Size + Plus</c> over every entry in its <see cref="LayerHeader.AssetIds"/> -
/// once per listing, not once per distinct asset ID. Confirmed against
/// <c>n100f/prototype_2001-06-11</c>, the only build where a listed asset ID repeats within a single
/// layer and its last asset carries a non-zero <c>Plus</c>, and therefore the only build where this
/// sum-of-listings definition and a simpler first-to-last byte extent disagree.
/// </summary>
internal sealed class PackageMaxSizesMatchTreeInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "packageMaxSizesMatchTree";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        var counts = archive.AllBlocks.OfType<PackageCount>().FirstOrDefault();
        if (counts is null) return;

        var headers = archive.AllBlocks.OfType<AssetHeader>().ToList();
        CheckMaxAssetSize(archive, counts, headers);
        CheckMaxXFormAssetSize(archive, counts, headers);
        CheckMaxLayerSize(archive, counts, headers);
    }

    private void CheckMaxAssetSize(ArchiveContext archive, PackageCount counts, List<AssetHeader> headers)
    {
        uint expected = headers.Count == 0 ? 0 : headers.Max(h => h.Size);
        _result.Record(counts.MaxAssetSize == expected, () => new JsonObject
        {
            ["path"] = archive.RelativePath,
            ["field"] = nameof(PackageCount.MaxAssetSize),
            ["expected"] = expected,
            ["actual"] = counts.MaxAssetSize
        });
    }

    private void CheckMaxXFormAssetSize(ArchiveContext archive, PackageCount counts, List<AssetHeader> headers)
    {
        var transformed = headers.Where(h => h.Flags.HasFlag(AssetFlags.ReadTransform)).ToList();
        uint expected = transformed.Count == 0 ? 0 : transformed.Max(h => h.Size);
        _result.Record(counts.MaxXFormAssetSize == expected, () => new JsonObject
        {
            ["path"] = archive.RelativePath,
            ["field"] = nameof(PackageCount.MaxXFormAssetSize),
            ["expected"] = expected,
            ["actual"] = counts.MaxXFormAssetSize
        });
    }

    private void CheckMaxLayerSize(ArchiveContext archive, PackageCount counts, List<AssetHeader> headers)
    {
        var layers = archive.AllBlocks.OfType<LayerHeader>().ToList();
        if (layers.Count == 0) return;

        var headersById = headers.ToDictionary(h => h.Id);
        uint expected = (uint)layers.Max(layer =>
        {
            long selector(uint id) => (long)headersById[id].Size + headersById[id].Plus;
            return layer.AssetIds
                        .Where(headersById.ContainsKey)
                        .Sum(selector);
        });

        _result.Record(counts.MaxLayerSize == expected, () => new JsonObject
        {
            ["path"] = archive.RelativePath,
            ["field"] = nameof(PackageCount.MaxLayerSize),
            ["expected"] = expected,
            ["actual"] = counts.MaxLayerSize
        });
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

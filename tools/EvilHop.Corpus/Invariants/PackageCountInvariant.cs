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
/// size is the byte extent its assets span in <see cref="StreamData.Data"/> - from the first entry
/// in <see cref="LayerHeader.AssetIds"/> to the last, excluding trailing padding. This definition is
/// our current best understanding, not confirmed against the format, and worth revisiting if the
/// corpus ever reports implausible violation counts here.
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
        var extents = layers
            .Select(layer => layer.AssetIds as IReadOnlyList<uint> ?? [.. layer.AssetIds])
            .Where(ids => ids.Count > 0)
            .Select(ids => (First: headersById.GetValueOrDefault(ids[0]), Last: headersById.GetValueOrDefault(ids[^1])))
            .Where(pair => pair.First is not null && pair.Last is not null)
            .Select(pair => (long)(pair.Last!.Offset + pair.Last.Size) - pair.First!.Offset)
            .ToList();

        if (extents.Count == 0) return;

        uint expected = (uint)extents.Max();
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

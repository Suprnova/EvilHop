using EvilHop.Blocks;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary>
/// Translates an <see cref="AssetHeader.Offset"/> (absolute within the archive file) into an index
/// within STRM/DPAK's <see cref="StreamData.Data"/>. <see cref="StreamData.Data"/> is the last thing
/// in a HIP archive - DHDR, LHDR and everything else that could follow it in a well-formed archive
/// comes before STRM's own children - so its start is simply the archive length minus its own length.
/// </summary>
internal static class AssetDataLocator
{
    /// <summary>The absolute offset at which <paramref name="data"/> begins within an archive of <paramref name="archiveLength"/> bytes.</summary>
    public static long DataStart(long archiveLength, int dataLength) => archiveLength - dataLength;

    /// <summary>
    /// Attempts to resolve <paramref name="offset"/>/<paramref name="size"/> to a byte range within
    /// <paramref name="data"/>. Returns <see langword="false"/> when the range falls outside it.
    /// </summary>
    public static bool TryGetRange(long archiveLength, byte[] data, uint offset, uint size, out Range range)
    {
        long start = offset - DataStart(archiveLength, data.Length);
        if (start < 0 || size > int.MaxValue || start + size > data.Length)
        {
            range = default;
            return false;
        }

        range = new Range((int)start, (int)(start + size));
        return true;
    }
}

/// <summary><see cref="AssetDebug.Checksum"/> equals the CRC-32/MPEG-2 of the asset's own data.</summary>
internal sealed class AssetChecksumMatchesDataInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "assetChecksumMatchesData";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        var streamData = archive.AllBlocks.OfType<StreamData>().FirstOrDefault();
        if (streamData is null) return;

        foreach (var header in archive.AllBlocks.OfType<AssetHeader>())
        {
            var debug = header.GetChild<AssetDebug>();
            if (debug is null) continue;
            if (!AssetDataLocator.TryGetRange(archive.ArchiveLength, streamData.Data, header.Offset, header.Size, out var range))
                continue;

            uint computed = Crc32Mpeg2.Compute(streamData.Data.AsSpan(range));
            _result.Record(computed == debug.Checksum, () => new JsonObject
            {
                ["path"] = archive.RelativePath,
                ["id"] = $"0x{header.Id:X8}",
                ["expected"] = $"0x{debug.Checksum:X8}",
                ["actual"] = $"0x{computed:X8}"
            });
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

/// <summary><c>AHDR.Offset + Size + Plus</c> never exceeds the archive's length.</summary>
internal sealed class AssetOffsetsInBoundsInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "assetOffsetsInBounds";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var header in archive.AllBlocks.OfType<AssetHeader>())
        {
            long end = (long)header.Offset + header.Size + header.Plus;
            _result.Record(end <= archive.ArchiveLength, () => new JsonObject
            {
                ["path"] = archive.RelativePath,
                ["id"] = $"0x{header.Id:X8}",
                ["end"] = end,
                ["archiveLength"] = archive.ArchiveLength
            });
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

/// <summary>The final asset of each layer, per <see cref="LayerHeader.AssetIds"/> order, has <c>Plus == 0</c>.</summary>
internal sealed class LastAssetInLayerHasZeroPlusInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "lastAssetInLayerHasZeroPlus";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        var headersById = archive.AllBlocks.OfType<AssetHeader>().ToDictionary(h => h.Id);

        foreach (var layer in archive.AllBlocks.OfType<LayerHeader>())
        {
            var assetIds = layer.AssetIds as IReadOnlyList<uint> ?? [.. layer.AssetIds];
            if (assetIds.Count == 0 || !headersById.TryGetValue(assetIds[^1], out var lastAsset)) continue;

            _result.Record(lastAsset.Plus == 0, () => new JsonObject
            {
                ["path"] = archive.RelativePath,
                ["layerType"] = $"0x{(uint)layer.Type:X8}",
                ["id"] = $"0x{lastAsset.Id:X8}",
                ["plus"] = lastAsset.Plus
            });
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

/// <summary>
/// <c>AHDR.Plus</c> pads this asset's end up to its own <see cref="AssetDebug.Alignment"/> boundary.
/// Skipped when alignment is non-positive (<c>-1</c> means "use the type's default", which this
/// invariant does not have a table for).
/// </summary>
internal sealed class PlusMatchesAlignmentInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "plusMatchesAlignment";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var header in archive.AllBlocks.OfType<AssetHeader>())
        {
            var debug = header.GetChild<AssetDebug>();
            if (debug is null || debug.Alignment <= 0) continue;

            uint alignment = (uint)debug.Alignment;
            uint end = header.Offset + header.Size;
            uint expectedPlus = (alignment - (end % alignment)) % alignment;

            _result.Record(header.Plus == expectedPlus, () => new JsonObject
            {
                ["path"] = archive.RelativePath,
                ["id"] = $"0x{header.Id:X8}",
                ["alignment"] = debug.Alignment,
                ["expectedPlus"] = expectedPlus,
                ["actualPlus"] = header.Plus
            });
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

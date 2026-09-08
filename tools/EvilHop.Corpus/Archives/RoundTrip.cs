using EvilHop.Assets;

namespace EvilHop.Corpus.Archives;

/// <summary>
/// Checks that an archive written back out reproduces the bytes it was read from, at the block
/// layer and again at the asset layer.
/// </summary>
internal static class RoundTrip
{
    /// <summary>The number of offending assets named in a failure message before it is elided.</summary>
    private const int MaxReported = 5;

    /// <summary>
    /// Writes <paramref name="archive"/> back out and diffs it against the bytes it was read from,
    /// first as it was read and then again with every asset parsed and reserialized through an
    /// <see cref="AssetSession"/>.
    /// </summary>
    /// <remarks>
    /// The block-layer pass alone never runs an asset's reader or writer: it copies <c>DPAK</c>'s
    /// bytes through verbatim, so an asset codec could be entirely wrong and still pass. The
    /// session pass is what exercises them, and it reserializes <em>every</em> asset -
    /// <see cref="AssetSession.Commit"/> rebuilds unconditionally, so an asset nothing modified is
    /// still round-tripped through its reader and writer.
    /// </remarks>
    /// <param name="archive">The archive to write back out. Left with its assets committed.</param>
    /// <param name="original">The bytes <paramref name="archive"/> was read from.</param>
    /// <returns>A description of the first mismatch found, or <see langword="null"/> when it matches.</returns>
    public static string? Check(Archive archive, ReadOnlySpan<byte> original)
    {
        ArgumentNullException.ThrowIfNull(archive);

        if (!Rewrite(archive).AsSpan().SequenceEqual(original))
            return "block round-trip byte mismatch.";

        using var session = archive.OpenAssets();

        if (session.Diagnostics.Count > 0)
            return $"{Count(session.Diagnostics, "asset diagnostic")}: {Summarize(session.Diagnostics)}";

        session.Commit();

        if (session.ChangedAssets.Count > 0)
            return $"{Count(session.ChangedAssets, "asset")} reserialized differently: {Summarize(Describe(session))}";

        return Rewrite(archive).AsSpan().SequenceEqual(original) ? null : "asset round-trip byte mismatch.";
    }

    /// <summary>Names the assets whose bytes changed across the session, by type and name.</summary>
    private static List<string> Describe(AssetSession session)
    {
        var changed = session.ChangedAssets.ToHashSet();

        return [.. session.Layers
            .SelectMany(layer => layer.Assets)
            .Where(asset => changed.Contains(asset.Id))
            .Select(asset => $"{asset.Type} '{asset.Name}' ({asset.Id})")];
    }

    private static byte[] Rewrite(Archive archive)
    {
        using var buffer = new MemoryStream();
        archive.Save(buffer);
        return buffer.ToArray();
    }

    private static string Count<T>(IReadOnlyCollection<T> items, string noun) =>
        $"{items.Count} {noun}{(items.Count == 1 ? "" : "s")}";

    private static string Summarize<T>(IReadOnlyCollection<T> items) =>
        string.Join("; ", items.Take(MaxReported))
            + (items.Count > MaxReported ? $"; +{items.Count - MaxReported} more" : "");
}

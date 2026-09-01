namespace EvilHop.Corpus.Caching;

/// <summary>
/// A gitignored, on-disk cache of per-archive map-stage output, keyed by the mapping facet, the
/// archive's content hash, and the facet's <see cref="Generation.IFacetGenerator.InputFingerprint"/>.
/// An archive is re-mapped only when its bytes or the facet's declared dependencies change; the
/// reduce stage always runs over the full covered set regardless of what was cached.
/// </summary>
/// <param name="directory">The directory to store cached map records under.</param>
public sealed class MapCache(string directory)
{
    /// <summary>
    /// Looks up a previously cached map record.
    /// </summary>
    /// <param name="facetId">The identifier of the facet that produced the record.</param>
    /// <param name="archiveSha256">The mapped archive's content hash.</param>
    /// <param name="inputFingerprint">The facet's <see cref="Generation.IFacetGenerator.InputFingerprint"/> at map time.</param>
    /// <param name="json">The cached record's JSON, if found.</param>
    /// <returns><see langword="true"/> if a matching record was cached; otherwise <see langword="false"/>.</returns>
    public bool TryGet(string facetId, string archiveSha256, string inputFingerprint, out string? json)
    {
        string path = PathFor(facetId, archiveSha256, inputFingerprint);
        if (!File.Exists(path))
        {
            json = null;
            return false;
        }

        json = File.ReadAllText(path);
        return true;
    }

    /// <summary>
    /// Stores a map record.
    /// </summary>
    /// <param name="facetId">The identifier of the facet that produced the record.</param>
    /// <param name="archiveSha256">The mapped archive's content hash.</param>
    /// <param name="inputFingerprint">The facet's <see cref="Generation.IFacetGenerator.InputFingerprint"/> at map time.</param>
    /// <param name="json">The record's JSON.</param>
    public void Set(string facetId, string archiveSha256, string inputFingerprint, string json)
    {
        string path = PathFor(facetId, archiveSha256, inputFingerprint);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    private string PathFor(string facetId, string archiveSha256, string inputFingerprint) =>
        Path.Combine(directory, facetId, $"{archiveSha256}-{inputFingerprint}.json");
}

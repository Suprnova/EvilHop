using EvilHop.Corpus.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Generation;

/// <summary>
/// An archive covered by a facet generation run, already loaded and hashed.
/// </summary>
/// <param name="Path">The archive's path, relative to the artifact root.</param>
/// <param name="Sha256">The archive's content hash.</param>
/// <param name="Archive">The loaded archive.</param>
public sealed record CoveredArchive(string Path, string Sha256, Archive Archive);

/// <summary>
/// Runs an <see cref="IFacetGenerator"/>'s map stage over a set of archives, caching each archive's
/// contribution, then reduces every contribution into the facet's committed shape - <c>generator</c>
/// and <c>coverage</c> metadata alongside the generator's own <c>observations</c>.
/// </summary>
public static class FacetPipeline
{
    /// <summary>
    /// Generates <paramref name="generator"/>'s facet over <paramref name="archives"/>.
    /// </summary>
    /// <param name="generator">The facet generator to run.</param>
    /// <param name="archives">The archives the facet covers.</param>
    /// <param name="cache">The map-stage cache to read from and write to.</param>
    /// <returns>The facet, ready to be written under its ID in an inventory's <c>facets</c> object.</returns>
    public static JsonObject Run(IFacetGenerator generator, IReadOnlyList<CoveredArchive> archives, MapCache cache)
    {
        string inputFingerprint = generator.InputFingerprint();

        var records = archives
            .Select(archive => new MappedArchive(archive.Path, MapWithCache(generator, archive, inputFingerprint, cache)))
            .ToList();

        return new JsonObject
        {
            ["generator"] = new JsonObject { ["revision"] = generator.Revision, ["inputs"] = inputFingerprint },
            ["coverage"] = new JsonObject
            {
                ["archives"] = archives.Count,
                ["sourceSetHash"] = SourceSetHash(archives)
            },
            ["observations"] = generator.Reduce(records)
        };
    }

    private static JsonObject MapWithCache(
        IFacetGenerator generator, CoveredArchive archive, string inputFingerprint, MapCache cache)
    {
        if (cache.TryGet(generator.Id, archive.Sha256, inputFingerprint, out string? cached))
            return (JsonObject)JsonNode.Parse(cached!)!;

        JsonObject record = generator.Map(archive.Archive);
        cache.Set(generator.Id, archive.Sha256, inputFingerprint, record.ToJsonString());
        return record;
    }

    private static string SourceSetHash(IReadOnlyList<CoveredArchive> archives)
    {
        string joined = string.Join('\n', archives.Select(a => a.Sha256).OrderBy(sha => sha, StringComparer.Ordinal));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(joined)))[..7];
    }
}

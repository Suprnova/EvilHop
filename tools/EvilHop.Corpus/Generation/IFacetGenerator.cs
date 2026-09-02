using EvilHop.Validation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Generation;

/// <summary>
/// One archive's contribution to a facet, paired with the archive's path so the reduce stage can
/// choose witnesses.
/// </summary>
/// <param name="Path">The path to the mapped archive, relative to the artifact root.</param>
/// <param name="Record">The archive's map-stage contribution.</param>
public readonly record struct MappedArchive(string Path, JsonObject Record);

/// <summary>
/// Which half of an archive's map pass a facet reads, and therefore when it runs.
/// </summary>
/// <remarks>
/// One loaded <see cref="Archive"/> is shared by every generator mapping it, and entering the asset
/// layer rebuilds the very blocks the block layer reads. Ordering the stages is what keeps a
/// block-scoped facet's output independent of whether an asset-scoped one ran first.
/// </remarks>
public enum MapStage
{
    /// <summary>Reads the block tree as loaded.</summary>
    Blocks,

    /// <summary>Reads the archive's assets, which requires - and on commit rewrites - the blocks that describe them.</summary>
    Assets
}

/// <summary>
/// A two-stage generator for one facet of the corpus inventory: <see cref="Map"/> reduces a single
/// archive to its contribution, and <see cref="Reduce"/> aggregates every covered archive's
/// contribution into the facet's committed shape. Map is the expensive, cacheable half; reduce is
/// cheap and always runs over the full covered set.
/// </summary>
public interface IFacetGenerator
{
    /// <summary>This facet's identifier, such as <c>"blockFields"</c>.</summary>
    string Id { get; }

    /// <summary>
    /// This facet's revision, hand-bumped whenever a change to <see cref="Map"/> or <see cref="Reduce"/>
    /// isn't otherwise visible from the <see cref="ValidationCatalogue"/> declarations in <see cref="Dependencies"/>.
    /// </summary>
    int Revision { get; }

    /// <summary>
    /// The <see cref="ValidationCatalogue"/> keys - observable IDs, rule IDs, enum names - this
    /// facet's output depends on.
    /// </summary>
    IEnumerable<string> Dependencies { get; }

    /// <summary>
    /// Which half of an archive's map pass this facet reads. Defaults to <see cref="MapStage.Blocks"/>.
    /// </summary>
    MapStage Stage => MapStage.Blocks;

    /// <summary>
    /// Produces one archive's contribution to this facet.
    /// </summary>
    /// <param name="archive">The archive to map.</param>
    /// <returns>The archive's contribution, as a JSON object safe to cache and later reduce.</returns>
    JsonObject Map(Archive archive);

    /// <summary>
    /// Aggregates every covered archive's contribution into this facet's committed shape.
    /// </summary>
    /// <param name="records">Every covered archive's <see cref="Map"/> output.</param>
    /// <returns>The facet's <c>observations</c> payload.</returns>
    JsonObject Reduce(IReadOnlyList<MappedArchive> records);

    /// <summary>
    /// Computes the first 7 hex characters of a SHA-256 over the sorted <c>key=digest</c> lines of
    /// every entry in <see cref="Dependencies"/>, so this facet goes stale exactly when one of its
    /// dependencies' declarations changes.
    /// </summary>
    /// <returns>The fingerprint, as a lowercase hex string.</returns>
    string InputFingerprint()
    {
        string joined = string.Join('\n', Dependencies
            .Select(key => $"{key}={ValidationCatalogue.Instance.DigestOf(key)}")
            .OrderBy(line => line, StringComparer.Ordinal));

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(joined)))[..7];
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Json;

/// <summary>
/// Renders a <see cref="JsonNode"/> tree the way every committed inventory file must be written:
/// two-space indent, LF line endings, and a trailing newline, so regenerating an unchanged tree
/// produces byte-identical output.
/// </summary>
/// <remarks>
/// This renders the tree exactly as given - it does not reorder object keys. A tree's key order has
/// to already be deterministic before it gets here: fixed, code-declared keys (an inventory's own
/// envelope, a facet's <c>generator</c>/<c>coverage</c>/<c>observations</c>) are deterministic by
/// construction, and the one place a key set is genuinely data-driven -
/// <see cref="Generation.BlockFieldsFacetGenerator"/>'s per-observable <c>observations</c> - sorts it
/// explicitly at the source, rather than leaving it to a blanket re-sort here that would also flatten
/// the deliberate, human-readable order of everything else.
/// </remarks>
public static class DeterministicJson
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>
    /// Renders <paramref name="node"/> to its deterministic, committable text form.
    /// </summary>
    /// <param name="node">The tree to serialize.</param>
    /// <returns>The serialized JSON, LF-terminated.</returns>
    public static string Serialize(JsonNode? node)
    {
        string json = node?.ToJsonString(Options) ?? "null";
        return json.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
    }
}

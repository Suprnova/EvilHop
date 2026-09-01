using System.Text.Json;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Json;

/// <summary>
/// Serializes a <see cref="JsonNode"/> tree the way every committed inventory file must be written:
/// object keys sorted ordinally, two-space indent, LF line endings, and a trailing newline, so
/// regenerating an unchanged tree produces byte-identical output.
/// </summary>
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
        string json = Sort(node)?.ToJsonString(Options) ?? "null";
        return json.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
    }

    private static JsonNode? Sort(JsonNode? node) => node switch
    {
        JsonObject obj => SortObject(obj),
        JsonArray array => SortArray(array),
        _ => node?.DeepClone()
    };

    private static JsonObject SortObject(JsonObject obj)
    {
        var sorted = new JsonObject();
        foreach (string key in obj.Select(property => property.Key).OrderBy(key => key, StringComparer.Ordinal))
            sorted[key] = Sort(obj[key]);
        return sorted;
    }

    private static JsonArray SortArray(JsonArray array)
    {
        var sorted = new JsonArray();
        foreach (var item in array) sorted.Add(Sort(item));
        return sorted;
    }
}

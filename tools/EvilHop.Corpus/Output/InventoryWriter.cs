using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Output;

/// <summary>
/// Emits an <see cref="InventoryBuilder"/>'s accumulated state as the committed inventory JSON.
/// Deterministic - every key and value array is sorted, so the same corpus always produces a
/// byte-identical file.
/// </summary>
internal static class InventoryWriter
{
    // The default encoder escapes characters like '+' (e.g. a UTC offset becomes "+00:00") on
    // the assumption the JSON might be embedded in HTML. This file is only ever read as JSON, so
    // that protection just adds noise to a file meant to be readable in a diff.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Writes <paramref name="builder"/>'s accumulated state to <paramref name="path"/> as indented JSON.
    /// </summary>
    public static void Write(string path, InventoryBuilder builder)
    {
        var root = new JsonObject
        {
            ["builds"] = BuildBuildsArray(builder),
            ["fields"] = BuildFieldsObject(builder),
            ["invariants"] = BuildInvariantsObject(builder)
        };

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, root.ToJsonString(SerializerOptions) + Environment.NewLine);
    }

    private static JsonArray BuildBuildsArray(InventoryBuilder builder)
    {
        var array = new JsonArray();
        foreach (var (key, count) in builder.Builds.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            array.Add(new JsonObject { ["key"] = key, ["archives"] = count });
        return array;
    }

    private static JsonObject BuildFieldsObject(InventoryBuilder builder)
    {
        var fields = new JsonObject();
        foreach (var (key, accumulator) in builder.Fields.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            fields[key] = accumulator.ToSummary().ToJson();
        return fields;
    }

    private static JsonObject BuildInvariantsObject(InventoryBuilder builder)
    {
        var invariants = new JsonObject();
        foreach (var invariant in builder.Invariants.OrderBy(i => i.Name, StringComparer.Ordinal))
            invariants[invariant.Name] = invariant.ToJson();
        return invariants;
    }
}

using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Extraction;

/// <summary>
/// One distinct value recorded for a field that stayed under the cardinality cap.
/// </summary>
/// <param name="SortKey">
/// The underlying value to sort by, for numeric-ish kinds - the JSON key itself is not always in
/// the right sort order (e.g. plain-number keys "10" &lt; "2" lexicographically). Null for kinds
/// whose key already sorts correctly (text) or has no natural order (collections).
/// </param>
internal sealed record ValueOccurrence(long Count, IReadOnlyCollection<string> Builds, string Exemplar, IComparable? SortKey)
{
    public JsonObject ToJson() => new()
    {
        ["count"] = Count,
        ["builds"] = new JsonArray([.. Builds.Order(StringComparer.Ordinal).Select(b => (JsonNode)b)]),
        ["exemplar"] = Exemplar
    };
}

/// <summary>
/// The final observation recorded for one field: either the full distinct set, under the cap, or a
/// degraded summary once it is exceeded.
/// </summary>
internal sealed class ValueSummary
{
    /// <summary><c>"set"</c> or <c>"summary"</c>.</summary>
    public required string Kind { get; init; }

    /// <summary>The full distinct value set. Only present when <see cref="Kind"/> is <c>"set"</c>.</summary>
    public IReadOnlyDictionary<string, ValueOccurrence>? Values { get; init; }

    /// <summary>The exact distinct count. Only present when <see cref="Kind"/> is <c>"summary"</c>.</summary>
    public long? Distinct { get; init; }

    /// <summary>The smallest observed value, for <see cref="ValueKind.Numeric"/>/<see cref="ValueKind.Hex"/> fields.</summary>
    public JsonNode? Min { get; init; }

    /// <summary>The largest observed value, for <see cref="ValueKind.Numeric"/>/<see cref="ValueKind.Hex"/> fields.</summary>
    public JsonNode? Max { get; init; }

    /// <summary>The shortest observed length, for <see cref="ValueKind.Text"/>/<see cref="ValueKind.Collection"/>/<see cref="ValueKind.Bytes"/> fields.</summary>
    public int? MinLength { get; init; }

    /// <summary>The longest observed length, for <see cref="ValueKind.Text"/>/<see cref="ValueKind.Collection"/>/<see cref="ValueKind.Bytes"/> fields.</summary>
    public int? MaxLength { get; init; }

    public JsonObject ToJson()
    {
        if (Kind == "set")
        {
            var values = new JsonObject();
            foreach (var (key, occurrence) in Values!.OrderBy(kv => kv, ValueOrder.Instance))
                values[key] = occurrence.ToJson();

            return new JsonObject { ["kind"] = "set", ["values"] = values };
        }

        var summary = new JsonObject { ["kind"] = "summary" };
        if (Distinct is not null) summary["distinct"] = Distinct;
        if (Min is not null) summary["min"] = Min;
        if (Max is not null) summary["max"] = Max;
        if (MinLength is not null) summary["minLength"] = MinLength;
        if (MaxLength is not null) summary["maxLength"] = MaxLength;
        return summary;
    }

    /// <summary>
    /// Orders values by their numeric <see cref="ValueOccurrence.SortKey"/> when every entry has
    /// one; otherwise falls back to an ordinal sort of the JSON key itself.
    /// </summary>
    private sealed class ValueOrder : IComparer<KeyValuePair<string, ValueOccurrence>>
    {
        public static readonly ValueOrder Instance = new();

        public int Compare(KeyValuePair<string, ValueOccurrence> x, KeyValuePair<string, ValueOccurrence> y) =>
            x.Value.SortKey is not null && y.Value.SortKey is not null
                ? x.Value.SortKey.CompareTo(y.Value.SortKey)
                : string.CompareOrdinal(x.Key, y.Key);
    }
}

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
/// degraded summary once it is exceeded. Closed to <see cref="ValueSet"/> and <see cref="ValueDigest"/>
/// so a summary can never carry a mix of fields that only make sense for the other shape.
/// </summary>
internal abstract record ValueSummary
{
    public abstract JsonObject ToJson();
}

/// <summary>The full distinct value set, recorded while a field stays under the cardinality cap.</summary>
internal sealed record ValueSet(IReadOnlyDictionary<string, ValueOccurrence> Values) : ValueSummary
{
    public override JsonObject ToJson()
    {
        var values = new JsonObject();
        foreach (var (key, occurrence) in Values.OrderBy(kv => kv, ValueOrder.Instance))
            values[key] = occurrence.ToJson();

        return new JsonObject { ["kind"] = "set", ["values"] = values };
    }

    /// <summary>
    /// Orders values by their numeric <see cref="ValueOccurrence.SortKey"/> when the kind provides
    /// one (every entry in a given set does, uniformly - it comes from the field's single
    /// <see cref="FieldKind"/>); otherwise falls back to an ordinal sort of the JSON key itself.
    /// </summary>
    private sealed class ValueOrder : IComparer<KeyValuePair<string, ValueOccurrence>>
    {
        public static readonly ValueOrder Instance = new();

        public int Compare(KeyValuePair<string, ValueOccurrence> x, KeyValuePair<string, ValueOccurrence> y) =>
            x.Value.SortKey is { } xKey && y.Value.SortKey is { } yKey
                ? xKey.CompareTo(yKey)
                : string.CompareOrdinal(x.Key, y.Key);
    }
}

/// <summary>
/// A degraded summary, once a field's distinct value count exceeds the cardinality cap.
/// </summary>
/// <param name="Distinct">
/// The exact distinct count. Null for <see cref="BytesKind"/>, whose contents are never tracked for
/// cardinality in the first place.
/// </param>
/// <param name="Statistic">The field's kind-specific range - value min/max, or a length range.</param>
internal sealed record ValueDigest(long? Distinct, IValueStatistic Statistic) : ValueSummary
{
    public override JsonObject ToJson()
    {
        var summary = new JsonObject { ["kind"] = "summary" };
        if (Distinct is not null) summary["distinct"] = Distinct;
        Statistic.WriteTo(summary);
        return summary;
    }
}

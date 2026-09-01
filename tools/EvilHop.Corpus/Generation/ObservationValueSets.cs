using EvilHop.Validation;
using System.Buffers.Binary;
using System.Text;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Generation;

/// <summary>
/// Reduces a bag of per-archive occurrences into the <c>ValueSet</c> shape every facet's
/// <c>observations</c> is built from - distinct values with counts and witnesses for
/// <see cref="ObservableCardinality.Enumerated"/>, a single bitwise union for
/// <see cref="ObservableCardinality.Bitmask"/>. Takes a value's shape rather than an
/// <see cref="Observable"/> itself, so it serves both catalogue-declared observables and facts a
/// generator computes directly and records under its own ID.
/// </summary>
internal static class ObservationValueSets
{
    /// <summary>
    /// The most distinct values an <see cref="ObservableCardinality.Enumerated"/> value may record
    /// before reduction fails, so a mis-declared cardinality is a loud error rather than a
    /// multi-megabyte commit.
    /// </summary>
    public const int MaxEnumeratedValues = 512;

    /// <summary>
    /// Reduces every occurrence of <paramref name="id"/> across <paramref name="records"/> into its
    /// <c>ValueSet</c>.
    /// </summary>
    /// <param name="id">The identifier the value was recorded under in each archive's map record.</param>
    /// <param name="cardinality">How the value's distinct occurrences should be recorded.</param>
    /// <param name="presentation">How the value's occurrences should be rendered.</param>
    /// <param name="records">Every covered archive's map-stage contribution.</param>
    /// <returns>The reduced <c>ValueSet</c>.</returns>
    public static JsonObject Reduce(
        string id, ObservableCardinality cardinality, ObservablePresentation presentation,
        IReadOnlyList<MappedArchive> records)
    {
        var occurrences = records.SelectMany(record => Occurrences(record, id)).ToList();

        return cardinality switch
        {
            ObservableCardinality.Bitmask => ReduceBitmask(presentation, occurrences),
            ObservableCardinality.Enumerated => ReduceEnumerated(id, presentation, occurrences),
            _ => throw new NotSupportedException($"ValueSet reduction doesn't support {cardinality} values yet.")
        };
    }

    private static IEnumerable<(string Path, object Value)> Occurrences(MappedArchive record, string id) =>
        record.Record[id] is JsonArray values
            ? values.Select(node => (record.Path, FromJsonValue((JsonValue)node!)))
            : [];

    private static JsonObject ReduceBitmask(ObservablePresentation presentation, IReadOnlyList<(string Path, object Value)> occurrences)
    {
        uint union = occurrences.Aggregate(0u, (acc, occurrence) => acc | (uint)occurrence.Value);

        return new JsonObject
        {
            ["kind"] = "bitmask",
            ["presentation"] = presentation.ToString().ToLowerInvariant(),
            ["union"] = union,
            ["display"] = $"0x{union:X8}"
        };
    }

    private static JsonObject ReduceEnumerated(string id, ObservablePresentation presentation, IReadOnlyList<(string Path, object Value)> occurrences)
    {
        var groups = occurrences.GroupBy(o => o.Value).OrderBy(g => g.Key, ValueComparer.Instance).ToList();
        if (groups.Count > MaxEnumeratedValues)
            throw new InvalidOperationException(
                $"'{id}' has {groups.Count} distinct values, past the {MaxEnumeratedValues}-value cap " +
                "for an enumerated value. Its cardinality may be mis-declared.");

        var values = new JsonArray();

        foreach (var group in groups)
        {
            var witnesses = new JsonArray();
            foreach (string path in group.Select(o => o.Path).Distinct().OrderBy(p => p, StringComparer.Ordinal).Take(2))
                witnesses.Add(path);

            var entry = new JsonObject();
            if (DisplayFor(group.Key, presentation) is { } display) entry["display"] = display;
            entry["value"] = ToJsonValue(group.Key);
            entry["count"] = group.Count();
            entry["witnesses"] = witnesses;

            values.Add(entry);
        }

        return new JsonObject
        {
            ["kind"] = "enumerated",
            ["presentation"] = presentation.ToString().ToLowerInvariant(),
            ["values"] = values
        };
    }

    private static string? DisplayFor(object value, ObservablePresentation presentation) => (presentation, value) switch
    {
        (ObservablePresentation.Hex, uint hex) => $"0x{hex:X8}",
        (ObservablePresentation.Fourcc, uint fourcc) => FourccDisplay(fourcc),
        _ => null
    };

    private static string FourccDisplay(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    /// Converts a value yielded by an observable, or computed directly by a generator, to JSON. A
    /// signed count (a required-child multiplicity, say) is always non-negative in practice and is
    /// narrowed to <see cref="uint"/> so every whole number shares one canonical .NET type - a fresh,
    /// still-in-memory <c>JsonValue&lt;int&gt;</c> and a <c>JsonValue&lt;JsonElement&gt;</c> re-read
    /// from a cache hit disagree on whether reading it back as <see cref="uint"/> succeeds, which
    /// would otherwise mix boxed <see cref="int"/> and <see cref="uint"/> zeros in the same reduce
    /// pass and break comparison.
    /// </summary>
    public static JsonValue ToJsonValue(object value) => value switch
    {
        uint u => JsonValue.Create(u),
        int i => JsonValue.Create(checked((uint)i)),
        bool b => JsonValue.Create(b),
        string s => JsonValue.Create(s),
        _ => throw new NotSupportedException($"Observable values of type '{value.GetType()}' aren't supported yet.")
    };

    private static object FromJsonValue(JsonValue value)
    {
        if (value.TryGetValue(out uint u)) return u;
        if (value.TryGetValue(out bool b)) return b;
        if (value.TryGetValue(out string? s)) return s!;
        throw new NotSupportedException($"Unsupported cached observation: {value}");
    }

    private sealed class ValueComparer : IComparer<object>
    {
        public static readonly ValueComparer Instance = new();

        public int Compare(object? x, object? y) => (x, y) switch
        {
            (uint a, uint b) => a.CompareTo(b),
            (bool a, bool b) => a.CompareTo(b),
            (string a, string b) => string.CompareOrdinal(a, b),
            _ => throw new NotSupportedException($"Can't compare '{x}' and '{y}'.")
        };
    }
}

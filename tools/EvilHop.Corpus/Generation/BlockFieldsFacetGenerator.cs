using EvilHop.Blocks;
using EvilHop.Validation;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Generation;

/// <summary>
/// Maps every archive- and block-scoped <see cref="Observable"/> to the values observed in one
/// archive, and reduces every covered archive's contribution into the <c>blockFields</c> facet's
/// <c>ValueSet</c>s.
/// </summary>
public sealed class BlockFieldsFacetGenerator : IFacetGenerator
{
    /// <summary>
    /// The most distinct values an <see cref="ObservableCardinality.Enumerated"/> observable may
    /// record before generation fails, so a mis-declared cardinality is a loud error rather than a
    /// multi-megabyte commit.
    /// </summary>
    public const int MaxEnumeratedValues = 512;

    /// <inheritdoc/>
    public string Id => "blockFields";

    /// <inheritdoc/>
    public int Revision => 1;

    /// <inheritdoc/>
    public IEnumerable<string> Dependencies => BlockObservables.Select(o => o.Id);

    private static IEnumerable<Observable> BlockObservables =>
        ValidationCatalogue.Instance.Observables.Where(o => o.Scope == ObservableScope.Block);

    /// <inheritdoc/>
    public JsonObject Map(Archive archive)
    {
        var record = new JsonObject();

        foreach (var block in Descendants(archive))
            foreach (var (observableId, value) in ValidationCatalogue.Instance.Observe(block))
            {
                if (record[observableId] is not JsonArray values) record[observableId] = values = [];
                values.Add(ToJsonValue(value));
            }

        return record;
    }

    /// <inheritdoc/>
    public JsonObject Reduce(IReadOnlyList<MappedArchive> records)
    {
        var observations = new JsonObject();
        foreach (var observable in BlockObservables) observations[observable.Id] = ReduceObservable(observable, records);
        return observations;
    }

    private static JsonObject ReduceObservable(Observable observable, IReadOnlyList<MappedArchive> records)
    {
        var occurrences = records
            .SelectMany(record => Occurrences(record, observable.Id))
            .ToList();

        return observable.Cardinality switch
        {
            ObservableCardinality.Bitmask => ReduceBitmask(observable, occurrences),
            ObservableCardinality.Enumerated => ReduceEnumerated(observable, occurrences),
            _ => throw new NotSupportedException(
                $"The blockFields facet doesn't support {observable.Cardinality} observables yet.")
        };
    }

    private static IEnumerable<(string Path, object Value)> Occurrences(MappedArchive record, string observableId) =>
        record.Record[observableId] is JsonArray values
            ? values.Select(node => (record.Path, FromJsonValue((JsonValue)node!)))
            : [];

    private static JsonObject ReduceBitmask(Observable observable, IReadOnlyList<(string Path, object Value)> occurrences)
    {
        uint union = occurrences.Aggregate(0u, (acc, occurrence) => acc | (uint)occurrence.Value);

        return new JsonObject
        {
            ["kind"] = "bitmask",
            ["presentation"] = observable.Presentation.ToString().ToLowerInvariant(),
            ["union"] = union,
            ["display"] = $"0x{union:X8}"
        };
    }

    private static JsonObject ReduceEnumerated(Observable observable, IReadOnlyList<(string Path, object Value)> occurrences)
    {
        var groups = occurrences.GroupBy(o => o.Value).OrderBy(g => g.Key, ValueComparer.Instance).ToList();
        if (groups.Count > MaxEnumeratedValues)
            throw new InvalidOperationException(
                $"'{observable.Id}' has {groups.Count} distinct values, past the {MaxEnumeratedValues}-value cap " +
                "for an enumerated observable. Its cardinality may be mis-declared.");

        var values = new JsonArray();

        foreach (var group in groups)
        {
            var witnesses = new JsonArray();
            foreach (string path in group.Select(o => o.Path).Distinct().OrderBy(p => p, StringComparer.Ordinal).Take(2))
                witnesses.Add(path);

            var entry = new JsonObject
            {
                ["value"] = ToJsonValue(group.Key),
                ["count"] = group.Count(),
                ["witnesses"] = witnesses
            };
            if (DisplayFor(group.Key, observable.Presentation) is { } display) entry["display"] = display;

            values.Add(entry);
        }

        return new JsonObject
        {
            ["kind"] = "enumerated",
            ["presentation"] = observable.Presentation.ToString().ToLowerInvariant(),
            ["values"] = values
        };
    }

    private static string? DisplayFor(object value, ObservablePresentation presentation) => (presentation, value) switch
    {
        (ObservablePresentation.Hex, uint hex) => $"0x{hex:X8}",
        _ => null
    };

    private static JsonValue ToJsonValue(object value) => value switch
    {
        uint u => JsonValue.Create(u),
        int i => JsonValue.Create(i),
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

    private static IEnumerable<Block> Descendants(Archive archive) => archive.Roots.SelectMany(Descendants);

    private static IEnumerable<Block> Descendants(Block block) => new[] { block }.Concat(block.Children.SelectMany(Descendants));

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

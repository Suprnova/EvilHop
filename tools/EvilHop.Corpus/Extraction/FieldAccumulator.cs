namespace EvilHop.Corpus.Extraction;

/// <summary>
/// Accumulates every observed value of a single field across the whole corpus and produces its
/// <see cref="ValueSummary"/>. One instance per <paramref name="fieldKey"/> (e.g.
/// <c>"AssetHeader.Type"</c>), persisting across archives.
/// </summary>
/// <param name="kind">How the field's CLR type is tracked and summarized.</param>
/// <param name="fieldKey">
/// The field's key, checked against <see cref="NeverDegrades"/> to decide whether cardinality alone
/// can degrade this field to a digest.
/// </param>
internal sealed class FieldAccumulator(FieldKind kind, string fieldKey)
{
    /// <summary>
    /// An override for fields that are closed, meaningful enumerations and stay a full
    /// <see cref="ValueSet"/> no matter how many distinct values are observed.
    /// </summary>
    private static readonly HashSet<string> NeverDegrades = ["AssetHeader.Type"];

    private const int CardinalityCap = 70;

    private sealed class Occurrence
    {
        public long Count;
        public readonly HashSet<string> Builds = [];
        public string? Exemplar;
        public IComparable? SortKey;
    }

    private readonly Dictionary<string, Occurrence> _topValues = [];
    private readonly HashSet<string> _allKeys = [];
    private readonly IValueStatistic _statistic = kind.CreateStatistic();
    private bool _degraded;

    /// <summary>
    /// Records one observed <paramref name="value"/>, attributed to <paramref name="build"/> and
    /// traceable to <paramref name="exemplarPath"/>.
    /// </summary>
    public void Record(object? value, string build, string exemplarPath)
    {
        _statistic.Observe(value);
        if (!kind.RecordsValues) return;

        string key = kind.FormatKey(value);
        bool isNewKey = _allKeys.Add(key);

        if (_degraded) return;

        if (_topValues.TryGetValue(key, out var occurrence))
        {
            occurrence.Count++;
            occurrence.Builds.Add(build);
            return;
        }

        if (!isNewKey || (_topValues.Count >= CardinalityCap && !NeverDegrades.Contains(fieldKey)))
        {
            _degraded = true;
            _topValues.Clear();
            return;
        }

        var newOccurrence = new Occurrence { Count = 1, Exemplar = exemplarPath, SortKey = kind.SortKey(value) };
        newOccurrence.Builds.Add(build);
        _topValues[key] = newOccurrence;
    }

    /// <summary>
    /// Produces the final <see cref="ValueSummary"/> for this field.
    /// </summary>
    public ValueSummary ToSummary()
    {
        if (kind.RecordsValues && !_degraded)
        {
            var values = _topValues.ToDictionary(
                kv => kv.Key,
                kv => new ValueOccurrence(kv.Value.Count, kv.Value.Builds, kv.Value.Exemplar!, kv.Value.SortKey));
            return new ValueSet(values);
        }

        return new ValueDigest(kind.RecordsValues ? _allKeys.Count : null, _statistic);
    }
}

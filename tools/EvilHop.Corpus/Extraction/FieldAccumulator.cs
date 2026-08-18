using System.Collections;

namespace EvilHop.Corpus.Extraction;

/// <summary>
/// Accumulates every observed value of a single field across the whole corpus and produces its
/// <see cref="ValueSummary"/>. One instance per field key (e.g. <c>"AssetHeader.Type"</c>),
/// persisting across archives.
/// </summary>
internal sealed class FieldAccumulator(ValueKind kind)
{
    private const int CardinalityCap = 64;

    private sealed class Occurrence
    {
        public long Count;
        public readonly HashSet<string> Builds = [];
        public string? Exemplar;
        public IComparable? SortKey;
    }

    private readonly Dictionary<string, Occurrence> _topValues = [];
    private readonly HashSet<string> _allKeys = [];
    private bool _degraded;

    private object? _min;
    private object? _max;
    private int? _minLength;
    private int? _maxLength;

    /// <summary>
    /// Records one observed <paramref name="value"/>, attributed to <paramref name="build"/> and
    /// traceable to <paramref name="exemplarPath"/>.
    /// </summary>
    public void Record(object? value, string build, string exemplarPath)
    {
        if (kind == ValueKind.Bytes)
        {
            UpdateLength(value is byte[] bytes ? bytes.Length : (int?)null);
            return;
        }

        if (kind is ValueKind.Numeric or ValueKind.Hex) UpdateMinMax(value);
        if (kind is ValueKind.Text or ValueKind.Collection) UpdateLength(LengthOf(value));

        string key = ValueFormatter.FormatKey(value, kind);
        bool isNewKey = _allKeys.Add(key);

        if (_degraded) return;

        if (_topValues.TryGetValue(key, out var occurrence))
        {
            occurrence.Count++;
            occurrence.Builds.Add(build);
            return;
        }

        if (!isNewKey || _topValues.Count >= CardinalityCap)
        {
            _degraded = true;
            _topValues.Clear();
            return;
        }

        // Text keys already sort correctly (and deterministically, culture-invariantly) via their own
        // Ordinal string comparison; only numeric-ish kinds need the underlying value to sort by magnitude.
        IComparable? sortKey = kind is ValueKind.Numeric or ValueKind.Hex ? value as IComparable : null;
        var newOccurrence = new Occurrence { Count = 1, Exemplar = exemplarPath, SortKey = sortKey };
        newOccurrence.Builds.Add(build);
        _topValues[key] = newOccurrence;
    }

    /// <summary>
    /// Produces the final <see cref="ValueSummary"/> for this field.
    /// </summary>
    public ValueSummary ToSummary()
    {
        if (kind == ValueKind.Bytes)
            return new ValueSummary { Kind = "summary", MinLength = _minLength, MaxLength = _maxLength };

        if (!_degraded)
        {
            var values = _topValues.ToDictionary(
                kv => kv.Key,
                kv => new ValueOccurrence(kv.Value.Count, kv.Value.Builds, kv.Value.Exemplar!, kv.Value.SortKey));
            return new ValueSummary { Kind = "set", Values = values };
        }

        return new ValueSummary
        {
            Kind = "summary",
            Distinct = _allKeys.Count,
            Min = ValueFormatter.ToJsonNode(_min, kind),
            Max = ValueFormatter.ToJsonNode(_max, kind),
            MinLength = _minLength,
            MaxLength = _maxLength
        };
    }

    private void UpdateMinMax(object? value)
    {
        if (value is not IComparable comparable) return;
        if (_min is null || comparable.CompareTo(_min) < 0) _min = value;
        if (_max is null || comparable.CompareTo(_max) > 0) _max = value;
    }

    private void UpdateLength(int? length)
    {
        if (length is null) return;
        _minLength = _minLength is null ? length : Math.Min(_minLength.Value, length.Value);
        _maxLength = _maxLength is null ? length : Math.Max(_maxLength.Value, length.Value);
    }

    private static int? LengthOf(object? value) => value switch
    {
        null => null,
        string s => s.Length,
        IEnumerable e => e.Cast<object?>().Count(),
        _ => null
    };
}

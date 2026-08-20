using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Extraction;

/// <summary>
/// Tracks the smallest and largest observed value, by <see cref="IComparable"/> order. Used by
/// <see cref="NumericKind"/> and <see cref="HexKind"/>; non-<see cref="IComparable"/> values are
/// ignored.
/// </summary>
internal sealed class ValueRange(Func<object?, JsonNode?> toJsonNode) : IValueStatistic
{
    private object? _min;
    private object? _max;

    public void Observe(object? value)
    {
        if (value is not IComparable comparable) return;
        if (_min is null || comparable.CompareTo(_min) < 0) _min = value;
        if (_max is null || comparable.CompareTo(_max) > 0) _max = value;
    }

    public void WriteTo(JsonObject target)
    {
        if (toJsonNode(_min) is { } min) target["min"] = min;
        if (toJsonNode(_max) is { } max) target["max"] = max;
    }
}

/// <summary>
/// Tracks the shortest and longest observed length. Used by <see cref="TextKind"/>,
/// <see cref="CollectionKind"/>, and <see cref="BytesKind"/>, each supplying its own notion of
/// "length" - string length, element count, or byte count.
/// </summary>
internal sealed class LengthRange(Func<object?, int?> lengthOf) : IValueStatistic
{
    private int? _min;
    private int? _max;

    public void Observe(object? value)
    {
        int? length = lengthOf(value);
        if (length is null) return;
        _min = _min is null ? length : Math.Min(_min.Value, length.Value);
        _max = _max is null ? length : Math.Max(_max.Value, length.Value);
    }

    public void WriteTo(JsonObject target)
    {
        if (_min is not null) target["minLength"] = _min;
        if (_max is not null) target["maxLength"] = _max;
    }
}

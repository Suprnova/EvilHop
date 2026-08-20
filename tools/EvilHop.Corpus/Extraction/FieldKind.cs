using System.Collections;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Extraction;

/// <summary>
/// How a field's CLR type is tracked and summarized: which statistic it degrades to past the
/// cardinality cap, and how its values are formatted along the way. One singleton instance per
/// kind - each owns its own formatting and statistic, so adding a kind never requires touching
/// <see cref="FieldAccumulator"/>, <see cref="ValueSummary"/>, or <see cref="ValueFormatter"/>.
/// </summary>
internal abstract record FieldKind
{
    /// <summary>Numbers, enums, and dates - degrades to a <c>min</c>/<c>max</c> range.</summary>
    public static readonly FieldKind Numeric = new NumericKind();

    /// <summary>
    /// Numbers that read better as hex than decimal (e.g. a fill byte), but aren't C# enums.
    /// Behaves exactly like <see cref="Numeric"/> otherwise.
    /// </summary>
    public static readonly FieldKind Hex = new HexKind();

    /// <summary>Strings - degrades to a <c>minLength</c>/<c>maxLength</c> range.</summary>
    public static readonly FieldKind Text = new TextKind();

    /// <summary>Non-<c>byte[]</c> collections - degrades to an element-count <c>minLength</c>/<c>maxLength</c> range.</summary>
    public static readonly FieldKind Collection = new CollectionKind();

    /// <summary><c>byte[]</c> - contents are never recorded, only a length range.</summary>
    public static readonly FieldKind Bytes = new BytesKind();

    /// <summary>Whether distinct values are tracked for cardinality. False only for <see cref="Bytes"/>.</summary>
    public virtual bool RecordsValues => true;

    /// <summary>Creates the statistic this kind degrades to once a field exceeds the cardinality cap.</summary>
    public abstract IValueStatistic CreateStatistic();

    /// <summary>
    /// The string key used to identify <paramref name="value"/> for cardinality purposes - also the
    /// literal JSON object key when the field stays under the cap.
    /// </summary>
    public string FormatKey(object? value) => value switch
    {
        null => "null",
        Enum e => ValueFormatter.FormatEnum(e),
        DateTimeOffset dto => ValueFormatter.FormatDate(dto),
        DateTime dt => ValueFormatter.FormatDate(dt),
        string s => s,
        _ => FormatLeaf(value)
    };

    /// <summary>
    /// The value to sort by, for kinds whose formatted key doesn't already sort correctly (e.g. plain-
    /// number keys "10" &lt; "2" lexicographically). Null for kinds whose key sorts correctly on its own.
    /// </summary>
    public virtual IComparable? SortKey(object? value) => null;

    /// <summary>Renders <paramref name="value"/> as a JSON node for <c>min</c>/<c>max</c> output.</summary>
    public JsonNode? ToJsonNode(object? value) => value switch
    {
        null => null,
        Enum e => JsonValue.Create(ValueFormatter.FormatEnum(e)),
        DateTimeOffset dto => JsonValue.Create(ValueFormatter.FormatDate(dto)),
        DateTime dt => JsonValue.Create(ValueFormatter.FormatDate(dt)),
        _ => ToJsonLeaf(value)
    };

    /// <summary>Renders <paramref name="value"/> as it appears in <c>--dump</c> output.</summary>
    public virtual JsonValue? RenderForDump(object? value) => JsonValue.Create(FormatKey(value));

    /// <summary>Formats a value that isn't null, an enum, a date, or a string.</summary>
    protected virtual string FormatLeaf(object value) => ValueFormatter.FormatScalar(value);

    /// <summary>Renders a value that isn't null, an enum, or a date, as a JSON node.</summary>
    protected virtual JsonNode? ToJsonLeaf(object value) => ValueFormatter.ToJsonNode(value);
}

internal record NumericKind : FieldKind
{
    public override IValueStatistic CreateStatistic() => new ValueRange(ToJsonNode);
    public override IComparable? SortKey(object? value) => value as IComparable;
}

internal sealed record HexKind : NumericKind
{
    protected override string FormatLeaf(object value) => ValueFormatter.FormatHex(value);
    protected override JsonNode? ToJsonLeaf(object value) => JsonValue.Create(ValueFormatter.FormatHex(value));
}

internal sealed record TextKind : FieldKind
{
    public override IValueStatistic CreateStatistic() => new LengthRange(v => (v as string)?.Length);
}

internal sealed record CollectionKind : FieldKind
{
    public override IValueStatistic CreateStatistic() =>
        new LengthRange(v => v is IEnumerable e ? e.Cast<object?>().Count() : null);

    protected override string FormatLeaf(object value) =>
        value is IEnumerable e ? FormatCollection(e) : ValueFormatter.FormatScalar(value);

    private static string FormatCollection(IEnumerable enumerable) =>
        $"[{string.Join(",", enumerable.Cast<object?>().Select(ValueFormatter.FormatScalar))}]";
}

internal sealed record BytesKind : FieldKind
{
    public override bool RecordsValues => false;
    public override IValueStatistic CreateStatistic() => new LengthRange(v => v is byte[] b ? b.Length : null);
    public override JsonValue? RenderForDump(object? value) => JsonValue.Create(value is byte[] b ? b.Length : 0);
}

/// <summary>
/// Classifies a field's declared CLR type into a <see cref="FieldKind"/>.
/// </summary>
internal static class FieldKindClassifier
{
    private static readonly HashSet<Type> NumericTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal), typeof(bool),
        typeof(DateTimeOffset), typeof(DateTime)
    ];

    /// <summary>
    /// Classifies <paramref name="type"/>, the declared type of a field property.
    /// </summary>
    public static FieldKind Classify(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(byte[])) return FieldKind.Bytes;
        if (type == typeof(string)) return FieldKind.Text;
        if (type.IsEnum || NumericTypes.Contains(type)) return FieldKind.Numeric;
        if (typeof(IEnumerable).IsAssignableFrom(type)) return FieldKind.Collection;
        return FieldKind.Numeric;
    }
}

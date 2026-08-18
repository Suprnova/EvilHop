namespace EvilHop.Corpus.Extraction;

/// <summary>
/// How a field's CLR type should be summarized once it degrades past the cardinality cap.
/// </summary>
internal enum ValueKind
{
    /// <summary>Numbers, enums, and dates - degrades to a <c>min</c>/<c>max</c> range.</summary>
    Numeric,

    /// <summary>
    /// Numbers that read better as hex than decimal (e.g. a fill byte), but aren't C# enums.
    /// Behaves exactly like <see cref="Numeric"/> otherwise.
    /// </summary>
    Hex,

    /// <summary>Strings - degrades to a <c>minLength</c>/<c>maxLength</c> range.</summary>
    Text,

    /// <summary>Non-<c>byte[]</c> collections - degrades to an element-count <c>minLength</c>/<c>maxLength</c> range.</summary>
    Collection,

    /// <summary><c>byte[]</c> - contents are never recorded, only a length range.</summary>
    Bytes
}

/// <summary>
/// Classifies a field's declared CLR type into a <see cref="ValueKind"/>.
/// </summary>
internal static class ValueKindClassifier
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
    public static ValueKind Classify(Type type)
    {
        if (type == typeof(byte[])) return ValueKind.Bytes;
        if (type == typeof(string)) return ValueKind.Text;
        if (type.IsEnum || NumericTypes.Contains(type)) return ValueKind.Numeric;
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type)) return ValueKind.Collection;
        return ValueKind.Numeric;
    }
}

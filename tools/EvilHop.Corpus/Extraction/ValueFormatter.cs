using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Extraction;

/// <summary>
/// Formats extracted field values into the string keys used for cardinality tracking and the JSON
/// nodes used for <c>min</c>/<c>max</c> output. Enum-typed values render as their ASCII form when
/// every byte is printable (many, like <see cref="EvilHop.Common.AssetType"/>, are FourCCs - e.g.
/// <c>ANIM</c> for <c>Animation</c>), falling back to hex otherwise; everything else renders as
/// plain numbers or literal strings.
/// </summary>
internal static class ValueFormatter
{
    /// <summary>
    /// Formats <paramref name="value"/> into the string key used to identify it for cardinality
    /// purposes - also the literal JSON object key when the field stays under the cap.
    /// </summary>
    public static string FormatKey(object? value, ValueKind kind) => value switch
    {
        null => "null",
        Enum e => FormatEnum(e),
        _ when kind == ValueKind.Hex => FormatHex(value!),
        DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        string s => s,
        IEnumerable e when kind == ValueKind.Collection => FormatCollection(e),
        _ => FormatScalar(value)
    };

    /// <summary>
    /// Renders <paramref name="value"/> as a JSON node for <c>min</c>/<c>max</c> output.
    /// </summary>
    public static JsonNode? ToJsonNode(object? value, ValueKind kind) => value switch
    {
        null => null,
        Enum e => JsonValue.Create(FormatEnum(e)),
        _ when kind == ValueKind.Hex => JsonValue.Create(FormatHex(value!)),
        DateTimeOffset dto => JsonValue.Create(dto.ToString("O", CultureInfo.InvariantCulture)),
        DateTime dt => JsonValue.Create(dt.ToString("O", CultureInfo.InvariantCulture)),
        uint ui => JsonValue.Create(ui),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        ulong ul => JsonValue.Create(ul),
        short sh => JsonValue.Create(sh),
        ushort ush => JsonValue.Create(ush),
        byte b => JsonValue.Create(b),
        sbyte sb => JsonValue.Create(sb),
        double d => JsonValue.Create(d),
        float f => JsonValue.Create(f),
        decimal m => JsonValue.Create(m),
        bool bo => JsonValue.Create(bo),
        _ => JsonValue.Create(value.ToString())
    };

    private static string FormatCollection(IEnumerable enumerable) =>
        $"[{string.Join(",", enumerable.Cast<object?>().Select(FormatScalar))}]";

    private static string FormatScalar(object? value) => value switch
    {
        null => "null",
        bool b => b ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "null"
    };

    private static string FormatEnum(Enum value)
    {
        var underlyingType = Enum.GetUnderlyingType(value.GetType());
        ulong numeric = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        int hexDigits = HexDigitsFor(underlyingType);

        return TryFormatAsAscii(numeric, hexDigits / 2, out string ascii) ? ascii : FormatHex(numeric, hexDigits);
    }

    /// <summary>
    /// Renders <paramref name="value"/>'s big-endian bytes as ASCII, the way its on-disk FourCC
    /// would read (e.g. <c>0x414E494D</c> as <c>ANIM</c>) - only when every byte is printable, so a
    /// value like <see cref="EvilHop.Common.AssetType.Unknown"/> (all zero bytes) still falls back to hex.
    /// </summary>
    private static bool TryFormatAsAscii(ulong value, int byteCount, out string ascii)
    {
        var bytes = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
            bytes[i] = (byte)(value >> (8 * (byteCount - 1 - i)));

        if (bytes.Any(b => b is < 0x20 or > 0x7E))
        {
            ascii = "";
            return false;
        }

        ascii = Encoding.ASCII.GetString(bytes);
        return true;
    }

    private static string FormatHex(object value) => value switch
    {
        byte or sbyte => FormatHex(Convert.ToUInt64(value, CultureInfo.InvariantCulture), 2),
        short or ushort => FormatHex(Convert.ToUInt64(value, CultureInfo.InvariantCulture), 4),
        long or ulong => FormatHex(Convert.ToUInt64(value, CultureInfo.InvariantCulture), 16),
        _ => FormatHex(Convert.ToUInt64(value, CultureInfo.InvariantCulture), 8)
    };

    private static string FormatHex(ulong value, int digits) => $"0x{value.ToString($"X{digits}", CultureInfo.InvariantCulture)}";

    private static int HexDigitsFor(Type integralType) => Type.GetTypeCode(integralType) switch
    {
        TypeCode.Byte or TypeCode.SByte => 2,
        TypeCode.Int16 or TypeCode.UInt16 => 4,
        TypeCode.Int64 or TypeCode.UInt64 => 16,
        _ => 8
    };
}

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Extraction;

/// <summary>
/// CLR-type-driven rendering shared across every <see cref="FieldKind"/> - dispatch here depends only
/// on a value's runtime type, never on which kind is observing it. Enum-typed values render as their
/// ASCII form when every byte is printable (many, like <see cref="EvilHop.Common.AssetType"/>, are
/// FourCCs - e.g. <c>ANIM</c> for <c>Animation</c>), falling back to hex otherwise.
/// </summary>
internal static class ValueFormatter
{
    /// <summary>Formats a date/time value in a round-trippable, culture-invariant form.</summary>
    public static string FormatDate(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    /// <inheritdoc cref="FormatDate(DateTimeOffset)"/>
    public static string FormatDate(DateTime value) => value.ToString("O", CultureInfo.InvariantCulture);

    /// <summary>Formats a non-null, non-collection value as its plain string form.</summary>
    public static string FormatScalar(object? value) => value switch
    {
        null => "null",
        bool b => b ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "null"
    };

    /// <summary>Renders a non-null numeric value as a JSON node, matching its underlying CLR numeric type.</summary>
    public static JsonNode? ToJsonNode(object value) => value switch
    {
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

    public static string FormatEnum(Enum value)
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
            ascii = String.Empty;
            return false;
        }

        ascii = Encoding.ASCII.GetString(bytes);
        return true;
    }

    public static string FormatHex(object value) => value switch
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

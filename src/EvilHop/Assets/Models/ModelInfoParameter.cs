using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EvilHop.Assets;

/// <summary>
/// One named parameter read by an NPC's AI code by hashing the parameter's name and matching it
/// against <see cref="HashId"/>. Defined by <see cref="AssetType.ModelInfo"/>, and reused verbatim by
/// <see cref="AssetType.NPCSettings"/>.
/// </summary>
public sealed class ModelInfoParameter
{
    /// <summary>A BKDR hash of this parameter's name.</summary>
    public uint HashId { get; set; }

    /// <summary>
    /// This parameter's value, usually a floating-point number or a vector written as text (for
    /// example <c>"1.0"</c> or <c>"{ 3.0, 3.0, 3.0 }"</c>).
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// Trailing padding bytes between the null terminator and the 4-byte boundary.
    /// Preserved directly to reproduce uninitialized memory on round-trip.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Raw padding bytes with no field structure of its own; a byte[] is the natural representation.")]
    public byte[] Padding { get; set; } = [];

    /// <summary>Reads one entry from <paramref name="reader"/>'s current position.</summary>
    internal static ModelInfoParameter Read(EndianReader reader, FormatProfile _)
    {
        uint hashId = reader.ReadUInt32();
        byte wordLength = reader.ReadByte();
        int stringAreaLength = (wordLength + 1) * 4 - 1;
        byte[] stringArea = reader.ReadBytes(stringAreaLength);
        int nullIndex = Array.IndexOf(stringArea, (byte)0);
        string value = Encoding.Latin1.GetString(stringArea, 0, nullIndex >= 0 ? nullIndex : stringArea.Length);
        byte[] padding = nullIndex >= 0 && nullIndex + 1 < stringArea.Length
            ? stringArea[(nullIndex + 1)..]
            : [];
        return new ModelInfoParameter { HashId = hashId, Value = value, Padding = padding };
    }

    /// <summary>Writes <paramref name="value"/> to <paramref name="writer"/>.</summary>
    internal static void Write(ModelInfoParameter value, EndianWriter writer, FormatProfile _)
    {
        byte[] valueBytes = Encoding.Latin1.GetBytes(value.Value);
        int stringBytesWithNull = valueBytes.Length + 1;
        int naturalPaddingLength = (4 - ((1 + stringBytesWithNull) % 4)) % 4;

        byte[] rawPadding = value.Padding ?? [];
        byte[] padding = (1 + stringBytesWithNull + rawPadding.Length) % 4 == 0 && rawPadding.Length >= naturalPaddingLength
            ? rawPadding
            : new byte[naturalPaddingLength];

        int totalRegion = 1 + stringBytesWithNull + padding.Length;
        byte wordLength = (byte)(totalRegion / 4 - 1);

        writer.Write(value.HashId);
        writer.Write(wordLength);
        writer.Write(valueBytes);
        writer.Write((byte)0);
        writer.Write(padding);
    }
}

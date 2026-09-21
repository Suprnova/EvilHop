using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// Scrolls the UI's UV offset by <see cref="AmountU"/>/<see cref="AmountV"/>, relative to the
/// original UV set on its <see cref="AssetType.UI"/>.
/// </summary>
public sealed class UVScrollCommand : UIMotionCommand
{
    /// <summary>How much to scroll the U (horizontal) coordinate (1.0 = full texture width).</summary>
    public float AmountU { get; set; }

    /// <summary>How much to scroll the V (vertical) coordinate (1.0 = full texture height).</summary>
    public float AmountV { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.UVScroll;

    private protected override int FieldsSize => 8;

    internal static new UVScrollCommand Read(EndianReader reader, FormatProfile _) => new()
    {
        AmountU = reader.ReadSingle(),
        AmountV = reader.ReadSingle(),
    };

    internal static void Write(UVScrollCommand value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.AmountU);
        writer.Write(value.AmountV);
    }
}

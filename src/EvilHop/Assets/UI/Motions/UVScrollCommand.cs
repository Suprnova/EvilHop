using EvilHop.Common;
using EvilHop.Primitives;

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

    internal override void ReadFields(EndianReader reader)
    {
        AmountU = reader.ReadSingle();
        AmountV = reader.ReadSingle();
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.Write(AmountU);
        writer.Write(AmountV);
    }
}

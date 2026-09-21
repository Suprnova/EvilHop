using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// Scales the UI by <see cref="AmountX"/>/<see cref="AmountY"/>, relative to the original scale set
/// on its <see cref="AssetType.UI"/>.
/// </summary>
public sealed class ScaleCommand : UIMotionCommand
{
    /// <summary>The ratio to scale the X axis by (2.0 = 200%, 0.5 = 50%, etc.).</summary>
    public float AmountX { get; set; }

    /// <summary>The ratio to scale the Y axis by (2.0 = 200%, 0.5 = 50%, etc.).</summary>
    public float AmountY { get; set; }

    /// <summary>
    /// When set, the UI scales from its center, offset by <see cref="CenterOffsetX"/>/
    /// <see cref="CenterOffsetY"/>, instead of its top-left corner.
    /// </summary>
    public bool CenterPivot { get; set; }

    /// <summary>
    /// The scale center point's X axis offset, in pixels. Only applies when
    /// <see cref="CenterPivot"/> is set.
    /// </summary>
    public float CenterOffsetX { get; set; }

    /// <summary>
    /// The scale center point's Y axis offset, in pixels. Only applies when
    /// <see cref="CenterPivot"/> is set.
    /// </summary>
    public float CenterOffsetY { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.Scale;

    private protected override int FieldsSize => 20;

    internal static new ScaleCommand Read(EndianReader reader, FormatProfile _)
    {
        var value = new ScaleCommand
        {
            AmountX = reader.ReadSingle(),
            AmountY = reader.ReadSingle(),
            CenterPivot = reader.ReadByte() != 0,
        };
        reader.ReadBytes(3); // padding, always zero
        value.CenterOffsetX = reader.ReadSingle();
        value.CenterOffsetY = reader.ReadSingle();
        return value;
    }

    internal static void Write(ScaleCommand value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.AmountX);
        writer.Write(value.AmountY);
        writer.Write((byte)(value.CenterPivot ? 1 : 0));
        writer.Write(new byte[3]); // padding
        writer.Write(value.CenterOffsetX);
        writer.Write(value.CenterOffsetY);
    }
}

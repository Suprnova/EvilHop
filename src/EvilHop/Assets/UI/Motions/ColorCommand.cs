using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// Changes the UI's color from <see cref="StartColor"/> to <see cref="EndColor"/>, overwriting its
/// previous color.
/// </summary>
public sealed class ColorCommand : UIMotionCommand
{
    /// <summary>The starting color.</summary>
    public Rgb StartColor { get; set; }

    /// <summary>The ending color.</summary>
    public Rgb EndColor { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.Color;

    private protected override int FieldsSize => 8;

    internal override void ReadFields(EndianReader reader)
    {
        StartColor = reader.ReadRgb24();
        EndColor = reader.ReadRgb24();
        reader.ReadBytes(2); // padding, always zero
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.WriteRgb24(StartColor);
        writer.WriteRgb24(EndColor);
        writer.Write(new byte[2]); // padding
    }
}

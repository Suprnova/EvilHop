using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// Changes the UI's scale from <see cref="StartX"/>/<see cref="StartY"/> to <see cref="EndX"/>/
/// <see cref="EndY"/>, overwriting its previous scale.
/// </summary>
public sealed class AbsoluteScaleCommand : UIMotionCommand
{
    /// <summary>The starting X axis scale, as a ratio (2.0 = 200%, 0.5 = 50%, etc.).</summary>
    public float StartX { get; set; }

    /// <summary>The starting Y axis scale, as a ratio (2.0 = 200%, 0.5 = 50%, etc.).</summary>
    public float StartY { get; set; }

    /// <summary>The ending X axis scale, as a ratio (2.0 = 200%, 0.5 = 50%, etc.).</summary>
    public float EndX { get; set; }

    /// <summary>The ending Y axis scale, as a ratio (2.0 = 200%, 0.5 = 50%, etc.).</summary>
    public float EndY { get; set; }

    /// <summary>When set, the UI scales from its center instead of its top-left corner.</summary>
    public bool CenterPivot { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public byte TextScale { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.AbsoluteScale;

    private protected override int FieldsSize => 20;

    internal static new AbsoluteScaleCommand Read(EndianReader reader, FormatProfile _)
    {
        var value = new AbsoluteScaleCommand
        {
            StartX = reader.ReadSingle(),
            StartY = reader.ReadSingle(),
            EndX = reader.ReadSingle(),
            EndY = reader.ReadSingle(),
            CenterPivot = reader.ReadByte() != 0,
            TextScale = reader.ReadByte(),
        };
        reader.ReadBytes(2); // padding, always zero
        return value;
    }

    internal static void Write(AbsoluteScaleCommand value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.StartX);
        writer.Write(value.StartY);
        writer.Write(value.EndX);
        writer.Write(value.EndY);
        writer.Write((byte)(value.CenterPivot ? 1 : 0));
        writer.Write(value.TextScale);
        writer.Write(new byte[2]); // padding
    }
}

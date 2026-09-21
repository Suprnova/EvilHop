using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// Rotates the UI by <see cref="Rotation"/> degrees, relative to its last rotation.
/// </summary>
public sealed class RotateCommand : UIMotionCommand
{
    /// <summary>How far to rotate, in degrees. Positive is clockwise, negative is counter-clockwise.</summary>
    public float Rotation { get; set; }

    /// <summary>The rotation pivot's X axis offset, in pixels.</summary>
    public float CenterOffsetX { get; set; }

    /// <summary>The rotation pivot's Y axis offset, in pixels.</summary>
    public float CenterOffsetY { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.Rotate;

    private protected override int FieldsSize => 12;

    internal override void ReadFields(EndianReader reader)
    {
        Rotation = reader.ReadSingle();
        CenterOffsetX = reader.ReadSingle();
        CenterOffsetY = reader.ReadSingle();
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.Write(Rotation);
        writer.Write(CenterOffsetX);
        writer.Write(CenterOffsetY);
    }
}

using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// Moves the UI by <see cref="DistanceX"/>/<see cref="DistanceY"/> pixels, relative to its last
/// position.
/// </summary>
public sealed class MoveCommand : UIMotionCommand
{
    /// <summary>How far to move on the X axis, in pixels.</summary>
    public float DistanceX { get; set; }

    /// <summary>How far to move on the Y axis, in pixels.</summary>
    public float DistanceY { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.Move;

    private protected override int FieldsSize => 8;

    internal override void ReadFields(EndianReader reader)
    {
        DistanceX = reader.ReadSingle();
        DistanceY = reader.ReadSingle();
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.Write(DistanceX);
        writer.Write(DistanceY);
    }
}

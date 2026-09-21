using EvilHop.Primitives;
using EvilHop.Serialization;

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

    internal static new MoveCommand Read(EndianReader reader, FormatProfile _) => new()
    {
        DistanceX = reader.ReadSingle(),
        DistanceY = reader.ReadSingle(),
    };

    internal static void Write(MoveCommand value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.DistanceX);
        writer.Write(value.DistanceY);
    }
}

using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// One timed change applied to a <see cref="AssetType.UI"/> by a <see cref="UIMotionAsset"/>,
/// interpolating a property of the UI between a start and end state over
/// <see cref="StartTime"/>-<see cref="EndTime"/>.
/// </summary>
public abstract class UIMotionCommand
{
    /// <summary>The size, in bytes, of the header shared by every <see cref="UIMotionCommand"/>.</summary>
    internal const int HeaderSize = 24;

    /// <summary>The time, in seconds, this command starts at.</summary>
    public float StartTime { get; set; }

    /// <summary>The time, in seconds, this command ends at.</summary>
    public float EndTime { get; set; }

    /// <summary>The ease-in time, in seconds, the interpolation accelerates over.</summary>
    public float AccelTime { get; set; }

    /// <summary>The ease-out time, in seconds, the interpolation decelerates over.</summary>
    public float DecelTime { get; set; }

    /// <summary>
    /// Whether this command runs at all. A disabled command is skipped entirely, likely left over
    /// from testing.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Which concrete <see cref="UIMotionCommand"/> subclass this is.</summary>
    public abstract UIMotionCommandType Type { get; }

    /// <summary>This command's total serialized size, in bytes, header included.</summary>
    internal int SerializedSize => HeaderSize + FieldsSize;

    private protected abstract int FieldsSize { get; }

    /// <summary>
    /// Reads one command, including its shared header.
    /// </summary>
    /// <exception cref="InvalidDataException">The stored command type is not a known <see cref="UIMotionCommandType"/>.</exception>
    internal static UIMotionCommand Read(EndianReader reader, FormatProfile profile)
    {
        var type = (UIMotionCommandType)reader.ReadUInt32();
        float startTime = reader.ReadSingle();
        float endTime = reader.ReadSingle();
        float accelTime = reader.ReadSingle();
        float decelTime = reader.ReadSingle();
        bool enabled = reader.ReadByte() != 0;
        reader.ReadBytes(3); // padding, always zero

        UIMotionCommand command = type switch
        {
            UIMotionCommandType.Move => MoveCommand.Read(reader, profile),
            UIMotionCommandType.Scale => ScaleCommand.Read(reader, profile),
            UIMotionCommandType.Rotate => RotateCommand.Read(reader, profile),
            UIMotionCommandType.Opacity => OpacityCommand.Read(reader, profile),
            UIMotionCommandType.AbsoluteScale => AbsoluteScaleCommand.Read(reader, profile),
            UIMotionCommandType.Brightness => BrightnessCommand.Read(reader, profile),
            UIMotionCommandType.Color => ColorCommand.Read(reader, profile),
            UIMotionCommandType.UVScroll => UVScrollCommand.Read(reader, profile),
            _ => throw new InvalidDataException($"Unknown UI Motion command type 0x{(uint)type:X8}."),
        };

        command.StartTime = startTime;
        command.EndTime = endTime;
        command.AccelTime = accelTime;
        command.DecelTime = decelTime;
        command.Enabled = enabled;
        return command;
    }

    /// <summary>
    /// Writes one command, including its shared header.
    /// </summary>
    internal static void Write(UIMotionCommand command, EndianWriter writer, FormatProfile profile)
    {
        writer.Write((uint)command.Type);
        writer.Write(command.StartTime);
        writer.Write(command.EndTime);
        writer.Write(command.AccelTime);
        writer.Write(command.DecelTime);
        writer.Write((byte)(command.Enabled ? 1 : 0));
        writer.Write(new byte[3]); // padding

        switch (command)
        {
            case MoveCommand c: MoveCommand.Write(c, writer, profile); break;
            case ScaleCommand c: ScaleCommand.Write(c, writer, profile); break;
            case RotateCommand c: RotateCommand.Write(c, writer, profile); break;
            case OpacityCommand c: OpacityCommand.Write(c, writer, profile); break;
            case AbsoluteScaleCommand c: AbsoluteScaleCommand.Write(c, writer, profile); break;
            case BrightnessCommand c: BrightnessCommand.Write(c, writer, profile); break;
            case ColorCommand c: ColorCommand.Write(c, writer, profile); break;
            case UVScrollCommand c: UVScrollCommand.Write(c, writer, profile); break;
        }
    }
}

/// <summary>
/// Defines the type of animated property transformation applied to a UI element.
/// </summary>
public enum UIMotionCommandType : uint
{
    /// <summary>A <see cref="MoveCommand"/>.</summary>
    Move = 0,
    /// <summary>A <see cref="ScaleCommand"/>.</summary>
    Scale = 1,
    /// <summary>A <see cref="RotateCommand"/>.</summary>
    Rotate = 2,
    /// <summary>An <see cref="OpacityCommand"/>.</summary>
    Opacity = 3,
    /// <summary>An <see cref="AbsoluteScaleCommand"/>.</summary>
    AbsoluteScale = 4,
    /// <summary>A <see cref="BrightnessCommand"/>.</summary>
    Brightness = 5,
    /// <summary>A <see cref="ColorCommand"/>.</summary>
    Color = 6,
    /// <summary>A <see cref="UVScrollCommand"/>.</summary>
    UVScroll = 7,
}

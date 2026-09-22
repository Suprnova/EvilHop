using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// One timed change applied to a <see cref="AssetType.UI"/> by a <see cref="UIMotionAsset"/>,
/// interpolating a property of the UI between a start and end state over
/// <see cref="StartTime"/>-<see cref="EndTime"/>.
/// </summary>
public abstract partial class UIMotionCommand
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
    public abstract Command Type { get; }

    /// <summary>This command's total serialized size, in bytes, header included.</summary>
    internal int SerializedSize => HeaderSize + FieldsSize;

    private protected abstract int FieldsSize { get; }

    /// <summary>
    /// Reads one command, including its shared header.
    /// </summary>
    /// <exception cref="InvalidDataException">The stored command type is not a known <see cref="Command"/>.</exception>
    internal static UIMotionCommand Read(EndianReader reader, FormatProfile profile)
    {
        var type = (Command)reader.ReadUInt32();
        float startTime = reader.ReadSingle();
        float endTime = reader.ReadSingle();
        float accelTime = reader.ReadSingle();
        float decelTime = reader.ReadSingle();
        bool enabled = reader.ReadByte() != 0;
        reader.ReadBytes(3); // padding, always zero

        UIMotionCommand command = type switch
        {
            Command.Move => Move.Read(reader, profile),
            Command.Scale => Scale.Read(reader, profile),
            Command.Rotate => Rotate.Read(reader, profile),
            Command.Opacity => Opacity.Read(reader, profile),
            Command.AbsoluteScale => AbsoluteScale.Read(reader, profile),
            Command.Brightness => Brightness.Read(reader, profile),
            Command.Color => Color.Read(reader, profile),
            Command.UVScroll => UVScroll.Read(reader, profile),
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
            case Move c: Move.Write(c, writer, profile); break;
            case Scale c: Scale.Write(c, writer, profile); break;
            case Rotate c: Rotate.Write(c, writer, profile); break;
            case Opacity c: Opacity.Write(c, writer, profile); break;
            case AbsoluteScale c: AbsoluteScale.Write(c, writer, profile); break;
            case Brightness c: Brightness.Write(c, writer, profile); break;
            case Color c: Color.Write(c, writer, profile); break;
            case UVScroll c: UVScroll.Write(c, writer, profile); break;
        }
    }

    /// <summary>
    /// Defines the type of animated property transformation applied to a UI element.
    /// </summary>
    public enum Command : uint
    {
        /// <summary>A <see cref="UIMotionCommand.Move"/>.</summary>
        Move = 0,
        /// <summary>A <see cref="UIMotionCommand.Scale"/>.</summary>
        Scale = 1,
        /// <summary>A <see cref="UIMotionCommand.Rotate"/>.</summary>
        Rotate = 2,
        /// <summary>An <see cref="UIMotionCommand.Opacity"/>.</summary>
        Opacity = 3,
        /// <summary>An <see cref="UIMotionCommand.AbsoluteScale"/>.</summary>
        AbsoluteScale = 4,
        /// <summary>A <see cref="UIMotionCommand.Brightness"/>.</summary>
        Brightness = 5,
        /// <summary>A <see cref="UIMotionCommand.Color"/>.</summary>
        Color = 6,
        /// <summary>A <see cref="UIMotionCommand.UVScroll"/>.</summary>
        UVScroll = 7,
    }
}

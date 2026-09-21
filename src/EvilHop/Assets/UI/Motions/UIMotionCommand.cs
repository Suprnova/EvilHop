using EvilHop.Common;
using EvilHop.Primitives;

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

    internal abstract void ReadFields(EndianReader reader);

    internal abstract void WriteFields(EndianWriter writer);
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

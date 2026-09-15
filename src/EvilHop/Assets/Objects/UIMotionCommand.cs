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
/// Represents all known values for <see cref="UIMotionCommand.Type"/>.
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

    internal override void ReadFields(EndianReader reader)
    {
        AmountX = reader.ReadSingle();
        AmountY = reader.ReadSingle();
        CenterPivot = reader.ReadByte() != 0;
        reader.ReadBytes(3); // padding, always zero
        CenterOffsetX = reader.ReadSingle();
        CenterOffsetY = reader.ReadSingle();
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.Write(AmountX);
        writer.Write(AmountY);
        writer.Write((byte)(CenterPivot ? 1 : 0));
        writer.Write(new byte[3]); // padding
        writer.Write(CenterOffsetX);
        writer.Write(CenterOffsetY);
    }
}

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

/// <summary>
/// Changes the UI's opacity from <see cref="StartOpacity"/> to <see cref="EndOpacity"/>,
/// overwriting its previous opacity.
/// </summary>
public sealed class OpacityCommand : UIMotionCommand
{
    /// <summary>The starting opacity/alpha (0-255).</summary>
    public byte StartOpacity { get; set; }

    /// <summary>The ending opacity/alpha (0-255).</summary>
    public byte EndOpacity { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.Opacity;

    private protected override int FieldsSize => 4;

    internal override void ReadFields(EndianReader reader)
    {
        StartOpacity = reader.ReadByte();
        EndOpacity = reader.ReadByte();
        reader.ReadBytes(2); // padding, always zero
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.Write(StartOpacity);
        writer.Write(EndOpacity);
        writer.Write(new byte[2]); // padding
    }
}

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
    /// TODO: validate against decompiled source
    public byte TextScale { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.AbsoluteScale;

    private protected override int FieldsSize => 20;

    internal override void ReadFields(EndianReader reader)
    {
        StartX = reader.ReadSingle();
        StartY = reader.ReadSingle();
        EndX = reader.ReadSingle();
        EndY = reader.ReadSingle();
        CenterPivot = reader.ReadByte() != 0;
        TextScale = reader.ReadByte();
        reader.ReadBytes(2); // padding, always zero
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.Write(StartX);
        writer.Write(StartY);
        writer.Write(EndX);
        writer.Write(EndY);
        writer.Write((byte)(CenterPivot ? 1 : 0));
        writer.Write(TextScale);
        writer.Write(new byte[2]); // padding
    }
}

/// <summary>
/// Changes the UI's brightness from <see cref="StartBrightness"/> to <see cref="EndBrightness"/>,
/// overwriting its previous brightness.
/// </summary>
/// <remarks>
/// Unknown. Might be related to bloom on Xbox.
/// </remarks>
public sealed class BrightnessCommand : UIMotionCommand
{
    /// <summary>The starting brightness (0-255).</summary>
    public byte StartBrightness { get; set; }

    /// <summary>The ending brightness (0-255).</summary>
    public byte EndBrightness { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.Brightness;

    private protected override int FieldsSize => 4;

    internal override void ReadFields(EndianReader reader)
    {
        StartBrightness = reader.ReadByte();
        EndBrightness = reader.ReadByte();
        reader.ReadBytes(2); // padding, always zero
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.Write(StartBrightness);
        writer.Write(EndBrightness);
        writer.Write(new byte[2]); // padding
    }
}

/// <summary>
/// Changes the UI's color from <see cref="StartColor"/> to <see cref="EndColor"/>, overwriting its
/// previous color.
/// </summary>
public sealed class ColorCommand : UIMotionCommand
{
    /// <summary>The starting color.</summary>
    public Rgb24 StartColor { get; set; }

    /// <summary>The ending color.</summary>
    public Rgb24 EndColor { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.Color;

    private protected override int FieldsSize => 8;

    internal override void ReadFields(EndianReader reader)
    {
        StartColor = new Rgb24(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        EndColor = new Rgb24(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        reader.ReadBytes(2); // padding, always zero
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.Write(StartColor.R);
        writer.Write(StartColor.G);
        writer.Write(StartColor.B);
        writer.Write(EndColor.R);
        writer.Write(EndColor.G);
        writer.Write(EndColor.B);
        writer.Write(new byte[2]); // padding
    }
}

/// <summary>
/// An RGB color with no alpha channel.
/// </summary>
/// <param name="R">The red channel.</param>
/// <param name="G">The green channel.</param>
/// <param name="B">The blue channel.</param>
public readonly record struct Rgb24(byte R, byte G, byte B);

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

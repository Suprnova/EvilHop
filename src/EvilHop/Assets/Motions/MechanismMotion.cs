using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityMotion"/> that slides along and/or rotates around an axis, easing in and out
/// of each movement.
/// </summary>
public sealed class MechanismMotion : EntityMotion
{
    /// <summary>Which movements this mechanism makes, and in what order.</summary>
    public MechanismMovement Movement { get; set; }

    /// <summary>How this mechanism loops.</summary>
    public MechanismFlags MechanismFlags { get; set; }

    /// <summary>The axis to slide along.</summary>
    public MotionAxis SlideAxis { get; set; }

    /// <summary>The axis to rotate around.</summary>
    public MotionAxis RotateAxis { get; set; }

    /// <summary>
    /// Unknown. Not present in <see cref="GameVersion.N100F"/> or <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public byte ScaleAxis { get; set; }

    /// <summary>The distance to slide.</summary>
    public float SlideDistance { get; set; }

    /// <summary>The time, in seconds, the slide takes.</summary>
    public float SlideTime { get; set; }

    /// <summary>The time, in seconds, to ease into the slide.</summary>
    public float SlideAccelTime { get; set; }

    /// <summary>The time, in seconds, to ease out of the slide.</summary>
    public float SlideDecelTime { get; set; }

    /// <summary>The angle, in degrees, to rotate.</summary>
    public float RotateDistance { get; set; }

    /// <summary>The time, in seconds, the rotation takes.</summary>
    public float RotateTime { get; set; }

    /// <summary>The time, in seconds, to ease into the rotation.</summary>
    public float RotateAccelTime { get; set; }

    /// <summary>The time, in seconds, to ease out of the rotation.</summary>
    public float RotateDecelTime { get; set; }

    /// <summary>The time, in seconds, to wait after moving forward.</summary>
    public float ReturnDelay { get; set; }

    /// <summary>The time, in seconds, to wait after moving back.</summary>
    public float PostReturnDelay { get; set; }

    /// <summary>
    /// Unknown. Not present in <see cref="GameVersion.N100F"/> or <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public float ScaleAmount { get; set; }

    /// <summary>
    /// Unknown. Not present in <see cref="GameVersion.N100F"/> or <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public float ScaleDuration { get; set; }

    private protected override MotionType Type => MotionType.Mechanism;

    private protected override void ReadFields(EndianReader reader, GameVersion game)
    {
        Movement = (MechanismMovement)reader.ReadByte();
        MechanismFlags = (MechanismFlags)reader.ReadByte();
        SlideAxis = (MotionAxis)reader.ReadByte();
        RotateAxis = (MotionAxis)reader.ReadByte();
        if (!IsBFBBOrEarlier(game))
        {
            ScaleAxis = reader.ReadByte();
            reader.ReadBytes(3); // padding, always zero
        }

        SlideDistance = reader.ReadSingle();
        SlideTime = reader.ReadSingle();
        SlideAccelTime = reader.ReadSingle();
        SlideDecelTime = reader.ReadSingle();
        RotateDistance = reader.ReadSingle();
        RotateTime = reader.ReadSingle();
        RotateAccelTime = reader.ReadSingle();
        RotateDecelTime = reader.ReadSingle();
        ReturnDelay = reader.ReadSingle();
        PostReturnDelay = reader.ReadSingle();
        if (IsBFBBOrEarlier(game)) return;

        ScaleAmount = reader.ReadSingle();
        ScaleDuration = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion game)
    {
        writer.Write((byte)Movement);
        writer.Write((byte)MechanismFlags);
        writer.Write((byte)SlideAxis);
        writer.Write((byte)RotateAxis);
        if (!IsBFBBOrEarlier(game))
        {
            writer.Write(ScaleAxis);
            writer.Write(new byte[3]); // padding
        }

        writer.Write(SlideDistance);
        writer.Write(SlideTime);
        writer.Write(SlideAccelTime);
        writer.Write(SlideDecelTime);
        writer.Write(RotateDistance);
        writer.Write(RotateTime);
        writer.Write(RotateAccelTime);
        writer.Write(RotateDecelTime);
        writer.Write(ReturnDelay);
        writer.Write(PostReturnDelay);
        if (IsBFBBOrEarlier(game)) return;

        writer.Write(ScaleAmount);
        writer.Write(ScaleDuration);
    }
}

/// <summary>
/// Represents all known values for <see cref="MechanismMotion.Movement"/>.
/// </summary>
/// <remarks>
/// <see cref="GameVersion.BFBB"/> treats any other value as <see cref="Rotate"/>. Later games store
/// further values whose meaning is unknown.
/// </remarks>
public enum MechanismMovement : byte
{
    /// <summary>Slides only.</summary>
    Slide = 0,
    /// <summary>Rotates only.</summary>
    Rotate = 1,
    /// <summary>Slides and rotates at the same time.</summary>
    SlideAndRotate = 2,
    /// <summary>Slides, then rotates.</summary>
    SlideThenRotate = 3,
    /// <summary>Rotates, then slides.</summary>
    RotateThenSlide = 4,
}

/// <summary>
/// Represents all known values for <see cref="MechanismMotion.MechanismFlags"/>.
/// </summary>
[Flags]
public enum MechanismFlags : byte
{
    /// <summary>
    /// No flags are set: the mechanism only moves forward, repeating continuously.
    /// </summary>
    Repeat = 0,
    /// <summary>
    /// Each cycle moves forward, then back to the start.
    /// </summary>
    ReturnToStart = 1 << 0,
    /// <summary>
    /// The mechanism runs one cycle, then stops.
    /// </summary>
    PlayOnce = 1 << 1,
}

/// <summary>
/// Represents all known values for <see cref="MechanismMotion.SlideAxis"/> and
/// <see cref="MechanismMotion.RotateAxis"/>.
/// </summary>
public enum MotionAxis : byte
{
    /// <summary>The X axis.</summary>
    X = 0,
    /// <summary>The Y axis.</summary>
    Y = 1,
    /// <summary>The Z axis.</summary>
    Z = 2,
}

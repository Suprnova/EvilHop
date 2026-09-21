using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityMotion"/> that slides along and/or rotates around an axis, easing in and out
/// of each movement.
/// </summary>
public sealed class MechanismMotion() : EntityMotion
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

    internal static new MechanismMotion Read(EndianReader reader, FormatProfile profile)
    {
        var motion = new MechanismMotion
        {
            Movement = (MechanismMovement)reader.ReadByte(),
            MechanismFlags = (MechanismFlags)reader.ReadByte(),
            SlideAxis = (MotionAxis)reader.ReadByte(),
            RotateAxis = (MotionAxis)reader.ReadByte(),
        };
        if (!IsBFBBOrEarlier(profile.Game))
        {
            motion.ScaleAxis = reader.ReadByte();
            reader.ReadBytes(3); // padding
        }

        motion.SlideDistance = reader.ReadSingle();
        motion.SlideTime = reader.ReadSingle();
        motion.SlideAccelTime = reader.ReadSingle();
        motion.SlideDecelTime = reader.ReadSingle();
        motion.RotateDistance = reader.ReadSingle();
        motion.RotateTime = reader.ReadSingle();
        motion.RotateAccelTime = reader.ReadSingle();
        motion.RotateDecelTime = reader.ReadSingle();
        motion.ReturnDelay = reader.ReadSingle();
        motion.PostReturnDelay = reader.ReadSingle();
        if (IsBFBBOrEarlier(profile.Game)) return motion;

        motion.ScaleAmount = reader.ReadSingle();
        motion.ScaleDuration = reader.ReadSingle();
        return motion;
    }

    internal static void Write(MechanismMotion value, EndianWriter writer, FormatProfile profile)
    {
        writer.Write((byte)value.Movement);
        writer.Write((byte)value.MechanismFlags);
        writer.Write((byte)value.SlideAxis);
        writer.Write((byte)value.RotateAxis);
        if (!IsBFBBOrEarlier(profile.Game))
        {
            writer.Write(value.ScaleAxis);
            writer.Write(new byte[3]); // padding
        }

        writer.Write(value.SlideDistance);
        writer.Write(value.SlideTime);
        writer.Write(value.SlideAccelTime);
        writer.Write(value.SlideDecelTime);
        writer.Write(value.RotateDistance);
        writer.Write(value.RotateTime);
        writer.Write(value.RotateAccelTime);
        writer.Write(value.RotateDecelTime);
        writer.Write(value.ReturnDelay);
        writer.Write(value.PostReturnDelay);
        if (IsBFBBOrEarlier(profile.Game)) return;

        writer.Write(value.ScaleAmount);
        writer.Write(value.ScaleDuration);
    }
}

/// <summary>
/// Defines the motion movement sequence, axis operation, and mechanical execution order for a <see cref="MechanismMotion"/>.
/// </summary>
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
/// Flags controlling looping, repetition, and return-to-start behavior for a <see cref="MechanismMotion"/>.
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
/// Defines the motion movement axis and mechanical travel path for a <see cref="MechanismMotion"/>.
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

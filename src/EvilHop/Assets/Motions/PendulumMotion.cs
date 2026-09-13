using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityMotion"/> that swings from side to side.
/// </summary>
public sealed class PendulumMotion : EntityMotion
{
    /// <summary>Unknown.</summary>
    public byte PendulumFlags { get; set; }

    /// <summary>Unknown.</summary>
    public byte Plane { get; set; }

    /// <summary>The height of the pivot point.</summary>
    public float Length { get; set; }

    /// <summary>The angle, in radians, swung to either side.</summary>
    public float Range { get; set; }

    /// <summary>The time, in seconds, one full swing takes.</summary>
    public float Period { get; set; }

    /// <summary>The angle, in radians, into the swing to start at.</summary>
    public float Phase { get; set; }

    private protected override MotionType Type => MotionType.Pendulum;

    private protected override void ReadFields(EndianReader reader, GameVersion _)
    {
        PendulumFlags = reader.ReadByte();
        Plane = reader.ReadByte();
        reader.ReadBytes(2); // padding, always 0
        Length = reader.ReadSingle();
        Range = reader.ReadSingle();
        Period = reader.ReadSingle();
        Phase = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion _)
    {
        writer.Write(PendulumFlags);
        writer.Write(Plane);
        writer.Write(new byte[2]); // padding
        writer.Write(Length);
        writer.Write(Range);
        writer.Write(Period);
        writer.Write(Phase);
    }
}

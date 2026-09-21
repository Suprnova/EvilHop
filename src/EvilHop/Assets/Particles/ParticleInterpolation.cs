using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A value that changes from <see cref="Start"/> to <see cref="End"/> over a particle's lifetime,
/// according to <see cref="Mode"/>.
/// </summary>
public sealed class ParticleInterpolation
{
    /// <summary>The value at the start of the interpolation.</summary>
    public float Start { get; set; }

    /// <summary>The value at the end of the interpolation.</summary>
    public float End { get; set; }

    /// <summary>How the current value is computed from <see cref="Start"/> and <see cref="End"/>.</summary>
    public ParticleInterpolationMode Mode { get; set; }

    /// <summary>
    /// The frequency, in Hertz, <see cref="ParticleInterpolationMode.Random"/>,
    /// <see cref="ParticleInterpolationMode.Linear"/>, and <see cref="ParticleInterpolationMode.Step"/>
    /// advance at.
    /// </summary>
    public float Frequency { get; set; }

    /// <summary>
    /// The inverse frequency <see cref="ParticleInterpolationMode.Sine"/> and
    /// <see cref="ParticleInterpolationMode.Cosine"/> advance at.
    /// </summary>
    public float InverseFrequency { get; set; }

    internal static ParticleInterpolation Read(EndianReader reader, FormatProfile _) => new()
    {
        Start = reader.ReadSingle(),
        End = reader.ReadSingle(),
        Mode = (ParticleInterpolationMode)reader.ReadUInt32(),
        Frequency = reader.ReadSingle(),
        InverseFrequency = reader.ReadSingle(),
    };

    internal static void Write(ParticleInterpolation interpolation, EndianWriter writer, FormatProfile _)
    {
        writer.Write(interpolation.Start);
        writer.Write(interpolation.End);
        writer.Write((uint)interpolation.Mode);
        writer.Write(interpolation.Frequency);
        writer.Write(interpolation.InverseFrequency);
    }
}

/// <summary>
/// A color, each channel interpolated independently over a particle's lifetime.
/// </summary>
public sealed class ParticleColorInterpolation
{
    /// <summary>The red channel.</summary>
    public ParticleInterpolation Red { get; set; } = new();

    /// <summary>The green channel.</summary>
    public ParticleInterpolation Green { get; set; } = new();

    /// <summary>The blue channel.</summary>
    public ParticleInterpolation Blue { get; set; } = new();

    /// <summary>The alpha channel.</summary>
    public ParticleInterpolation Alpha { get; set; } = new();

    internal static ParticleColorInterpolation Read(EndianReader reader, FormatProfile profile) => new()
    {
        Red = ParticleInterpolation.Read(reader, profile),
        Green = ParticleInterpolation.Read(reader, profile),
        Blue = ParticleInterpolation.Read(reader, profile),
        Alpha = ParticleInterpolation.Read(reader, profile),
    };

    internal static void Write(ParticleColorInterpolation color, EndianWriter writer, FormatProfile profile)
    {
        ParticleInterpolation.Write(color.Red, writer, profile);
        ParticleInterpolation.Write(color.Green, writer, profile);
        ParticleInterpolation.Write(color.Blue, writer, profile);
        ParticleInterpolation.Write(color.Alpha, writer, profile);
    }
}

/// <summary>
/// Defines the interpolation curve used to transition particle properties over their lifetime.
/// </summary>
/// <remarks>
/// Real archives almost always store this as a BKDR hash of the mode's name; a raw value below 8
/// selecting the same order is also accepted by the engine and appears in at least one archive
/// checked, with <see cref="Time"/> reachable only that way - hashing the name <c>"Time"</c> does not
/// map to it.
/// </remarks>
public enum ParticleInterpolationMode : uint
{
    /// <summary>Always <see cref="ParticleInterpolation.Start"/>.</summary>
    ConstA = 0x48E48E7A,

    /// <summary>Always <see cref="ParticleInterpolation.End"/>.</summary>
    ConstB = 0x48E48E7B,

    /// <summary>
    /// A new random value between <see cref="ParticleInterpolation.Start"/> and
    /// <see cref="ParticleInterpolation.End"/>, refreshed every <see cref="ParticleInterpolation.Frequency"/> seconds.
    /// </summary>
    Random = 0x0FE111BF,

    /// <summary>
    /// Linearly interpolates between <see cref="ParticleInterpolation.Start"/> and
    /// <see cref="ParticleInterpolation.End"/> at <see cref="ParticleInterpolation.Frequency"/>.
    /// </summary>
    Linear = 0xB7353B79,

    /// <summary>
    /// Interpolates between <see cref="ParticleInterpolation.Start"/> and
    /// <see cref="ParticleInterpolation.End"/> using a sine curve at <see cref="ParticleInterpolation.InverseFrequency"/>.
    /// </summary>
    Sine = 0x0B326F01,

    /// <summary>
    /// Interpolates between <see cref="ParticleInterpolation.Start"/> and
    /// <see cref="ParticleInterpolation.End"/> using a cosine curve at <see cref="ParticleInterpolation.InverseFrequency"/>.
    /// </summary>
    Cosine = 0x498D7119,

    /// <summary>
    /// The current time of the interpolation. Unused - not reachable by hashing a mode name, and
    /// never observed in a real archive.
    /// </summary>
    /// TODO: validate against decompiled source
    Time = 6,

    /// <summary>
    /// <see cref="ParticleInterpolation.Start"/> until <c>time * </c><see cref="ParticleInterpolation.Frequency"/>
    /// reaches 0.5, then <see cref="ParticleInterpolation.End"/>.
    /// </summary>
    Step = 0x0B354BD4,
}

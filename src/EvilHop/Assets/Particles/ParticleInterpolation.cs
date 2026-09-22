using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A value that changes from <see cref="Start"/> to <see cref="End"/> over a particle's lifetime,
/// according to <see cref="Mode"/>.
/// </summary>
public sealed partial class ParticleInterpolation
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

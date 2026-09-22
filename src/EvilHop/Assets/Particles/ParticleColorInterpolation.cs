using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

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

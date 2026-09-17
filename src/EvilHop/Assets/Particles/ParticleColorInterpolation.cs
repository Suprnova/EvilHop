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
}

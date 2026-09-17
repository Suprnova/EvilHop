using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterAsset"/>'s spawn rate, particle lifetime, size, and color, in the same
/// shape a <see cref="AssetType.ParticleEmitterProperty"/> asset would otherwise hold. Only read or
/// written in <see cref="GameVersion.N100F"/>.
/// </summary>
public sealed class ParticleEmitterInlineProperties
{
    /// <summary>The number of particles spawned per emission.</summary>
    public byte Count { get; set; }

    /// <summary>How much <see cref="Count"/> randomly varies per emission.</summary>
    public byte CountVariation { get; set; }

    /// <summary>The time, in seconds, between emissions.</summary>
    public float Interval { get; set; }

    /// <summary>The particle system whose visuals newly spawned particles use.</summary>
    public AssetId ParSysId { get; set; }

    /// <summary>A particle's color when it spawns.</summary>
    public Rgba ColorBirth { get; set; }

    /// <summary>A particle's color just before it expires.</summary>
    public Rgba ColorDeath { get; set; }

    /// <summary>A particle's size when it spawns.</summary>
    public float SizeBirth { get; set; }

    /// <summary>How much <see cref="SizeBirth"/> randomly varies per particle.</summary>
    public float SizeBirthVariation { get; set; }

    /// <summary>A particle's size just before it expires.</summary>
    public float SizeDeath { get; set; }

    /// <summary>How long, in seconds, a particle lives before expiring.</summary>
    public float Life { get; set; }

    /// <summary>How much <see cref="Life"/> randomly varies per particle.</summary>
    public float LifeVariation { get; set; }

    /// <summary>The maximum number of particles this emitter may have alive at once.</summary>
    public byte MaxEmit { get; set; }
}

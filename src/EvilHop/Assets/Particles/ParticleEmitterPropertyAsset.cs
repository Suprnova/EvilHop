using EvilHop.Common;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// Describes how a particle changes over its lifetime - its emission rate, color, size, and
/// velocity - shared by every <see cref="AssetType.ParticleEmitter"/> that references it.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PARP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class ParticleEmitterPropertyAsset() : BaseAsset(AssetType.ParticleEmitterProperty)
{
    /// <summary>The <see cref="AssetType.ParticleSystem"/> this emitter's particles use.</summary>
    public AssetId ParSysId { get; set; }

    /// <summary>How many particles are emitted per second.</summary>
    public ParticleInterpolation Rate { get; set; } = new();

    /// <summary>How long, in seconds, a particle lives before expiring.</summary>
    public ParticleInterpolation Life { get; set; } = new();

    /// <summary>A particle's size, in units, when it spawns.</summary>
    public ParticleInterpolation SizeBirth { get; set; } = new();

    /// <summary>A particle's size, in units, just before it expires.</summary>
    public ParticleInterpolation SizeDeath { get; set; } = new();

    /// <summary>A particle's color when it spawns.</summary>
    public ParticleColorInterpolation ColorBirth { get; set; } = new();

    /// <summary>A particle's color just before it expires.</summary>
    public ParticleColorInterpolation ColorDeath { get; set; } = new();

    /// <summary>Not used by the particle system.</summary>
    public ParticleInterpolation VelocityScale { get; set; } = new();

    /// <summary>Not used by the particle system.</summary>
    public ParticleInterpolation VelocityAngle { get; set; } = new();

    /// <summary>Not used by the particle system. Always zero.</summary>
    public Vector3 Velocity { get; set; }

    /// <summary>
    /// The maximum number of particles this emitter may have alive at once, or -1 for unlimited.
    /// </summary>
    public int EmitLimit { get; set; } = -1;

    /// <summary>
    /// The time, in seconds, before the count toward <see cref="EmitLimit"/> resets. Usually 0.
    /// </summary>
    public float EmitLimitResetTime { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.ParticleEmitterProperty"/> is known to be
    /// read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };
}

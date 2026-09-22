using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A source of particles, whose shape, spawn rate, and other parameters are either defined by a
/// separate <see cref="AssetType.ParticleEmitterProperty"/> asset, or (in <see cref="GameVersion.N100F"/>
/// only) embedded directly in <see cref="InlineProperties"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PARE">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class ParticleEmitterAsset() : BaseAsset(AssetType.ParticleEmitter, baseType: 0x26)
{
    /// <summary>This emitter's flags.</summary>
    public Behavior Flags { get; set; }

    /// <summary>The shape particles are emitted from or along.</summary>
    public ShapeKind Kind { get; set; }

    /// <summary>
    /// <see cref="Kind"/>-specific data describing the emission shape.
    /// </summary>
    public ParticleEmitterShape Shape { get; set; } = new ParticleEmitterShape.Point();

    /// <summary>
    /// The <see cref="AssetType.ParticleEmitterProperty"/> asset defining this emitter's spawn rate,
    /// particle lifetime, size, and color. Not present in <see cref="GameVersion.N100F"/>, which
    /// stores the same information directly in <see cref="InlineProperties"/>.
    /// </summary>
    public AssetId PropId { get; set; }

    /// <summary>
    /// The object this emitter is attached to and emits from - typically an <see cref="AssetType.Marker"/>
    /// when <see cref="Kind"/> is <see cref="ShapeKind.Point"/>, or another object asset when
    /// <see cref="Kind"/> is <see cref="ShapeKind.EntityBone"/> or
    /// <see cref="ShapeKind.EntityBound"/>.
    /// </summary>
    public AssetId AttachToId { get; set; }

    /// <summary>This emitter's position in the game world.</summary>
    public Vector3 Position { get; set; }

    /// <summary>The velocity newly emitted particles inherit.</summary>
    public Vector3 Velocity { get; set; }

    /// <summary>How much <see cref="Velocity"/>'s direction randomly varies per particle.</summary>
    public float VelocityAngleVariation { get; set; }

    /// <summary>How this emitter is culled when out of view or out of range.</summary>
    public uint CullMode { get; set; }

    /// <summary>The squared distance beyond which this emitter is culled.</summary>
    public float CullDistanceSquared { get; set; }

    /// <summary>
    /// This emitter's spawn rate, particle lifetime, size, and color, stored inline instead of in a
    /// separate <see cref="AssetType.ParticleEmitterProperty"/> asset. Only meaningful in
    /// <see cref="GameVersion.N100F"/>; every other game leaves this at its default.
    /// </summary>
    public Properties InlineProperties { get; set; } = new();

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.ParticleEmitter"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    /// <summary>
    /// A <see cref="ParticleEmitterAsset"/>'s spawn rate, particle lifetime, size, and color, in the same
    /// shape a <see cref="AssetType.ParticleEmitterProperty"/> asset would otherwise hold. Only read or
    /// written in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public sealed class Properties
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

    /// <summary>
    /// Flags controlling particle emission timing, orientation, and simulation behavior.
    /// </summary>
    [Flags]
    public enum Behavior : byte
    {
        /// <summary>No flags are set.</summary>
        None = 0,

        /// <summary>This emitter is active and emitting particles.</summary>
        On = 1 << 0,
    }
}

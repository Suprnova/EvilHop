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
    public ParticleEmitterFlags Flags { get; set; }

    /// <summary>The shape particles are emitted from or along.</summary>
    public ParticleEmitterKind Kind { get; set; }

    /// <summary>
    /// <see cref="Kind"/>-specific data describing the emission shape.
    /// </summary>
    public ParticleEmitterShape Shape { get; set; } = new PointEmitterShape();

    /// <summary>
    /// The <see cref="AssetType.ParticleEmitterProperty"/> asset defining this emitter's spawn rate,
    /// particle lifetime, size, and color. Not present in <see cref="GameVersion.N100F"/>, which
    /// stores the same information directly in <see cref="InlineProperties"/>.
    /// </summary>
    public AssetId PropId { get; set; }

    /// <summary>
    /// The object this emitter is attached to and emits from - typically an <see cref="AssetType.Marker"/>
    /// when <see cref="Kind"/> is <see cref="ParticleEmitterKind.Point"/>, or another object asset when
    /// <see cref="Kind"/> is <see cref="ParticleEmitterKind.EntityBone"/> or
    /// <see cref="ParticleEmitterKind.EntityBound"/>.
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
    public ParticleEmitterInlineProperties InlineProperties { get; set; } = new();

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
}

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

/// <summary>
/// Flags controlling particle emission timing, orientation, and simulation behavior.
/// </summary>
[Flags]
public enum ParticleEmitterFlags : byte
{
    /// <summary>No flags are set.</summary>
    None = 0,

    /// <summary>This emitter is active and emitting particles.</summary>
    On = 1 << 0,
}

/// <summary>
/// Defines the shape and volume geometry used by a <see cref="ParticleEmitterAsset"/> to spawn particles.
/// </summary>
public enum ParticleEmitterKind : byte
{
    /// <summary>Emits from a single point, with no <see cref="ParticleEmitterAsset.Shape"/> data of its own.</summary>
    Point = 0,

    /// <summary>Emits from the edge of a <see cref="CircleEmitterShape"/>.</summary>
    CircleEdge = 1,

    /// <summary>Emits from anywhere within a <see cref="CircleEmitterShape"/>.</summary>
    Circle = 2,

    /// <summary>Emits from the edge of a <see cref="RectEmitterShape"/>.</summary>
    RectEdge = 3,

    /// <summary>Emits from anywhere within a <see cref="RectEmitterShape"/>.</summary>
    Rect = 4,

    /// <summary>Emits from anywhere along a <see cref="LineEmitterShape"/>.</summary>
    Line = 5,

    /// <summary>Emits from anywhere within a <see cref="VolumeEmitterShape"/>.</summary>
    Volume = 6,

    /// <summary>Emits from the edge of a <see cref="SphereEmitterShape"/>.</summary>
    SphereEdge1 = 7,

    /// <summary>Emits from anywhere within a <see cref="SphereEmitterShape"/>.</summary>
    Sphere = 8,

    /// <summary>Emits from a point offset from this emitter, per <see cref="OffsetPointEmitterShape"/>.</summary>
    OffsetPoint = 9,

    /// <summary>Emits from the edge of a <see cref="SphereEmitterShape"/>.</summary>
    SphereEdge2 = 10,

    /// <summary>Emits from the edge of a <see cref="SphereEmitterShape"/>.</summary>
    SphereEdge3 = 11,

    /// <summary>Emits from the edge of a vertical cylinder, per <see cref="VCylEmitterShape"/>.</summary>
    VCylEdge = 12,

    /// <summary>Emits from the edge of an oriented <see cref="CircleEmitterShape"/>.</summary>
    OCircleEdge = 13,

    /// <summary>Emits from anywhere within an oriented <see cref="CircleEmitterShape"/>.</summary>
    OCircle = 14,

    /// <summary>Emits from a bone on an attached entity, per <see cref="EntityBoneEmitterShape"/>.</summary>
    EntityBone = 15,

    /// <summary>Emits from within an attached entity's bounding volume, per <see cref="EntityBoundEmitterShape"/>.</summary>
    EntityBound = 16,
}

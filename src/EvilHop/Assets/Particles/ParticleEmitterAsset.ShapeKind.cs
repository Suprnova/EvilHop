namespace EvilHop.Assets;

public partial class ParticleEmitterAsset
{
    /// <summary>
    /// Defines the shape and volume geometry used by a <see cref="ParticleEmitterAsset"/> to spawn particles.
    /// </summary>
    public enum ShapeKind : byte
    {
        /// <summary>Emits from a single point, with no <see cref="Shape"/> data of its own.</summary>
        Point = 0,

        /// <summary>Emits from the edge of a <see cref="ParticleEmitterShape.Circle"/>.</summary>
        CircleEdge = 1,

        /// <summary>Emits from anywhere within a <see cref="ParticleEmitterShape.Circle"/>.</summary>
        Circle = 2,

        /// <summary>Emits from the edge of a <see cref="ParticleEmitterShape.Rectangle"/>.</summary>
        RectEdge = 3,

        /// <summary>Emits from anywhere within a <see cref="ParticleEmitterShape.Rectangle"/>.</summary>
        Rect = 4,

        /// <summary>Emits from anywhere along a <see cref="ParticleEmitterShape.Line"/>.</summary>
        Line = 5,

        /// <summary>Emits from anywhere within a <see cref="ParticleEmitterShape.Volume"/>.</summary>
        Volume = 6,

        /// <summary>Emits from the edge of a <see cref="ParticleEmitterShape.Sphere"/>.</summary>
        SphereEdge1 = 7,

        /// <summary>Emits from anywhere within a <see cref="ParticleEmitterShape.Sphere"/>.</summary>
        Sphere = 8,

        /// <summary>Emits from a point offset from this emitter, per <see cref="ParticleEmitterShape.OffsetPoint"/>.</summary>
        OffsetPoint = 9,

        /// <summary>Emits from the edge of a <see cref="ParticleEmitterShape.Sphere"/>.</summary>
        SphereEdge2 = 10,

        /// <summary>Emits from the edge of a <see cref="ParticleEmitterShape.Sphere"/>.</summary>
        SphereEdge3 = 11,

        /// <summary>Emits from the edge of a vertical cylinder, per <see cref="ParticleEmitterShape.Cylinder"/>.</summary>
        VCylEdge = 12,

        /// <summary>Emits from the edge of an oriented <see cref="ParticleEmitterShape.Circle"/>.</summary>
        OCircleEdge = 13,

        /// <summary>Emits from anywhere within an oriented <see cref="ParticleEmitterShape.Circle"/>.</summary>
        OCircle = 14,

        /// <summary>Emits from a bone on an attached entity, per <see cref="ParticleEmitterShape.EntityBone"/>.</summary>
        EntityBone = 15,

        /// <summary>Emits from within an attached entity's bounding volume, per <see cref="ParticleEmitterShape.EntityBound"/>.</summary>
        EntityBound = 16,
    }
}

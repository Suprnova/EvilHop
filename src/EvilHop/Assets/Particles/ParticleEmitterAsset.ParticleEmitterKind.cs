namespace EvilHop.Assets;

public partial class ParticleEmitterAsset
{
    /// <summary>
    /// Defines the shape and volume geometry used by a <see cref="ParticleEmitterAsset"/> to spawn particles.
    /// </summary>
    public enum ParticleEmitterKind : byte
    {
        /// <summary>Emits from a single point, with no <see cref="Shape"/> data of its own.</summary>
        Point = 0,

        /// <summary>Emits from the edge of a <see cref="ParticleEmitterShape.CircleEmitterShape"/>.</summary>
        CircleEdge = 1,

        /// <summary>Emits from anywhere within a <see cref="ParticleEmitterShape.CircleEmitterShape"/>.</summary>
        Circle = 2,

        /// <summary>Emits from the edge of a <see cref="ParticleEmitterShape.RectEmitterShape"/>.</summary>
        RectEdge = 3,

        /// <summary>Emits from anywhere within a <see cref="ParticleEmitterShape.RectEmitterShape"/>.</summary>
        Rect = 4,

        /// <summary>Emits from anywhere along a <see cref="ParticleEmitterShape.LineEmitterShape"/>.</summary>
        Line = 5,

        /// <summary>Emits from anywhere within a <see cref="ParticleEmitterShape.VolumeEmitterShape"/>.</summary>
        Volume = 6,

        /// <summary>Emits from the edge of a <see cref="ParticleEmitterShape.SphereEmitterShape"/>.</summary>
        SphereEdge1 = 7,

        /// <summary>Emits from anywhere within a <see cref="ParticleEmitterShape.SphereEmitterShape"/>.</summary>
        Sphere = 8,

        /// <summary>Emits from a point offset from this emitter, per <see cref="ParticleEmitterShape.OffsetPointEmitterShape"/>.</summary>
        OffsetPoint = 9,

        /// <summary>Emits from the edge of a <see cref="ParticleEmitterShape.SphereEmitterShape"/>.</summary>
        SphereEdge2 = 10,

        /// <summary>Emits from the edge of a <see cref="ParticleEmitterShape.SphereEmitterShape"/>.</summary>
        SphereEdge3 = 11,

        /// <summary>Emits from the edge of a vertical cylinder, per <see cref="ParticleEmitterShape.VCylEmitterShape"/>.</summary>
        VCylEdge = 12,

        /// <summary>Emits from the edge of an oriented <see cref="ParticleEmitterShape.CircleEmitterShape"/>.</summary>
        OCircleEdge = 13,

        /// <summary>Emits from anywhere within an oriented <see cref="ParticleEmitterShape.CircleEmitterShape"/>.</summary>
        OCircle = 14,

        /// <summary>Emits from a bone on an attached entity, per <see cref="ParticleEmitterShape.EntityBoneEmitterShape"/>.</summary>
        EntityBone = 15,

        /// <summary>Emits from within an attached entity's bounding volume, per <see cref="ParticleEmitterShape.EntityBoundEmitterShape"/>.</summary>
        EntityBound = 16,
    }
}

using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ShapeKind.SphereEdge1"/>,
    /// <see cref="ParticleEmitterAsset.ShapeKind.Sphere"/>, <see cref="ParticleEmitterAsset.ShapeKind.SphereEdge2"/>, and
    /// <see cref="ParticleEmitterAsset.ShapeKind.SphereEdge3"/>.
    /// </summary>
    public sealed class Sphere : ParticleEmitterShape
    {
        /// <summary>The sphere's radius.</summary>
        public float Radius { get; set; }

        internal static Sphere Read(EndianReader reader, FormatProfile _) => new()
        {
            Radius = reader.ReadSingle(),
        };

        internal static void Write(Sphere value, EndianWriter writer, FormatProfile _) =>
            writer.Write(value.Radius);
    }
}

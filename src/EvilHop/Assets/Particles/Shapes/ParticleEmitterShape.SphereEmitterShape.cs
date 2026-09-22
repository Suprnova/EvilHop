using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ParticleEmitterKind.SphereEdge1"/>,
    /// <see cref="ParticleEmitterAsset.ParticleEmitterKind.Sphere"/>, <see cref="ParticleEmitterAsset.ParticleEmitterKind.SphereEdge2"/>, and
    /// <see cref="ParticleEmitterAsset.ParticleEmitterKind.SphereEdge3"/>.
    /// </summary>
    public sealed class SphereEmitterShape : ParticleEmitterShape
    {
        /// <summary>The sphere's radius.</summary>
        public float Radius { get; set; }

        internal static SphereEmitterShape Read(EndianReader reader, FormatProfile _) => new()
        {
            Radius = reader.ReadSingle(),
        };

        internal static void Write(SphereEmitterShape value, EndianWriter writer, FormatProfile _) =>
            writer.Write(value.Radius);
    }
}

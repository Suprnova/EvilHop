using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ParticleEmitterKind.CircleEdge"/>,
    /// <see cref="ParticleEmitterAsset.ParticleEmitterKind.Circle"/>, <see cref="ParticleEmitterAsset.ParticleEmitterKind.OCircleEdge"/>, and
    /// <see cref="ParticleEmitterAsset.ParticleEmitterKind.OCircle"/>.
    /// </summary>
    public sealed class CircleEmitterShape : ParticleEmitterShape
    {
        /// <summary>The circle's radius.</summary>
        public float Radius { get; set; }

        /// <summary>How much a particle's emitted direction deflects away from the circle's normal.</summary>
        public float Deflection { get; set; }

        /// <summary>
        /// The circle's normal direction. Not present in <see cref="GameVersion.N100F"/>.
        /// </summary>
        public Vector3 Direction { get; set; }

        internal static CircleEmitterShape Read(EndianReader reader, FormatProfile profile)
        {
            var shape = new CircleEmitterShape
            {
                Radius = reader.ReadSingle(),
                Deflection = reader.ReadSingle(),
            };
            if (profile.Game is not GameVersion.N100F) shape.Direction = reader.ReadVector3();
            return shape;
        }

        internal static void Write(CircleEmitterShape value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(value.Radius);
            writer.Write(value.Deflection);
            if (profile.Game is not GameVersion.N100F) writer.Write(value.Direction);
        }
    }
}

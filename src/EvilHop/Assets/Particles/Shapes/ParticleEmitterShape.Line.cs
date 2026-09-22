using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ShapeKind.Line"/>.
    /// </summary>
    public sealed class Line : ParticleEmitterShape
    {
        /// <summary>One end of the line.</summary>
        public Vector3 Position1 { get; set; }

        /// <summary>The other end of the line.</summary>
        public Vector3 Position2 { get; set; }

        /// <summary>The line's thickness.</summary>
        public float Radius { get; set; }

        internal static Line Read(EndianReader reader, FormatProfile _) => new()
        {
            Position1 = reader.ReadVector3(),
            Position2 = reader.ReadVector3(),
            Radius = reader.ReadSingle(),
        };

        internal static void Write(Line value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Position1);
            writer.Write(value.Position2);
            writer.Write(value.Radius);
        }
    }
}

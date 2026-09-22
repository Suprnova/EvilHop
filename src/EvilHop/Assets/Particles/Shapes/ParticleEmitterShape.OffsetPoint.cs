using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ShapeKind.OffsetPoint"/>.
    /// </summary>
    public sealed class OffsetPoint : ParticleEmitterShape
    {
        /// <summary>The offset from <see cref="ParticleEmitterAsset.Position"/> particles are emitted from.</summary>
        public Vector3 Offset { get; set; }

        internal static OffsetPoint Read(EndianReader reader, FormatProfile _) => new()
        {
            Offset = reader.ReadVector3(),
        };

        internal static void Write(OffsetPoint value, EndianWriter writer, FormatProfile _) =>
            writer.Write(value.Offset);
    }
}

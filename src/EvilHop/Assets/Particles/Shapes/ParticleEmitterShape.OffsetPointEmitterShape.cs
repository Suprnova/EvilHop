using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ParticleEmitterKind.OffsetPoint"/>.
    /// </summary>
    public sealed class OffsetPointEmitterShape : ParticleEmitterShape
    {
        /// <summary>The offset from <see cref="ParticleEmitterAsset.Position"/> particles are emitted from.</summary>
        public Vector3 Offset { get; set; }

        internal static OffsetPointEmitterShape Read(EndianReader reader, FormatProfile _) => new()
        {
            Offset = reader.ReadVector3(),
        };

        internal static void Write(OffsetPointEmitterShape value, EndianWriter writer, FormatProfile _) =>
            writer.Write(value.Offset);
    }
}

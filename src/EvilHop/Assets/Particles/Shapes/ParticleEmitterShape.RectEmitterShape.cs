using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ParticleEmitterKind.RectEdge"/> and
    /// <see cref="ParticleEmitterAsset.ParticleEmitterKind.Rect"/>.
    /// </summary>
    public sealed class RectEmitterShape : ParticleEmitterShape
    {
        /// <summary>The rectangle's length along the X axis.</summary>
        public float XLength { get; set; }

        /// <summary>The rectangle's length along the Z axis.</summary>
        public float ZLength { get; set; }

        internal static RectEmitterShape Read(EndianReader reader, FormatProfile _) => new()
        {
            XLength = reader.ReadSingle(),
            ZLength = reader.ReadSingle(),
        };

        internal static void Write(RectEmitterShape value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.XLength);
            writer.Write(value.ZLength);
        }
    }
}

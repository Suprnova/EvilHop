using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ShapeKind.RectEdge"/> and
    /// <see cref="ParticleEmitterAsset.ShapeKind.Rect"/>.
    /// </summary>
    public sealed class Rectangle : ParticleEmitterShape
    {
        /// <summary>The rectangle's length along the X axis.</summary>
        public float XLength { get; set; }

        /// <summary>The rectangle's length along the Z axis.</summary>
        public float ZLength { get; set; }

        internal static Rectangle Read(EndianReader reader, FormatProfile _) => new()
        {
            XLength = reader.ReadSingle(),
            ZLength = reader.ReadSingle(),
        };

        internal static void Write(Rectangle value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.XLength);
            writer.Write(value.ZLength);
        }
    }
}

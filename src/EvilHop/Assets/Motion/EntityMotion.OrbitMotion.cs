using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class EntityMotion
{
    /// <summary>
    /// An <see cref="EntityMotion"/> that circles around a center point.
    /// </summary>
    public sealed class OrbitMotion() : EntityMotion
    {
        /// <summary>The point the entity orbits around.</summary>
        public Vector3 Center { get; set; }

        /// <summary>The orbit's scale along the X axis.</summary>
        public float Width { get; set; }

        /// <summary>The orbit's scale along the Z axis.</summary>
        public float Height { get; set; }

        /// <summary>The time, in seconds, one full orbit takes.</summary>
        public float Period { get; set; }

        private protected override MotionType Type => MotionType.Orbit;

        internal static new OrbitMotion Read(EndianReader reader, FormatProfile _) => new()
        {
            Center = reader.ReadVector3(),
            Width = reader.ReadSingle(),
            Height = reader.ReadSingle(),
            Period = reader.ReadSingle(),
        };

        internal static void Write(OrbitMotion value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Center);
            writer.Write(value.Width);
            writer.Write(value.Height);
            writer.Write(value.Period);
        }
    }
}

using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public sealed partial class VolumeAsset
{
    /// <summary>
    /// The shape of a <see cref="VolumeAsset"/>'s region.
    /// </summary>
    public abstract partial class Bound
    {
        private protected Bound() { }

        /// <summary>
        /// The center of the shape.
        /// </summary>
        public Vector3 Center { get; set; }

        /// <summary>
        /// Which kind of shape this is.
        /// </summary>
        public abstract ShapeKind Kind { get; }

        /// <summary>
        /// The size, in bytes, of the region every <see cref="ShapeKind"/> shares after
        /// <see cref="Kind"/> - the largest of them, a <see cref="Box"/>.
        /// </summary>
        private const int UnionSize = 36;

        /// <exception cref="InvalidDataException">The stored <see cref="ShapeKind"/> is unknown.</exception>
        internal static Bound Read(EndianReader reader, FormatProfile profile)
        {
            var kind = (ShapeKind)reader.ReadByte();
            reader.ReadBytes(3); // padding, always zero

            using var union = new EndianReader(new MemoryStream(reader.ReadBytes(UnionSize)), reader.Endianness);
            var center = union.ReadVector3();
            Bound value = kind switch
            {
                ShapeKind.Sphere => Sphere.Read(union, profile),
                ShapeKind.Box => Box.Read(union, profile),
                ShapeKind.Cylinder => Cylinder.Read(union, profile),
                _ => throw new InvalidDataException($"Unknown volume shape 0x{(byte)kind:X2}."),
            };
            value.Center = center;
            return value;
        }

        internal static void Write(Bound value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write((byte)value.Kind);
            writer.Write(new byte[3]); // padding

            using var stream = new MemoryStream();
            using (var union = new EndianWriter(stream, writer.Endianness, leaveOpen: true))
            {
                union.Write(value.Center);
                switch (value)
                {
                    case Sphere sphere: Sphere.Write(sphere, union, profile); break;
                    case Box box: Box.Write(box, union, profile); break;
                    case Cylinder cylinder: Cylinder.Write(cylinder, union, profile); break;
                }
            }
            writer.Write(stream.ToArray());
            writer.Write(new byte[UnionSize - (int)stream.Length]);
        }

        /// <summary>
        /// Every kind of shape a <see cref="Bound"/> can be.
        /// </summary>
        public enum ShapeKind : byte
        {
            /// <summary>A <see cref="Sphere"/>.</summary>
            Sphere = 1,
            /// <summary>A <see cref="Box"/>.</summary>
            Box = 2,
            /// <summary>A <see cref="Cylinder"/>.</summary>
            Cylinder = 3,
        }
    }
}

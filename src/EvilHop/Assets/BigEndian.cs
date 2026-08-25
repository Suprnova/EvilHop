using System.Buffers.Binary;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// Big-endian write helpers for the field types asset data uses beyond
/// <see cref="Primitives.EvilInt"/>'s unsigned integers.
/// </summary>
/// <remarks>
/// <see cref="BinaryWriter"/> writes multi-byte values little-endian, which is the wrong order for
/// every field in a HIP archive.
/// </remarks>
internal static class BigEndian
{
    extension(BinaryWriter writer)
    {
        /// <summary>Writes <paramref name="value"/> as two big-endian bytes.</summary>
        public void WriteBigEndian(short value)
        {
            Span<byte> bytes = stackalloc byte[2];
            BinaryPrimitives.WriteInt16BigEndian(bytes, value);
            writer.Write(bytes);
        }

        /// <summary>Writes <paramref name="value"/> as four big-endian bytes.</summary>
        public void WriteBigEndian(int value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            writer.Write(bytes);
        }

        /// <summary>Writes <paramref name="value"/> as four big-endian bytes.</summary>
        public void WriteBigEndian(float value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteSingleBigEndian(bytes, value);
            writer.Write(bytes);
        }

        /// <summary>Writes <paramref name="value"/>'s three components as big-endian floats.</summary>
        public void WriteBigEndian(Vector3 value)
        {
            writer.WriteBigEndian(value.X);
            writer.WriteBigEndian(value.Y);
            writer.WriteBigEndian(value.Z);
        }
    }
}

using System.Buffers.Binary;

namespace EvilHop.Extensions;

// todo: rename this namespace to Primitives or something

/// <summary>
/// Integers in EvilEngine's HIP files are unsigned, 32-bit, and in big endian regardless of platform.
/// This class extends multiple types to support handling these specifications for both input and output.
/// </summary>
public static class EvilInt
{
    extension(BinaryReader reader)
    {
        /// <summary>
        /// Reads a 4-byte unsigned integer in big endian from the stream and advances the stream by 4 bytes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <returns>A 4-byte unsigned integer read from this stream.</returns>
        public uint ReadEvilInt() => BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
    }

    extension(BinaryWriter writer)
    {
        /// <summary>
        /// Writes a <see cref="uint"/> formatted as an EvilInt to the underlying stream.
        /// </summary>
        /// <param name="value">The <see cref="uint"/> to write.</param>
        public void WriteEvilInt(uint value) => writer.Write(value.ToEvilBytes());
    }

    extension(uint val)
    {
        /// <summary>
        /// Returns the EvilEngine byte representation of this <see cref="uint"/>.
        /// </summary>
        /// <returns>the EvilEngine byte representation of this <see cref="uint"/>.</returns>
        public byte[] ToEvilBytes()
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, val);
            return bytes.ToArray();
        }
    }

    extension(Span<byte> bytes)
    {
        /// <summary>
        /// Returns the <see cref="uint"/> representation of this <see cref="Span{T}"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <returns>the <see cref="uint"/> representation of this <see cref="Span{T}"/>.</returns>
        public uint ToEvilInt() => BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }
}

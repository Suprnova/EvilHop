using System.Buffers.Binary;

namespace EvilHop.Primitives;

/// <summary>
/// Provides extension methods for reading and writing EvilEngine-formatted integers.
/// </summary>
public static class EvilInt
{
    extension(BinaryReader reader)
    {
        /// <summary>
        /// Reads an EvilEngine-formatted int from the stream and advances the stream by 4 bytes.
        /// </summary>
        /// <returns>An unsigned integer read from the stream.</returns>
        public uint ReadEvilInt() => BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
    }

    extension(BinaryWriter writer)
    {
        /// <summary>
        /// Writes an unsigned integer to the stream in an EvilEngine format and
        /// advances the stream by 4 bytes.
        /// </summary>
        /// <param name="value">The unsigned integer to write to the stream.</param>
        public void WriteEvilInt(uint value) => writer.Write(value.ToEvilBytes());
    }

    extension(uint value)
    {
        /// <summary>
        /// Converts the unsigned integer to a byte array in EvilEngine format.
        /// </summary>
        /// <returns>A 4-byte array representing the unsigned integer in EvilEngine format.</returns>
        public byte[] ToEvilBytes()
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            return bytes;
        }
    }

    extension(Span<byte> bytes)
    {
        /// <summary>
        /// Converts the byte span to an unsigned integer in EvilEngine format.
        /// </summary>
        /// <returns>An unsigned integer parsed from the byte span in EvilEngine format.</returns>
        public uint ToEvilInt() => BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }
}

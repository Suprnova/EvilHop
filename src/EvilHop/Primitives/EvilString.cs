using System.Text;

namespace EvilHop.Primitives;

/// <summary>
/// Strings in EvilEngine's HIP files are 7-bit ASCII (?) and null-terminated. Strings are padded with nulls
/// until the length is a multiple of two bytes.
/// This class extends multiple types to support handling these specifications for both input and output.
/// </summary>
public static class EvilString
{
    extension(BinaryReader reader)
    {
        /// <summary>
        /// Reads an EvilString <see cref="string"/> from the stream and advances the stream to the end of EvilString.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="EndOfStreamException"/>
        /// <returns>A string read from this stream.</returns>
        public String ReadEvilString()
        {
            List<byte> bytes = [];
            while (reader.PeekChar() != '\0')
            {
                bytes.Add(reader.ReadByte());
            }

            // ensure string was terminated corrected, i.e. we didn't consume too much
            int expectedNullCount = bytes.Count % 2 == 0 ? 2 : 1;
            byte[] nullBytes = reader.ReadBytes(expectedNullCount);
            // TODO: custom exception here
            if (nullBytes.Count(new byte[] { 0x00 }) != expectedNullCount) throw new ArgumentOutOfRangeException();

            return Encoding.ASCII.GetString([.. bytes]);
        }
    }

    extension(BinaryWriter writer)
    {
        /// <summary>
        /// Writes a <see cref="string"/> formatted as an EvilString to the underlying stream.
        /// </summary>
        /// <param name="str">The <see cref="string"/> to write.</param>
        public void WriteEvilString(string str) => writer.Write(str.ToEvilBytes());
    }

    extension(string str)
    {
        /// <summary>
        /// Returns the length of this <see cref="string"/> where it formatted as an EvilEngine string.
        /// </summary>
        /// <returns>The length of this <see cref="string"/> where it formatted as an EvilEngine string.</returns>
        public uint GetEvilStringLength() => (uint)(str.Length % 2 == 0 ? str.Length + 2 : str.Length + 1);

        /// <summary>
        /// Returns the EvilEngine byte representation of this <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// Any byte that cannot be represented in ASCII will be displayed as <c>?</c>.
        /// </remarks>
        /// <returns>The EvilEngine byte representation of this <see cref="string"/>.</returns>
        public byte[] ToEvilBytes()
        {
            str = str.Length % 2 == 0 ? str + "\0\0" : str + '\0';
            // todo: ascii or extended ascii?
            return Encoding.ASCII.GetBytes(str);
        }
    }

    extension(Span<byte> bytes)
    {
        /// <summary>
        /// Returns the <see cref="string"/> representation of this <see cref="Span{T}"/>.
        /// </summary>
        /// <remarks>
        /// For parsing a <see cref="string"/> of unknown length, use extension method <see cref="ReadEvilString(BinaryReader)"/>.
        /// Terminates at first occurrence of null byte, does not validate.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <returns>The <see cref="string"/> representation of this <see cref="Span{T}"/>.</returns>
        public String ToEvilString() => Encoding.ASCII.GetString(bytes[..bytes.IndexOf(new byte[] { 0x00 })]);
    }
}

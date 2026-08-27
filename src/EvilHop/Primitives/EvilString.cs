using System.Text;

namespace EvilHop.Primitives;

/// <summary>
/// Provides extension methods for reading and writing EvilEngine-formatted strings.
/// </summary>
public static class EvilString
{
    extension(BinaryReader reader)
    {
        /// <summary>
        /// Reads an EvilEngine-formatted string from the stream and advances the stream
        /// past that string.
        /// </summary>
        /// <returns>A string read from the stream.</returns>
        public string ReadEvilString()
        {
            List<byte> bytes = [];

            byte current;
            while ((current = reader.ReadByte()) != 0x00) bytes.Add(current);

            int expectedNullCount = bytes.Count % 2 == 0 ? 2 : 1;
            var remainingNullBytes = reader.ReadBytes(expectedNullCount - 1);
            if (remainingNullBytes.Any(b => !b.Equals(0x00)))
                throw new InvalidDataException(
                    $"Expected {expectedNullCount} null bytes after string of length {bytes.Count}.");

            return Encoding.ASCII.GetString([.. bytes]);
        }
    }

    extension(BinaryWriter writer)
    {
        /// <summary>
        /// Writes a string to the stream in an EvilEngine format and
        /// advances the stream by that formatted string's length.
        /// </summary>
        /// <param name="str">The string to write to the stream.</param>
        public void WriteEvilString(string str)
        {
            var bytes = Encoding.ASCII.GetBytes(str);
            writer.Write(bytes);
            int nullCount = bytes.Length % 2 == 0 ? 2 : 1;
            writer.Write(new byte[nullCount]);
        }
    }
}

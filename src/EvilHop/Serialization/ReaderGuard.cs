namespace EvilHop.Serialization;

/// <summary>
/// Guards untrusted length and count fields read from a HIP file against oversized reads and allocations.
/// </summary>
internal static class ReaderGuard
{
    /// <summary>
    /// Returns the number of bytes left in the underlying stream, or <see cref="long.MaxValue"/> if unknown.
    /// </summary>
    public static long Remaining(BinaryReader reader) =>
        reader.BaseStream.CanSeek ? reader.BaseStream.Length - reader.BaseStream.Position : long.MaxValue;

    /// <summary>
    /// Throws if <paramref name="count"/> bytes cannot be satisfied by the remaining stream contents.
    /// </summary>
    /// <exception cref="InvalidDataException"/>
    public static void EnsureAvailable(BinaryReader reader, ulong count, string description)
    {
        if (count > int.MaxValue)
            throw new InvalidDataException($"{description} declares {count} bytes, which exceeds the maximum supported size.");

        long remaining = Remaining(reader);
        if (count > (ulong)remaining)
            throw new InvalidDataException($"{description} declares {count} bytes, but only {remaining} bytes remain in the stream.");
    }
}

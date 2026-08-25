namespace EvilHop.Common;

/// <summary>
/// Computes the CRC-32/MPEG-2 checksum <see cref="Blocks.AssetDebug.Checksum"/> is validated
/// against - unreflected, with a <c>0xFFFFFFFF</c> initial value and no final XOR, unlike the more
/// common reflected CRC-32/ISO-HDLC variant.
/// </summary>
public static class Crc32Mpeg2
{
    private const uint Polynomial = 0x04C11DB7;

    private static readonly uint[] Table = BuildTable();

    /// <summary>
    /// Computes the CRC-32/MPEG-2 checksum of <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The bytes to checksum.</param>
    /// <returns>The computed checksum.</returns>
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
            crc = (crc << 8) ^ Table[((crc >> 24) ^ b) & 0xFF];
        return crc;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i << 24;
            for (int bit = 0; bit < 8; bit++)
                crc = (crc & 0x80000000) != 0 ? (crc << 1) ^ Polynomial : crc << 1;
            table[i] = crc;
        }
        return table;
    }
}

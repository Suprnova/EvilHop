using System.Text;

namespace EvilHop.Common;

/// <summary>
/// A helper class used for calculating an <c>Asset</c>'s BKDR hash.
/// </summary>
public static class BKDRHash
{
    /// <summary>
    /// Calculates the BKDR (modified) hash of the provided <paramref name="str"/>.
    /// </summary>
    /// <remarks>
    /// This algorithm is modified from the traditional BKDR algorithm in the sense that
    /// characters between 0x60-0x7F are remapped to 0x40-0x5F.
    /// <para><seealso href="https://discord.com/channels/446321271635050506/469375373067550740/1540563491856064582">Related discussion</seealso></para>
    /// <para><seealso href="https://heavyironmodding.org/wiki/EvilEngine/Assets">Heavy Iron Modding documentation</seealso></para>
    /// </remarks>
    /// <param name="str">The <see langword="string"/> to calculate the hash of.</param>
    /// <returns>The calculated hash.</returns>
    public static uint Calculate(string str)
    {
        uint seed = 131, hash = 0;
        byte[] bytes = [.. Encoding.ASCII.GetBytes(str)];

        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            b = (byte)(b - (b & (b >> 1) & 0x20));

            hash = b == 0 ? hash : (hash * seed) + b;
        }

        return hash;
    }
}

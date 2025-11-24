using EvilHop.Primitives;

namespace EvilHop.Common;

public class BKDRHash
{
    public static uint Calculate(string str)
    {
        uint seed = 131, hash = 0;
        byte[] bytes = [.. str.ToEvilBytes().Take(32)];
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            b = (byte)(b - (b & (b >> 1) & 0x20));

            hash = b == 0 ? hash : (hash * seed) + b;
        }

        return hash;
    }
}

using EvilHop.Primitives;

namespace EvilHop.Helpers;

public class BKDRHash
{
    public static uint Calculate(string str)
    {
        uint seed = 131, hash = 0;
        IEnumerable<byte> bytes = str.ToUpper().ToEvilBytes().Take(32);
        foreach (byte b in bytes)
            hash = b == 0 ? hash : (hash * seed) + b;

        return hash;
    }
}

using EvilHop.Blocks;

namespace EvilHop.Serialization;

public static class BlockFactory
{
    internal static Type GetBlockType(string id)
    {
        return id switch
        {
            "HIPA" => typeof(HIPA),
            "PACK" => typeof(Package),
            "PVER" => typeof(PackageVersion),
            "PFLG" => typeof(PackageFlags),
            "PCNT" => typeof(PackageCount),
            "PCRT" => typeof(PackageCreated),
            "PMOD" => typeof(PackageModified),
            "PLAT" => typeof(PackagePlatform),
            _ => throw new InvalidDataException()
        };
    }
}

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
            "DICT" => typeof(Dictionary),
            "ATOC" => typeof(AssetTable),
            "AINF" => typeof(AssetInf),
            "AHDR" => typeof(AssetHeader),
            "ADBG" => typeof(AssetDebug),
            "LTOC" => typeof(LayerTable),
            "LINF" => typeof(LayerInf),
            "LHDR" => typeof(LayerHeader),
            "LDBG" => typeof(LayerDebug),
            _ => throw new InvalidDataException()
        };
    }
}

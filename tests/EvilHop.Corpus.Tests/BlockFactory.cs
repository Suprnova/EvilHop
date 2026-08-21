using EvilHop.Blocks;
using EvilHop.Serialization;

namespace EvilHop.Corpus.Tests;

/// <summary>
/// Builds synthetic <see cref="Block"/> trees for tests, entirely through EvilHop's public API -
/// <see cref="Serializer.CreateBlock{T}"/>, exactly what a real consumer without
/// <c>InternalsVisibleTo</c> would use.
/// </summary>
internal static class BlockFactory
{
    private static readonly Serializer Serializer = new N100FSerializer();

    public static T Create<T>() where T : Block => Serializer.CreateBlock<T>();

    /// <summary>Builds a minimal, structurally valid Package/Dictionary/AssetStream archive with no assets or layers.</summary>
    public static List<Block> MinimalArchive()
    {
        var package = Create<Package>();
        package.Version = Create<PackageVersion>();
        package.Flags = Create<PackageFlags>();
        package.Counts = Create<PackageCount>();
        package.Created = Create<PackageCreated>();
        package.Modified = Create<PackageModified>();

        var dictionary = Create<Dictionary>();
        var assetTable = Create<AssetTable>();
        assetTable.Inf = Create<AssetInf>();
        dictionary.AssetTable = assetTable;

        var layerTable = Create<LayerTable>();
        layerTable.Inf = Create<LayerInf>();
        dictionary.LayerTable = layerTable;

        var stream = Create<AssetStream>();
        stream.Header = Create<StreamHeader>();
        stream.Data = Create<StreamData>();

        return [Create<HIPA>(), package, dictionary, stream];
    }

    public static AssetHeader CreateAssetHeader(uint id, string name, uint offset = 0, uint size = 0, uint plus = 0)
    {
        var debug = Create<AssetDebug>();
        debug.Name = name;

        var header = Create<AssetHeader>();
        header.Id = id;
        header.Offset = offset;
        header.Size = size;
        header.Plus = plus;
        header.Debug = debug;
        return header;
    }
}

using EvilHop.Blocks;
using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// The root blocks of a new, empty <see cref="GameVersion.TSSM"/> archive. Its values are the ones
/// every NTSC-U US archive of every platform shares; only the <c>PLAT</c> platform identifier differs.
/// </summary>
internal static class TSSMArchiveTemplate
{
    private const uint SubVersion = 2;
    private const uint CompatVersion = 1;
    private const uint StreamHeaderValue = 0xFFFFFFFF;

    private const string Region = "NTSC";
    private const string Language = "US";
    private const string GameName = "Incredibles";

    /// <summary>
    /// Builds the roots of an empty archive stamped as built for <paramref name="platform"/> at
    /// <paramref name="now"/>. Counts and stream padding are left for the first
    /// <see cref="Assets.AssetSession"/> commit to lay out.
    /// </summary>
    public static List<Block> Build(Serializer serializer, Platform platform, DateTimeOffset now)
    {
        var package = serializer.CreateBlock<Package>();
        package.Version = Version(serializer);
        package.Flags = Flags(serializer);
        package.Counts = serializer.CreateBlock<PackageCount>();
        package.Created = new PackageCreated(now);
        package.Modified = Modified(serializer, now);
        package.Platform = Platform(serializer, platform);

        var dictionary = serializer.CreateBlock<Dictionary>();
        dictionary.AssetTable = AssetTable(serializer);
        dictionary.LayerTable = LayerTable(serializer);

        var stream = serializer.CreateBlock<AssetStream>();
        stream.Header = StreamHeader(serializer);
        stream.Data = serializer.CreateBlock<StreamData>();

        return [serializer.CreateBlock<HIPA>(), package, dictionary, stream];
    }

    private static PackageVersion Version(Serializer serializer)
    {
        var version = serializer.CreateBlock<PackageVersion>();
        version.SubVersion = SubVersion;
        version.ClientVersion = ClientVersion.Default;
        version.CompatVersion = CompatVersion;
        return version;
    }

    private static PackageFlags Flags(Serializer serializer)
    {
        var flags = serializer.CreateBlock<PackageFlags>();
        flags.Flags = PackFlags.Default;
        return flags;
    }

    private static PackageModified Modified(Serializer serializer, DateTimeOffset now)
    {
        var modified = serializer.CreateBlock<PackageModified>();
        modified.ModifiedDate = now;
        return modified;
    }

    private static PackagePlatform Platform(Serializer serializer, Platform platform)
    {
        var stamp = serializer.CreateBlock<PackagePlatform>();
        stamp.PlatformId = PlatformIds.Id(platform);
        stamp.Region = Region;
        stamp.Language = Language;
        stamp.GameName = GameName;
        return stamp;
    }

    private static AssetTable AssetTable(Serializer serializer)
    {
        var table = serializer.CreateBlock<AssetTable>();
        table.Inf = serializer.CreateBlock<AssetInf>();
        return table;
    }

    private static LayerTable LayerTable(Serializer serializer)
    {
        var table = serializer.CreateBlock<LayerTable>();
        table.Inf = serializer.CreateBlock<LayerInf>();
        return table;
    }

    private static StreamHeader StreamHeader(Serializer serializer)
    {
        var header = serializer.CreateBlock<StreamHeader>();
        header.Value = StreamHeaderValue;
        return header;
    }
}

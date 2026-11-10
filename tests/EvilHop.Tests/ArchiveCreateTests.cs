using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;
using EvilHop.Serialization.Sniffing;

namespace EvilHop.Tests;

public class ArchiveCreateTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 8, 7, TimeSpan.Zero);

    public static TheoryData<Platform> AllPlatforms => [Platform.GameCube, Platform.Xbox, Platform.PlayStation2];

    public static TheoryData<Platform, string> PlatformStamps => new()
    {
        { Platform.GameCube, "GC" },
        { Platform.Xbox, "BX" },
        { Platform.PlayStation2, "PS2" }
    };

    public static TheoryData<Platform, int> DataAlignments => new()
    {
        { Platform.GameCube, 32 },
        { Platform.Xbox, 2048 },
        { Platform.PlayStation2, 2048 }
    };

    private static Archive Create(Platform platform) =>
        Archive.Create(Serializer.Create(GameVersion.TSSM, platform), Now);

    private static byte[] Save(Archive archive)
    {
        using var stream = new MemoryStream();
        archive.Save(stream);
        return stream.ToArray();
    }

    [Theory]
    [MemberData(nameof(PlatformStamps))]
    public void Create_StampsThePackageTheWayRealArchivesAre(Platform platform, string id)
    {
        var package = Create(platform).Roots.OfType<Package>().Single();

        Assert.Equal(["PVER", "PFLG", "PCNT", "PCRT", "PMOD", "PLAT"], package.Children.Select(child => child.Tag));
        Assert.Equal(2u, package.Version.SubVersion);
        Assert.Equal(ClientVersion.Default, package.Version.ClientVersion);
        Assert.Equal(1u, package.Version.CompatVersion);
        Assert.Equal(PackFlags.Default, package.Flags.Flags);
        Assert.Equal(id, package.Platform!.PlatformId);
        Assert.Null(package.Platform.PlatformName);
        Assert.Equal("NTSC", package.Platform.Region);
        Assert.Equal("US", package.Platform.Language);
        Assert.Equal("Incredibles", package.Platform.GameName);
    }

    [Fact]
    public void Create_StampsCreationAndModificationWithTheCurrentTime()
    {
        var package = Create(Platform.Xbox).Roots.OfType<Package>().Single();

        Assert.Equal(Now, package.Created.CreatedDate);
        Assert.Equal("Tue Nov 10 09:08:07 2026", package.Created.CreatedDateString);
        Assert.Equal(Now, package.Modified.ModifiedDate);
    }

    [Fact]
    public void Create_WithoutAFixedTime_UsesTheCurrentTime()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var created = Archive.Create(Serializer.Create(GameVersion.TSSM, Platform.Xbox))
            .Roots.OfType<Package>().Single().Created.CreatedDate;

        Assert.InRange(created, before, DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Theory]
    [MemberData(nameof(DataAlignments))]
    public void Create_HasNoAssetsOrLayersAndPadsToThePlatformsAlignment(Platform platform, int alignment)
    {
        var archive = Create(platform);

        Assert.Equal(["HIPA", "PACK", "DICT", "STRM"], archive.Roots.Select(root => root.Tag));
        var counts = archive.Roots.OfType<Package>().Single().Counts;
        Assert.Equal(0u, counts.AssetCount);
        Assert.Equal(0u, counts.LayerCount);
        Assert.Equal(0, Save(archive).Length % alignment);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void Create_ThenSave_ReloadsExplicitlyAndSavesIdenticalBytes(Platform platform)
    {
        byte[] created = Save(Create(platform));

        var reloaded = Archive.Load(new MemoryStream(created), GameVersion.TSSM, platform);

        Assert.Equal(created, Save(reloaded));
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void Create_ThenSave_SniffsAsStronglyThePlatformItWasCreatedFor(Platform platform)
    {
        byte[] created = Save(Create(platform));

        var result = Serializer.Sniff(new MemoryStream(created), GameVersion.TSSM);

        Assert.True(result.IsStrong);
        Assert.Equal(PlatformSource.PlatStamp, result.PlatformSource);
        Assert.Equal(platform, result.Profile!.Platform);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void Create_ThenAddingACounter_ReadsBackUnderTheCreatedPlatformOnly(Platform platform)
    {
        var archive = Create(platform);
        using (var session = archive.OpenAssets())
        {
            var counter = new CounterAsset { Name = "counter", InitialValue = 3 };
            counter.CalculateId();
            session.CreateLayer(LayerType.Default).Add(counter);
        }
        byte[] saved = Save(archive);
        var otherPlatform = platform is Platform.GameCube ? Platform.Xbox : Platform.GameCube;

        short Read(Platform readAs)
        {
            using var session = Archive.Load(new MemoryStream(saved), GameVersion.TSSM, readAs).OpenAssets();
            return session.Layers.SelectMany(layer => layer.Assets).OfType<CounterAsset>().Single().InitialValue;
        }

        Assert.Equal(3, Read(platform));
        Assert.Equal(768, Read(otherPlatform));
    }

    [Fact]
    public void Create_ForAnotherGame_ThrowsNotSupportedException() =>
        Assert.Throws<NotSupportedException>(() => Archive.Create(Serializer.Create(GameVersion.BFBB, Platform.GameCube)));

    [Fact]
    public void Create_SerializerStatingNoPlatform_ThrowsInvalidOperationException() =>
        Assert.Throws<InvalidOperationException>(() => Archive.Create(Serializer.Create(GameVersion.TSSM)));

    [Fact]
    public void Create_NullSerializer_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => Archive.Create(null!));

    [Fact]
    public void Save_LoadedArchive_KeepsItsPlatBlock()
    {
        var created = Create(Platform.Xbox);
        var package = created.Roots.OfType<Package>().Single();
        package.Platform!.Region = "PAL";
        package.Platform.Language = "UK";
        byte[] saved = Save(created);

        var reloaded = Archive.Load(new MemoryStream(saved), GameVersion.TSSM, Platform.PlayStation2);

        Assert.Equal(saved, Save(reloaded));
        var stamp = reloaded.Roots.OfType<Package>().Single().Platform!;
        Assert.Equal("BX", stamp.PlatformId);
        Assert.Equal("PAL", stamp.Region);
    }
}

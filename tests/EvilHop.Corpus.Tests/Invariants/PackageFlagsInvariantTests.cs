using EvilHop.Blocks;
using EvilHop.Corpus.Archives;
using EvilHop.Corpus.Invariants;

namespace EvilHop.Corpus.Tests.Invariants;

public class PackageFlagsDefaultAlwaysPresentInvariantTests
{
    private static ArchiveContext ArchiveOf(params Block[] roots) => new()
    {
        BuildKey = "bfbb/release",
        RelativePath = "bfbb/release/boot.HIP",
        Roots = roots,
        ArchiveLength = 0
    };

    private static PackageFlags Flags(PackFlags flags)
    {
        var block = BlockFactory.Create<PackageFlags>();
        block.Flags = flags;
        return block;
    }

    [Fact]
    public void Check_DefaultPresent_Passes()
    {
        var invariant = new PackageFlagsDefaultAlwaysPresentInvariant();

        invariant.Check(ArchiveOf(Flags(PackFlags.Default)));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(0, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(1, outcomes["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_DefaultPresentWithFullUpperWord_Passes()
    {
        var invariant = new PackageFlagsDefaultAlwaysPresentInvariant();

        var flags = PackFlags.Default
            | PackFlags.GameCube
            | PackFlags.NTSC
            | PackFlags.LanguageUSCommon
            | PackFlags.Platform;
        invariant.Check(ArchiveOf(Flags(flags)));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(0, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(1, outcomes["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_DefaultMissing_RecordsViolation()
    {
        var invariant = new PackageFlagsDefaultAlwaysPresentInvariant();

        invariant.Check(ArchiveOf(Flags(PackFlags.GameCube)));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(1, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(0, outcomes["passing"]!.GetValue<long>());
    }
}

public class PackageFlagsUpperWordInvariantTests
{
    private static ArchiveContext ArchiveOf(params Block[] roots) => new()
    {
        BuildKey = "bfbb/release",
        RelativePath = "bfbb/release/boot.HIP",
        Roots = roots,
        ArchiveLength = 0
    };

    private static PackageFlags Flags(PackFlags flags)
    {
        var block = BlockFactory.Create<PackageFlags>();
        block.Flags = flags;
        return block;
    }

    private static PackagePlatform Platform(
        string platformId = "GC",
        string region = "NTSC",
        string language = "US Common")
    {
        var block = BlockFactory.Create<PackagePlatform>();
        block.PlatformId = platformId;
        block.Region = region;
        block.Language = language;
        return block;
    }

    [Fact]
    public void Check_NoPlatformBitAndZeroUpperWord_Passes()
    {
        var invariant = new PackageFlagsUpperWordInvariant();

        invariant.Check(ArchiveOf(Flags(PackFlags.Default)));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(0, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(1, outcomes["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_PlatformBitAgreesWithPlat_Passes()
    {
        var invariant = new PackageFlagsUpperWordInvariant();

        var flags = PackFlags.Default
            | PackFlags.GameCube
            | PackFlags.NTSC
            | PackFlags.LanguageUSCommon
            | PackFlags.Platform;
        invariant.Check(ArchiveOf(Flags(flags), Platform()));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(0, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(1, outcomes["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_NoPlatformBitButNonZeroUpperWord_RecordsViolation()
    {
        var invariant = new PackageFlagsUpperWordInvariant();

        invariant.Check(ArchiveOf(Flags(PackFlags.Default | PackFlags.GameCube)));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(1, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(0, outcomes["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_PlatformBitPresentButNoPlatBlock_RecordsViolation()
    {
        var invariant = new PackageFlagsUpperWordInvariant();

        var flags = PackFlags.Default
            | PackFlags.GameCube
            | PackFlags.NTSC
            | PackFlags.LanguageUSCommon
            | PackFlags.Platform;
        invariant.Check(ArchiveOf(Flags(flags)));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(1, outcomes["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_TwoPlatformBitsSet_RecordsViolation()
    {
        var invariant = new PackageFlagsUpperWordInvariant();

        var flags = PackFlags.Default
            | PackFlags.GameCube
            | PackFlags.Xbox
            | PackFlags.NTSC
            | PackFlags.LanguageUSCommon
            | PackFlags.Platform;
        invariant.Check(ArchiveOf(Flags(flags), Platform()));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(1, outcomes["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_PlatformBitDisagreesWithPlat_RecordsViolation()
    {
        var invariant = new PackageFlagsUpperWordInvariant();

        var flags = PackFlags.Default
            | PackFlags.GameCube
            | PackFlags.NTSC
            | PackFlags.LanguageUSCommon
            | PackFlags.Platform;
        invariant.Check(ArchiveOf(Flags(flags), Platform(platformId: "XB")));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(1, outcomes["violated"]!.GetValue<long>());
    }
}

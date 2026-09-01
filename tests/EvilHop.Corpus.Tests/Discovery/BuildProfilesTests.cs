using EvilHop.Common;
using EvilHop.Corpus.Discovery;
using EvilHop.Serialization;

namespace EvilHop.Corpus.Tests.Discovery;

public class BuildProfilesTests
{
    [Theory]
    [InlineData("bfbb/release/GC/NTSC-U/US", GameVersion.BFBB)]
    [InlineData("n100f/prototype_2001-01-31/PS2/NTSC-U/US", GameVersion.N100F)]
    [InlineData("rotu/release/XBOX/PAL/UK", GameVersion.ROTU)]
    public void GameFor_KnownGameDirectory_ReturnsTheGame(string buildDirectory, GameVersion expected) =>
        Assert.Equal(expected, BuildProfiles.GameFor(buildDirectory));

    [Fact]
    public void GameFor_UnknownGameDirectory_Throws() =>
        Assert.Throws<InvalidOperationException>(() => BuildProfiles.GameFor("unknown/release/GC/NTSC-U/US"));

    [Theory]
    [InlineData("bfbb/release/GC/NTSC-U/US", Platform.GameCube)]
    [InlineData("bfbb/release/PS2/PAL/DE", Platform.PlayStation2)]
    [InlineData("bfbb/release/XBOX/NTSC-U/US", Platform.Xbox)]
    public void PlatformFor_KnownPlatformDirectory_ReturnsThePlatform(string buildDirectory, Platform expected) =>
        Assert.Equal(expected, BuildProfiles.PlatformFor(buildDirectory));

    [Fact]
    public void PlatformFor_NoKnownPlatformSegment_Throws() =>
        Assert.Throws<InvalidOperationException>(() => BuildProfiles.PlatformFor("bfbb/release/NTSC-U/US"));

    [Fact]
    public void ProfileFor_ReturnsTheGamesDefaultProfile_AdjustedForThePlatform()
    {
        var profile = BuildProfiles.ProfileFor(GameVersion.TSSM, Platform.PlayStation2);

        Assert.Equal(GameVersion.TSSM, profile.Game);
        Assert.Equal(Platform.PlayStation2, profile.Platform);
    }

    [Fact]
    public void SerializerFor_ReturnsASerializer_ConstructedWithTheGivenProfile()
    {
        var profile = BuildProfiles.ProfileFor(GameVersion.TSSM, Platform.PlayStation2);

        var serializer = BuildProfiles.SerializerFor(GameVersion.TSSM, profile);

        Assert.Same(profile, serializer.Profile);
    }
}

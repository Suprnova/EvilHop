using EvilHop.Common;
using EvilHop.Corpus.Archives;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Corpus.Tests.Archives;

public class BuildProfilesTests
{
    private static readonly FormatProfile DefaultProfile = N100FSerializer.DefaultProfile;

    [Fact]
    public void Resolve_MatchingPrefix_AppliesOverride()
    {
        var manifest = BuildProfiles.Load(
            """[{ "pathPattern": "n100f/prototype_2001-06-11", "profile": { "streamDataHasPaddingField": false } }]""");

        var resolved = manifest.Resolve(DefaultProfile, "n100f/prototype_2001-06-11/PS2/NTSC-U/US/FOO1.HIP");

        Assert.False(resolved.StreamDataHasPaddingField);
    }

    [Fact]
    public void Resolve_NonMatchingPath_ReturnsDefaultUnchanged()
    {
        var manifest = BuildProfiles.Load(
            """[{ "pathPattern": "n100f/prototype_2001-06-11", "profile": { "streamDataHasPaddingField": false } }]""");

        var resolved = manifest.Resolve(DefaultProfile, "n100f/release/GC/NTSC-U/US/B0/b001.HIP");

        Assert.Equal(DefaultProfile, resolved);
    }

    [Fact]
    public void Resolve_MultipleMatches_AppliesFirst()
    {
        var manifest = BuildProfiles.Load(
            """
            [
              { "pathPattern": "n100f/prototype", "profile": { "streamDataHasPaddingField": false } },
              { "pathPattern": "n100f/prototype_2001-06-11", "profile": { "platformFieldOrder": "LanguageRegion" } }
            ]
            """);

        var resolved = manifest.Resolve(DefaultProfile, "n100f/prototype_2001-06-11/PS2/NTSC-U/US/FOO1.HIP");

        Assert.False(resolved.StreamDataHasPaddingField);
        Assert.Equal(PlatformFieldOrder.PlatformNameRegionLanguage, resolved.PlatformFieldOrder);
    }

    [Fact]
    public void Resolve_PartialOverride_LeavesOtherSwitchesIntact()
    {
        var manifest = BuildProfiles.Load(
            """[{ "pathPattern": "n100f/prototype_2001-06-11", "profile": { "streamDataHasPaddingField": false } }]""");

        var resolved = manifest.Resolve(DefaultProfile, "n100f/prototype_2001-06-11/PS2/NTSC-U/US/FOO1.HIP");

        Assert.Equal(DefaultProfile.PlatformFieldOrder, resolved.PlatformFieldOrder);
        Assert.Equal(DefaultProfile.Game, resolved.Game);
    }

    /// <summary>
    /// The manifest is the only thing that can name a platform for N100F, whose archives carry no
    /// <c>PLAT</c> block and leave <c>PFLG</c>'s platform bits zero.
    /// </summary>
    [Fact]
    public void Resolve_PlatformOverride_ChangesPlatformAndTheEndiannessDerivedFromIt()
    {
        var manifest = BuildProfiles.Load(
            """[{ "pathPattern": "n100f/release/PS2", "profile": { "platform": "PlayStation2" } }]""");

        var resolved = manifest.Resolve(DefaultProfile, "n100f/release/PS2/NTSC-U/US/B0/B001.HIP");

        Assert.Equal(Platform.PlayStation2, resolved.Platform);
        Assert.Equal(Endianness.Little, resolved.Endianness);
    }

    [Fact]
    public void Load_EmptyPathPattern_Throws()
    {
        Assert.Throws<ArgumentException>(() => BuildProfiles.Load(
            """[{ "pathPattern": "", "profile": { "streamDataHasPaddingField": false } }]"""));
    }

    [Fact]
    public void Resolve_DoubleStarGlob_MatchesAcrossVaryingBuildDirectories()
    {
        var manifest = BuildProfiles.Load(
            """[{ "pathPattern": "bfbb/**/b3/b301.hip", "profile": { "surfaceHasDamageFields": false } }]""");

        var release = manifest.Resolve(DefaultProfile, "bfbb/release/GC/NTSC-U/US/b3/b301.HIP");
        var prototype = manifest.Resolve(DefaultProfile, "bfbb/prototype_2003-10-01/GC/NTSC-U/US/b3/b301.HIP");

        Assert.False(release.SurfaceHasDamageFields);
        Assert.False(prototype.SurfaceHasDamageFields);
    }

    [Fact]
    public void Resolve_DoubleStarGlob_DoesNotMatchADifferentFileInTheSameGame()
    {
        var manifest = BuildProfiles.Load(
            """[{ "pathPattern": "bfbb/**/b3/b301.hip", "profile": { "surfaceHasDamageFields": false } }]""");

        var resolved = manifest.Resolve(DefaultProfile, "bfbb/release/GC/NTSC-U/US/b3/b302.HIP");

        Assert.Equal(DefaultProfile, resolved);
    }

    [Fact]
    public void Resolve_SingleStarGlob_MatchesExactlyOnePathSegment()
    {
        var manifest = BuildProfiles.Load(
            """[{ "pathPattern": "n100f/release/*/NTSC-U/**", "profile": { "streamDataHasPaddingField": false } }]""");

        var matches = manifest.Resolve(DefaultProfile, "n100f/release/GC/NTSC-U/US/B0/b001.HIP");
        var doesNotMatch = manifest.Resolve(DefaultProfile, "n100f/release/GC/PS2/NTSC-U/US/B0/b001.HIP");

        Assert.False(matches.StreamDataHasPaddingField);
        Assert.Equal(DefaultProfile, doesNotMatch);
    }
}

using EvilHop.Corpus.Archives;
using EvilHop.Serialization;

namespace EvilHop.Corpus.Tests.Archives;

public class BuildProfilesTests
{
    private static readonly FormatProfile DefaultProfile = N100FSerializer.DefaultProfile;

    [Fact]
    public void Resolve_MatchingPrefix_AppliesOverride()
    {
        var manifest = BuildProfiles.Load(
            """[{ "pathPrefix": "n100f/prototype_2001-06-11", "profile": { "streamDataHasPaddingField": false } }]""");

        var resolved = manifest.Resolve(DefaultProfile, "n100f/prototype_2001-06-11/PS2/NTSC-U/US/FOO1.HIP");

        Assert.False(resolved.StreamDataHasPaddingField);
    }

    [Fact]
    public void Resolve_NonMatchingPath_ReturnsDefaultUnchanged()
    {
        var manifest = BuildProfiles.Load(
            """[{ "pathPrefix": "n100f/prototype_2001-06-11", "profile": { "streamDataHasPaddingField": false } }]""");

        var resolved = manifest.Resolve(DefaultProfile, "n100f/release/GC/NTSC-U/US/B0/b001.HIP");

        Assert.Equal(DefaultProfile, resolved);
    }

    [Fact]
    public void Resolve_MultipleMatches_AppliesFirst()
    {
        var manifest = BuildProfiles.Load(
            """
            [
              { "pathPrefix": "n100f/prototype", "profile": { "streamDataHasPaddingField": false } },
              { "pathPrefix": "n100f/prototype_2001-06-11", "profile": { "platformFieldOrder": "LanguageRegion" } }
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
            """[{ "pathPrefix": "n100f/prototype_2001-06-11", "profile": { "streamDataHasPaddingField": false } }]""");

        var resolved = manifest.Resolve(DefaultProfile, "n100f/prototype_2001-06-11/PS2/NTSC-U/US/FOO1.HIP");

        Assert.Equal(DefaultProfile.PlatformFieldOrder, resolved.PlatformFieldOrder);
        Assert.Equal(DefaultProfile.Game, resolved.Game);
    }

    [Fact]
    public void Load_EmptyPathPrefix_Throws()
    {
        Assert.Throws<ArgumentException>(() => BuildProfiles.Load(
            """[{ "pathPrefix": "", "profile": { "streamDataHasPaddingField": false } }]"""));
    }
}

using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class FormatProfileTests
{
    [Fact]
    public void Profile_WithSwitchChanged_LeavesOtherSwitchesIntact()
    {
        var profile = new FormatProfile(GameVersion.N100F, Platform.GameCube, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: true);

        var changed = profile with { StreamDataHasPaddingField = false };

        Assert.Equal(GameVersion.N100F, changed.Game);
        Assert.Equal(PlatformFieldOrder.PlatformNameRegionLanguage, changed.PlatformFieldOrder);
        Assert.False(changed.StreamDataHasPaddingField);
    }

    [Fact]
    public void EntityHasPadding_DefaultsToFalse()
    {
        var profile = new FormatProfile(GameVersion.N100F, Platform.GameCube, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: true);

        Assert.False(profile.EntityHasPadding);
    }

    [Fact]
    public void EntityHasPadding_IsSetOnlyByBFBB()
    {
        Assert.True(BFBBSerializer.DefaultProfile.OnGameCube().EntityHasPadding);

        Assert.False(N100FSerializer.DefaultProfile.OnGameCube().EntityHasPadding);
        Assert.False(TSSMSerializer.DefaultProfile.OnGameCube().EntityHasPadding);
        Assert.False(IncrediblesSerializer.DefaultProfile.OnGameCube().EntityHasPadding);
        Assert.False(ROTUSerializer.DefaultProfile.OnGameCube().EntityHasPadding);
        Assert.False(RatatouilleSerializer.DefaultProfile.OnGameCube().EntityHasPadding);
    }

    [Fact]
    public void Endianness_DefaultsToBig()
    {
        var profile = new FormatProfile(GameVersion.N100F, Platform.GameCube, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: true);

        Assert.Equal(Endianness.Big, profile.Endianness);
    }

    [Theory]
    [InlineData(Platform.Xbox)]
    [InlineData(Platform.PlayStation2)]
    public void Endianness_IsLittle_ForNonGameCubePlatforms(Platform platform)
    {
        var profile = new FormatProfile(GameVersion.N100F, platform, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: true);

        Assert.Equal(Endianness.Little, profile.Endianness);
    }

    [Fact]
    public void Endianness_Throws_WhenNoPlatformIsStated()
    {
        var profile = new FormatProfile(GameVersion.N100F, null, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: true);

        Assert.Throws<InvalidOperationException>(() => profile.Endianness);
    }
}

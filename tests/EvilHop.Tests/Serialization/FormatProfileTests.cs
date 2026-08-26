using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class FormatProfileTests
{
    [Fact]
    public void Profile_WithSwitchChanged_LeavesOtherSwitchesIntact()
    {
        var profile = new FormatProfile(GameVersion.N100F, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: true);

        var changed = profile with { StreamDataHasPaddingField = false };

        Assert.Equal(GameVersion.N100F, changed.Game);
        Assert.Equal(PlatformFieldOrder.PlatformNameRegionLanguage, changed.PlatformFieldOrder);
        Assert.False(changed.StreamDataHasPaddingField);
    }

    [Fact]
    public void EntityHasPadding_DefaultsToFalse()
    {
        var profile = new FormatProfile(GameVersion.N100F, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: true);

        Assert.False(profile.EntityHasPadding);
    }

    [Fact]
    public void EntityHasPadding_IsSetOnlyByBFBB()
    {
        Assert.True(BFBBSerializer.DefaultProfile.EntityHasPadding);

        Assert.False(N100FSerializer.DefaultProfile.EntityHasPadding);
        Assert.False(TSSMSerializer.DefaultProfile.EntityHasPadding);
        Assert.False(IncrediblesSerializer.DefaultProfile.EntityHasPadding);
        Assert.False(ROTUSerializer.DefaultProfile.EntityHasPadding);
        Assert.False(RatatouilleSerializer.DefaultProfile.EntityHasPadding);
    }
}

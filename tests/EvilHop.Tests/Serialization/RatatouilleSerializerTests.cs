using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class RatatouilleSerializerTests
{
    [Fact]
    public void DefaultProfile_IsRatatouilleWithLanguageRegionPlatformOrder()
    {
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(GameVersion.Ratatouille, profile.Game);
        Assert.Equal(PlatformFieldOrder.LanguageRegion, profile.PlatformFieldOrder);
        Assert.True(profile.StreamDataHasPaddingField);
    }

    [Fact]
    public void Constructor_WithoutProfile_UsesDefaultProfile()
    {
        var serializer = new RatatouilleSerializer();

        Assert.Equal(RatatouilleSerializer.DefaultProfile, serializer.Profile);
    }

    [Fact]
    public void Constructor_WithOverride_KeepsOtherSwitches()
    {
        var profile = RatatouilleSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = new RatatouilleSerializer(profile);

        Assert.False(serializer.Profile.StreamDataHasPaddingField);
        Assert.Equal(RatatouilleSerializer.DefaultProfile.PlatformFieldOrder, serializer.Profile.PlatformFieldOrder);
    }
}

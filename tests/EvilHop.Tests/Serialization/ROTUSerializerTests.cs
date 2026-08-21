using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class ROTUSerializerTests
{
    [Fact]
    public void DefaultProfile_IsROTUWithLanguageRegionPlatformOrder()
    {
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(GameVersion.ROTU, profile.Game);
        Assert.Equal(PlatformFieldOrder.LanguageRegion, profile.PlatformFieldOrder);
        Assert.True(profile.StreamDataHasPaddingField);
    }

    [Fact]
    public void Constructor_WithoutProfile_UsesDefaultProfile()
    {
        var serializer = new ROTUSerializer();

        Assert.Equal(ROTUSerializer.DefaultProfile, serializer.Profile);
    }

    [Fact]
    public void Constructor_WithOverride_KeepsOtherSwitches()
    {
        var profile = ROTUSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = new ROTUSerializer(profile);

        Assert.False(serializer.Profile.StreamDataHasPaddingField);
        Assert.Equal(ROTUSerializer.DefaultProfile.PlatformFieldOrder, serializer.Profile.PlatformFieldOrder);
    }
}

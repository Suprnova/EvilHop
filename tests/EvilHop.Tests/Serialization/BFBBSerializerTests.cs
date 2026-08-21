using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class BFBBSerializerTests
{
    [Fact]
    public void DefaultProfile_IsBFBBWithPaddingField()
    {
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(GameVersion.BFBB, profile.Game);
        Assert.True(profile.StreamDataHasPaddingField);
    }

    [Fact]
    public void Constructor_WithoutProfile_UsesDefaultProfile()
    {
        var serializer = new BFBBSerializer();

        Assert.Equal(BFBBSerializer.DefaultProfile, serializer.Profile);
    }

    [Fact]
    public void Constructor_WithOverride_KeepsOtherSwitches()
    {
        var profile = BFBBSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = new BFBBSerializer(profile);

        Assert.False(serializer.Profile.StreamDataHasPaddingField);
        Assert.Equal(BFBBSerializer.DefaultProfile.PlatformFieldOrder, serializer.Profile.PlatformFieldOrder);
    }
}

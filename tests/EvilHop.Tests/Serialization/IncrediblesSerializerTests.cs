using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class IncrediblesSerializerTests : SerializerContractTests
{
    protected override Serializer CreateSerializer() => new IncrediblesSerializer();

    protected override FileStream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "incredibles", "minimal.hip"));

    [Fact]
    public void DefaultProfile_IsIncrediblesWithLanguageRegionPlatformOrder()
    {
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(GameVersion.Incredibles, profile.Game);
        Assert.Equal(PlatformFieldOrder.LanguageRegion, profile.PlatformFieldOrder);
        Assert.True(profile.StreamDataHasPaddingField);
    }

    [Fact]
    public void Constructor_WithoutProfile_UsesDefaultProfile()
    {
        var serializer = new IncrediblesSerializer();

        Assert.Equal(IncrediblesSerializer.DefaultProfile, serializer.Profile);
    }

    [Fact]
    public void Constructor_WithOverride_KeepsOtherSwitches()
    {
        var profile = IncrediblesSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = new IncrediblesSerializer(profile);

        Assert.False(serializer.Profile.StreamDataHasPaddingField);
        Assert.Equal(IncrediblesSerializer.DefaultProfile.PlatformFieldOrder, serializer.Profile.PlatformFieldOrder);
    }
}

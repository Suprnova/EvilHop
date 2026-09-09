using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class TSSMSerializerTests : SerializerContractTests
{
    protected override Serializer CreateSerializer() => new TSSMSerializer();

    protected override FileStream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "tssm", "minimal.hip"));

    [Fact]
    public void DefaultProfile_IsTSSMWithLanguageRegionPlatformOrder()
    {
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(GameVersion.TSSM, profile.Game);
        Assert.Equal(PlatformFieldOrder.LanguageRegion, profile.PlatformFieldOrder);
        Assert.True(profile.StreamDataHasPaddingField);
    }

    [Fact]
    public void Constructor_WithoutProfile_UsesDefaultProfile()
    {
        var serializer = new TSSMSerializer();

        Assert.Equal(TSSMSerializer.DefaultProfile, serializer.Profile);
    }

    [Fact]
    public void Constructor_WithOverride_KeepsOtherSwitches()
    {
        var profile = TSSMSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = new TSSMSerializer(profile);

        Assert.False(serializer.Profile.StreamDataHasPaddingField);
        Assert.Equal(TSSMSerializer.DefaultProfile.PlatformFieldOrder, serializer.Profile.PlatformFieldOrder);
    }
}

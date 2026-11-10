using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class N100FSerializerTests : SerializerContractTests
{
    protected override Serializer CreateSerializer() => new N100FSerializer();

    [Fact]
    public void DefaultProfile_IsN100FWithPaddingField()
    {
        var profile = N100FSerializer.DefaultProfile.OnGameCube();

        Assert.Equal(GameVersion.N100F, profile.Game);
        Assert.True(profile.StreamDataHasPaddingField);
    }

    [Fact]
    public void Constructor_WithoutProfile_UsesDefaultProfile()
    {
        var serializer = new N100FSerializer();

        Assert.Equal(N100FSerializer.DefaultProfile, serializer.Profile);
        Assert.Null(serializer.Profile.Platform);
    }

    [Fact]
    public void Constructor_WithOverride_KeepsOtherSwitches()
    {
        var profile = N100FSerializer.DefaultProfile.OnGameCube() with { StreamDataHasPaddingField = false };

        var serializer = new N100FSerializer(profile);

        Assert.False(serializer.Profile.StreamDataHasPaddingField);
        Assert.Equal(N100FSerializer.DefaultProfile.OnGameCube().PlatformFieldOrder, serializer.Profile.PlatformFieldOrder);
    }
}

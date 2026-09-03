using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class BFBBSerializerTests : SerializerContractTests
{
    protected override Serializer CreateSerializer() => new BFBBSerializer();

    protected override FileStream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "bfbb", "minimal.hip"));

    /// <summary>
    /// <see cref="OpenMinimalFixture"/> deliberately has no <c>PLAT</c> block, to exercise the shared
    /// envelope every game owes. <c>PLAT</c> is the one confirmed structural change BFBB introduces
    /// over N100F, so it gets its own fixture here rather than living in the shared base.
    /// </summary>
    private static FileStream OpenMinimalWithPlatFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "bfbb", "minimal-with-plat.hip"));

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

    [Fact]
    public void Read_MinimalWithPlatFixture_PackHasPlatformChild()
    {
        using var stream = OpenMinimalWithPlatFixture();
        var roots = CreateSerializer().Read(stream);
        var pack = (Package)roots[1];

        Assert.Equal(["PVER", "PFLG", "PCNT", "PCRT", "PMOD", "PLAT"], pack.Children.Select(c => c.Tag));
        Assert.NotNull(pack.Platform);
    }

    [Fact]
    public void Read_MinimalWithPlatFixture_PlatformFieldsMatchFixture()
    {
        using var stream = OpenMinimalWithPlatFixture();
        var roots = CreateSerializer().Read(stream);
        var platform = ((Package)roots[1]).Platform!;

        Assert.Equal("GC", platform.PlatformId);
        Assert.Equal("GameCube", platform.PlatformName);
        Assert.Equal("NTSC", platform.Region);
        Assert.Equal("US Common", platform.Language);
        Assert.Equal("Sponge Bob", platform.GameName);
    }
}

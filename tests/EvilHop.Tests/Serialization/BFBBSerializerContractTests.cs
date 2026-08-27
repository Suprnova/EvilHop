using EvilHop.Blocks;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class BFBBSerializerContractTests : SerializerContractTests
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
    public void Read_MinimalWithPlatFixture_PackHasPlatformChild()
    {
        var roots = CreateSerializer().Read(OpenMinimalWithPlatFixture());
        var pack = (Package)roots[1];

        Assert.Equal(["PVER", "PFLG", "PCNT", "PCRT", "PMOD", "PLAT"], pack.Children.Select(c => c.Tag));
        Assert.NotNull(pack.Platform);
    }

    [Fact]
    public void Read_MinimalWithPlatFixture_PlatformFieldsMatchFixture()
    {
        var roots = CreateSerializer().Read(OpenMinimalWithPlatFixture());
        var platform = ((Package)roots[1]).Platform!;

        Assert.Equal("GC", platform.PlatformId);
        Assert.Equal("GameCube", platform.PlatformName);
        Assert.Equal("NTSC", platform.Region);
        Assert.Equal("US Common", platform.Language);
        Assert.Equal("Sponge Bob", platform.GameName);
    }
}

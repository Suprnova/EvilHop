using EvilHop.Blocks;

namespace EvilHop.Tests.Blocks;

public class HIPBTests
{
    [Fact]
    public void HIPB_Tag_IsCorrect()
    {
        var hipb = new HIPB();
        Assert.Equal("HIPB", hipb.Tag);
    }

    [Fact]
    public void HIPB_LayerNames_StartsEmpty()
    {
        var hipb = new HIPB();
        Assert.Empty(hipb.LayerNames);
    }

    [Fact]
    public void HIPB_LayerNames_IsMutable()
    {
        var hipb = new HIPB();

        hipb.LayerNames[2] = "Boss Room";

        Assert.Equal("Boss Room", hipb.LayerNames[2]);
    }
}

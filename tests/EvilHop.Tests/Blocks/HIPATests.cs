using EvilHop.Blocks;

namespace EvilHop.Tests.Blocks;

public class HIPATests
{
    [Fact]
    public void HIPA_Tag_IsCorrect()
    {
        var hipa = new HIPA();
        Assert.Equal("HIPA", hipa.Tag);
    }
}

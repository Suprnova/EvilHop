using EvilHop.Assets;

namespace EvilHop.Tests.Assets;

public class BaseAssetTests
{
    private sealed class TestBaseAsset : BaseAsset { }

    [Fact]
    public void LinkCount_AndLinksCount_CanDisagree()
    {
        var asset = new TestBaseAsset { LinkCount = 3 };

        Assert.Empty(asset.Links);
        Assert.NotEqual(asset.LinkCount, asset.Links.Count);
    }
}

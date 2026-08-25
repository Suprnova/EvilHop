using EvilHop.Assets;
using EvilHop.Primitives;

namespace EvilHop.Tests.Assets;

public class BaseAssetTests
{
    private sealed class TestBaseAsset : BaseAsset { }

    [Fact]
    public void LinkCount_AndLinksCount_CanDisagree()
    {
        var asset = new TestBaseAsset();
        asset.Physical.LinkCount = 3;

        Assert.Empty(asset.Links);
        Assert.NotEqual(asset.Physical.LinkCount, asset.Links.Count);
    }

    [Fact]
    public void LinkCount_WhenNotOverridden_DerivesFromLinksCount()
    {
        var asset = new TestBaseAsset();
        asset.Links.Add(new Link());
        asset.Links.Add(new Link());

        Assert.Equal(2, asset.Physical.LinkCount);
    }

    [Fact]
    public void BaseId_WhenNotOverridden_FollowsId()
    {
        var asset = new TestBaseAsset { Id = new AssetId(0x1234) };

        Assert.Equal(new AssetId(0x1234), asset.Physical.BaseId);
    }

    [Fact]
    public void BaseId_WhenOverridden_DivergesFromId()
    {
        var asset = new TestBaseAsset { Id = new AssetId(0x1234) };
        asset.Physical.BaseId = new AssetId(0xDEAD);

        Assert.Equal(new AssetId(0xDEAD), asset.Physical.BaseId);
        Assert.Equal(new AssetId(0x1234), asset.Id);
    }

    [Fact]
    public void BaseId_SetToMatchingValue_KeepsFollowingId()
    {
        // A codec assigning the on-disk value unconditionally must not pin an override when the
        // file agreed - otherwise the asset stops tracking Id for every asset ever parsed.
        var asset = new TestBaseAsset { Id = new AssetId(0x1234) };
        asset.Physical.BaseId = new AssetId(0x1234);

        asset.Id = new AssetId(0x5678);

        Assert.Equal(new AssetId(0x5678), asset.Physical.BaseId);
    }

    [Fact]
    public void BaseId_WhenOverridden_StopsFollowingId()
    {
        var asset = new TestBaseAsset { Id = new AssetId(0x1234) };
        asset.Physical.BaseId = new AssetId(0xDEAD);

        asset.Id = new AssetId(0x5678);

        Assert.Equal(new AssetId(0xDEAD), asset.Physical.BaseId);
    }

    [Fact]
    public void BaseId_OverrideClearedByMatchingAssignment_FollowsIdAgain()
    {
        var asset = new TestBaseAsset { Id = new AssetId(0x1234) };
        asset.Physical.BaseId = new AssetId(0xDEAD);

        asset.Physical.BaseId = new AssetId(0x1234);
        asset.Id = new AssetId(0x5678);

        Assert.Equal(new AssetId(0x5678), asset.Physical.BaseId);
    }
}

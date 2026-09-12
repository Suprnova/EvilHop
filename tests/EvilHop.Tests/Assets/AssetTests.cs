using EvilHop.Assets;
using EvilHop.Common;

namespace EvilHop.Tests.Assets;

public class AssetTests
{
    private sealed class TestAsset(AssetType type = AssetType.Counter) : Asset(type);

    [Fact]
    public void PhysicalType_WhenNotOverridden_FollowsType()
    {
        var asset = new TestAsset(AssetType.Trigger);

        Assert.Equal(AssetType.Trigger, asset.Physical.Type);
    }

    [Fact]
    public void PhysicalType_SetToMatchingValue_KeepsFollowingType()
    {
        // Same contract as BaseId: a codec assigning the on-disk tag unconditionally must not pin
        // an override when it already agrees with the asset's own Type.
        var asset = new TestAsset(AssetType.Trigger);
        asset.Physical.Type = AssetType.Trigger;

        asset.Type = AssetType.Boulder;

        Assert.Equal(AssetType.Boulder, asset.Physical.Type);
    }

    [Fact]
    public void PhysicalType_WhenOverridden_StopsFollowingType()
    {
        var asset = new TestAsset(AssetType.Trigger);
        asset.Physical.Type = AssetType.Texture;

        asset.Type = AssetType.Boulder;

        Assert.Equal(AssetType.Texture, asset.Physical.Type);
        Assert.Equal(AssetType.Boulder, asset.Type);
    }

    [Fact]
    public void Name_DoesNotDeriveId()
    {
        // Id and Name are stored independently; roughly 2% of real assets have an Id that is not
        // the hash of the stored name, and rehashing on rename would corrupt every one of them.
        var asset = new TestAsset { Id = new AssetId(0x1234), Name = "something_else" };

        Assert.Equal(new AssetId(0x1234), asset.Id);
    }

    [Fact]
    public void CalculateId_SetsIdFromNameAndType()
    {
        var asset = new TestAsset(AssetType.Animation) { Name = "foo.dff" };

        asset.CalculateId();

        Assert.Equal(AssetId.FromName("foo.dff", AssetType.Animation), asset.Id);
    }

    public static TheoryData<Asset, AssetType> ConstructedAssets => new()
    {
        { new SurfaceAsset(), AssetType.Surface },
        { new SimpleObjectAsset(), AssetType.SimpleObject },
        { new PlatformAsset(), AssetType.Platform },
        { new EnvironmentAsset(), AssetType.Environment },
        { new LightKitAsset(), AssetType.LightKit },
        { new StaticCameraAsset(), AssetType.Camera },
        { new PlayerAsset(), AssetType.Player }
    };

    /// <summary>
    /// Every concrete asset fixes its own type through its constructor, so one built by hand is
    /// written under the right codec without the caller naming a type at all.
    /// </summary>
    [Theory]
    [MemberData(nameof(ConstructedAssets))]
    public void Type_OnAConstructedAsset_IsFixedByItsClass(Asset asset, AssetType expected) =>
        Assert.Equal(expected, asset.Type);
}

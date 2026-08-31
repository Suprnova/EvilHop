using EvilHop.Assets;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Tests.Assets;

public class AssetTests
{
    private sealed class TestAsset : Asset { }

    [Fact]
    public void PhysicalType_WhenNotOverridden_FollowsType()
    {
        var asset = new TestAsset { Type = AssetType.Trigger };

        Assert.Equal(AssetType.Trigger, asset.Physical.Type);
    }

    [Fact]
    public void PhysicalType_SetToMatchingValue_KeepsFollowingType()
    {
        // Same contract as BaseId: a codec assigning the on-disk tag unconditionally must not pin
        // an override when it already agrees with the asset's own Type.
        var asset = new TestAsset { Type = AssetType.Trigger };
        asset.Physical.Type = AssetType.Trigger;

        asset.Type = AssetType.Boulder;

        Assert.Equal(AssetType.Boulder, asset.Physical.Type);
    }

    [Fact]
    public void PhysicalType_WhenOverridden_StopsFollowingType()
    {
        var asset = new TestAsset { Type = AssetType.Trigger };
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
        var asset = new TestAsset { Name = "foo.dff", Type = AssetType.Animation };

        asset.CalculateId();

        Assert.Equal(AssetId.FromName("foo.dff", AssetType.Animation), asset.Id);
    }

    [Fact]
    public void Validate_ReturnsEmpty()
    {
        var asset = new TestAsset();
        var context = new ValidationContext(
            new FormatProfile(GameVersion.BFBB, Platform.GameCube, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: false));

        var issues = asset.Validate(context);

        Assert.Empty(issues);
    }
}

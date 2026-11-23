using EvilHop.Assets;
using EvilHop.Common;

namespace EvilHop.Tests.Assets;

public class AssetSupportTests
{
    [Theory]
    [InlineData(AssetType.Counter, GameVersion.TSSM, SupportState.Typed)]
    [InlineData(AssetType.Texture, GameVersion.TSSM, SupportState.Payload)]
    [InlineData(AssetType.PickupTypes, GameVersion.Incredibles, SupportState.Typed)]
    [InlineData(AssetType.PickupTypes, GameVersion.TSSM, SupportState.Untyped)]
    [InlineData(AssetType.Dynamic, GameVersion.TSSM, SupportState.Untyped)]
    public void Of_Type_IsWhatAReadUnderTheGameResolvesTo(AssetType type, GameVersion game, SupportState expected) =>
        Assert.Equal(expected, AssetSupport.Of(type, game));

    [Theory]
    [InlineData(DynamicKind.TalkBox, GameVersion.TSSM, 11, SupportState.Typed)]
    [InlineData(DynamicKind.TalkBox, GameVersion.TSSM, 10, SupportState.Untyped)]
    [InlineData(DynamicKind.TalkBox, GameVersion.ROTU, 11, SupportState.Untyped)]
    [InlineData(DynamicKind.TaskBox, GameVersion.TSSM, 2, SupportState.Untyped)]
    public void Of_Kind_IsTypedOnlyForAModeledLayout(DynamicKind kind, GameVersion game, short version, SupportState expected) =>
        Assert.Equal(expected, AssetSupport.Of(kind, game, version));

    [Fact]
    public void Of_TypeWithNoCodecOrShape_IsUntyped() =>
        Assert.Equal(SupportState.Untyped, AssetSupport.Of((AssetType)0x4E4F4E45, GameVersion.TSSM)); // "NONE"

    [Fact]
    public void Of_UnknownKind_IsUntyped() =>
        Assert.Equal(SupportState.Untyped, AssetSupport.Of((DynamicKind)0x12345678, GameVersion.TSSM, 1));
}

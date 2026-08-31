using EvilHop.Common;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Tests.Validation;

public class ValidationAttributeTests
{
    private static ValidationContext ContextFor(GameVersion game, Platform platform = Platform.GameCube) =>
        new(new FormatProfile(game, platform, PlatformFieldOrder.LanguageRegion, StreamDataHasPaddingField: true));

    [Fact]
    public void Matches_GamesEmpty_MatchesEveryGame()
    {
        var attribute = new ConstantValueAttribute(0u);

        Assert.True(attribute.Matches(ContextFor(GameVersion.N100F)));
        Assert.True(attribute.Matches(ContextFor(GameVersion.Ratatouille)));
    }

    [Fact]
    public void Matches_GamesNonEmpty_ContextGameIncluded_ReturnsTrue()
    {
        var attribute = new ConstantValueAttribute(0u) { Games = [GameVersion.BFBB, GameVersion.TSSM] };

        Assert.True(attribute.Matches(ContextFor(GameVersion.TSSM)));
    }

    [Fact]
    public void Matches_GamesNonEmpty_ContextGameExcluded_ReturnsFalse()
    {
        var attribute = new ConstantValueAttribute(0u) { Games = [GameVersion.BFBB, GameVersion.TSSM] };

        Assert.False(attribute.Matches(ContextFor(GameVersion.N100F)));
    }

    [Fact]
    public void Matches_GameWithinFromToRange_ReturnsTrue()
    {
        var attribute = new ConstantValueAttribute(0u) { From = GameVersion.BFBB, To = GameVersion.ROTU };

        Assert.True(attribute.Matches(ContextFor(GameVersion.Incredibles)));
    }

    [Fact]
    public void Matches_GameBelowFromToRange_ReturnsFalse()
    {
        var attribute = new ConstantValueAttribute(0u) { From = GameVersion.BFBB, To = GameVersion.ROTU };

        Assert.False(attribute.Matches(ContextFor(GameVersion.N100F)));
    }

    [Fact]
    public void Matches_GameAboveFromToRange_ReturnsFalse()
    {
        var attribute = new ConstantValueAttribute(0u) { From = GameVersion.BFBB, To = GameVersion.ROTU };

        Assert.False(attribute.Matches(ContextFor(GameVersion.Ratatouille)));
    }

    [Fact]
    public void Matches_PlatformsEmpty_MatchesEveryPlatform()
    {
        var attribute = new ConstantValueAttribute(0u);

        Assert.True(attribute.Matches(ContextFor(GameVersion.BFBB, Platform.Xbox)));
        Assert.True(attribute.Matches(ContextFor(GameVersion.BFBB, Platform.PlayStation2)));
    }

    [Fact]
    public void Matches_PlatformsNonEmpty_ContextPlatformIncluded_ReturnsTrue()
    {
        var attribute = new ConstantValueAttribute(0u) { Platforms = [Platform.Xbox, Platform.PlayStation2] };

        Assert.True(attribute.Matches(ContextFor(GameVersion.BFBB, Platform.PlayStation2)));
    }

    [Fact]
    public void Matches_PlatformsNonEmpty_ContextPlatformExcluded_ReturnsFalse()
    {
        var attribute = new ConstantValueAttribute(0u) { Platforms = [Platform.Xbox, Platform.PlayStation2] };

        Assert.False(attribute.Matches(ContextFor(GameVersion.BFBB, Platform.GameCube)));
    }

    [Fact]
    public void Matches_EveryAxisMatches_ReturnsTrue()
    {
        var attribute = new ConstantValueAttribute(0u)
        {
            Games = [GameVersion.BFBB],
            From = GameVersion.N100F,
            To = GameVersion.Ratatouille,
            Platforms = [Platform.GameCube]
        };

        Assert.True(attribute.Matches(ContextFor(GameVersion.BFBB, Platform.GameCube)));
    }

    [Fact]
    public void Matches_OneAxisFails_ReturnsFalse()
    {
        var attribute = new ConstantValueAttribute(0u)
        {
            Games = [GameVersion.BFBB],
            Platforms = [Platform.Xbox]
        };

        Assert.False(attribute.Matches(ContextFor(GameVersion.BFBB, Platform.GameCube)));
    }
}

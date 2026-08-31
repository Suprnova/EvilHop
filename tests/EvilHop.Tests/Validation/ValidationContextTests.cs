using EvilHop.Common;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Tests.Validation;

public class ValidationContextTests
{
    [Fact]
    public void Game_ReturnsProfilesGame()
    {
        var profile = new FormatProfile(GameVersion.Incredibles, Platform.Xbox, PlatformFieldOrder.LanguageRegion, StreamDataHasPaddingField: true);
        var context = new ValidationContext(profile);

        Assert.Equal(GameVersion.Incredibles, context.Game);
    }

    [Fact]
    public void Platform_ReturnsProfilesPlatform()
    {
        var profile = new FormatProfile(GameVersion.Incredibles, Platform.Xbox, PlatformFieldOrder.LanguageRegion, StreamDataHasPaddingField: true);
        var context = new ValidationContext(profile);

        Assert.Equal(Platform.Xbox, context.Platform);
    }

    [Fact]
    public void Origin_NotProvided_DefaultsToUnknown()
    {
        var context = new ValidationContext(N100FSerializer.DefaultProfile);

        Assert.Equal(ArchiveOrigin.Unknown, context.Origin);
    }

    [Fact]
    public void Role_NotProvided_DefaultsToUnknown()
    {
        var context = new ValidationContext(N100FSerializer.DefaultProfile);

        Assert.Equal(ArchiveRole.Unknown, context.Role);
    }

    [Fact]
    public void BuildId_NotProvided_DefaultsToNull()
    {
        var context = new ValidationContext(N100FSerializer.DefaultProfile);

        Assert.Null(context.BuildId);
    }
}

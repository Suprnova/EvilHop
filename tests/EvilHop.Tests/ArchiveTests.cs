using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests;

public class ArchiveTests
{
    private static FileStream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "n100f", "minimal.hip"));

    [Fact]
    public void Load_ThenSave_MinimalFixture_ProducesIdenticalBytes()
    {
        using var fixture = OpenMinimalFixture();
        using var fixtureCopy = new MemoryStream();
        fixture.CopyTo(fixtureCopy);
        byte[] originalBytes = fixtureCopy.ToArray();

        var archive = Archive.Load(new MemoryStream(originalBytes), new N100FSerializer());

        using var rewritten = new MemoryStream();
        archive.Save(rewritten);

        Assert.Equal(originalBytes, rewritten.ToArray());
    }

    private static byte[] MinimalFixtureBytes()
    {
        using var fixture = OpenMinimalFixture();
        using var copy = new MemoryStream();
        fixture.CopyTo(copy);
        return copy.ToArray();
    }

    [Fact]
    public void Load_GameOnly_ThenSave_MinimalFixture_ProducesIdenticalBytes()
    {
        byte[] originalBytes = MinimalFixtureBytes();

        var archive = Archive.Load(new MemoryStream(originalBytes), GameVersion.N100F);

        using var rewritten = new MemoryStream();
        archive.Save(rewritten);
        Assert.Equal(originalBytes, rewritten.ToArray());
    }

    [Fact]
    public void Load_GameOnly_StatesNoPlatform()
    {
        var archive = Archive.Load(new MemoryStream(MinimalFixtureBytes()), GameVersion.N100F);

        Assert.Equal(GameVersion.N100F, archive.Serializer.Profile.Game);
        Assert.Null(archive.Serializer.Profile.Platform);
    }

    [Theory]
    [InlineData(Platform.GameCube)]
    [InlineData(Platform.Xbox)]
    [InlineData(Platform.PlayStation2)]
    public void Load_GameAndPlatform_StatesBoth(Platform platform)
    {
        var archive = Archive.Load(new MemoryStream(MinimalFixtureBytes()), GameVersion.N100F, platform);

        Assert.Equal(GameVersion.N100F, archive.Serializer.Profile.Game);
        Assert.Equal(platform, archive.Serializer.Profile.Platform);
    }
}

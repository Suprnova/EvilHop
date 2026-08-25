using EvilHop.Serialization;

namespace EvilHop.Tests;

public class ArchiveTests
{
    private static Stream OpenMinimalFixture() =>
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
}

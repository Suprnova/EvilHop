namespace EvilHop.Corpus.Tests;

public class ArchiveWalkerTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("EvilHop.Corpus.Tests.").FullName;

    public void Dispose()
    {
        Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private string Game(string name)
    {
        var gamePath = Path.Combine(root, name);
        Directory.CreateDirectory(gamePath);
        return gamePath;
    }

    private static void TouchFile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, []);
    }

    [Fact]
    public void Discover_FourSegmentPath_DerivesBuildKeyFromGameAndAllSegments()
    {
        var game = Game("n100f");
        TouchFile(Path.Combine(game, "release", "GC", "NTSC-U", "US", "boot.HIP"));

        var archive = Assert.Single(ArchiveWalker.Discover([game]));

        Assert.Equal("n100f/release/GC/NTSC-U/US", archive.BuildKey);
        Assert.Equal("n100f/release/GC/NTSC-U/US/boot.HIP", archive.RelativePath);
    }

    [Fact]
    public void Discover_DeeperThanFourSegments_FoldsExtraSegmentsIntoBuildKeyAncestor()
    {
        var game = Game("n100f");
        TouchFile(Path.Combine(game, "release", "GC", "NTSC-U", "US", "B0", "b001.HIP"));

        var archive = Assert.Single(ArchiveWalker.Discover([game]));

        Assert.Equal("n100f/release/GC/NTSC-U/US", archive.BuildKey);
        Assert.Equal("n100f/release/GC/NTSC-U/US/B0/b001.HIP", archive.RelativePath);
    }

    [Fact]
    public void Discover_ShallowerThanFourSegments_UsesWhateverSegmentsExist()
    {
        var game = Game("n100f");
        TouchFile(Path.Combine(game, "prototype_2001-01-31", "boot.HIP"));

        var archive = Assert.Single(ArchiveWalker.Discover([game]));

        Assert.Equal("n100f/prototype_2001-01-31", archive.BuildKey);
        Assert.Equal("n100f/prototype_2001-01-31/boot.HIP", archive.RelativePath);
    }

    [Fact]
    public void Discover_FileDirectlyInRoot_UsesGameNameAsBuildKey()
    {
        var game = Game("n100f");
        TouchFile(Path.Combine(game, "boot.HIP"));

        var archive = Assert.Single(ArchiveWalker.Discover([game]));

        Assert.Equal("n100f", archive.BuildKey);
        Assert.Equal("n100f/boot.HIP", archive.RelativePath);
    }

    [Theory]
    [InlineData("boot.HIP")]
    [InlineData("boot.hip")]
    [InlineData("boot.HOP")]
    [InlineData("boot.hop")]
    public void Discover_ArchiveExtension_IsCaseInsensitive(string fileName)
    {
        var game = Game("n100f");
        TouchFile(Path.Combine(game, fileName));

        var archive = Assert.Single(ArchiveWalker.Discover([game]));

        Assert.Equal(fileName, Path.GetFileName(archive.FullPath));
    }

    [Fact]
    public void Discover_IgnoresNonArchiveFiles()
    {
        var game = Game("n100f");
        TouchFile(Path.Combine(game, "boot.HIP"));
        TouchFile(Path.Combine(game, "readme.txt"));

        var archives = ArchiveWalker.Discover([game]).ToList();

        Assert.Single(archives);
    }

    [Fact]
    public void Discover_MultipleRoots_DiscoversArchivesFromEach()
    {
        var n100f = Game("n100f");
        var bfbb = Game("bfbb");
        TouchFile(Path.Combine(n100f, "boot.HIP"));
        TouchFile(Path.Combine(bfbb, "boot.HIP"));

        var archives = ArchiveWalker.Discover([n100f, bfbb]).ToList();

        Assert.Equal(["bfbb", "n100f"], archives.Select(a => a.BuildKey).OrderBy(k => k));
    }

    [Fact]
    public void Discover_MissingRoot_ThrowsDirectoryNotFoundException()
    {
        var missing = Path.Combine(root, "does-not-exist");

        Assert.Throws<DirectoryNotFoundException>(() => ArchiveWalker.Discover([missing]).ToList());
    }

    [Fact]
    public void Discover_RootWithNoArchives_ThrowsInvalidOperationException()
    {
        var game = Game("n100f");

        Assert.Throws<InvalidOperationException>(() => ArchiveWalker.Discover([game]).ToList());
    }
}

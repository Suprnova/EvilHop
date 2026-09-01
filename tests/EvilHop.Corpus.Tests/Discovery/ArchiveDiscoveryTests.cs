using EvilHop.Corpus.Discovery;
using EvilHop.Validation;

namespace EvilHop.Corpus.Tests.Discovery;

public class ArchiveDiscoveryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"evilhop-corpus-tests-{Guid.NewGuid()}");

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private string Build(params string[] files)
    {
        string buildDirectory = Path.Combine(root, "build");
        Directory.CreateDirectory(buildDirectory);
        foreach (string file in files) File.WriteAllText(Path.Combine(buildDirectory, file), "");
        return "build";
    }

    [Fact]
    public void Find_HipWithNoSiblings_IsLevel()
    {
        string build = Build("hb01.HIP");

        var found = Assert.Single(ArchiveDiscovery.Find(root, build));

        Assert.Equal(ArchiveRole.Level, found.Role);
        Assert.Null(found.PairGroup);
    }

    [Fact]
    public void Find_HopWithMatchingHip_IsPaired()
    {
        string build = Build("b101.HIP", "b101.HOP");

        var hop = ArchiveDiscovery.Find(root, build).Single(a => a.RelativePath.EndsWith(".HOP"));

        Assert.Equal(ArchiveRole.Paired, hop.Role);
        Assert.Equal("build/b101", hop.PairGroup);
    }

    [Fact]
    public void Find_HopWithoutMatchingHip_IsLevel()
    {
        string build = Build("b101.HOP");

        var found = Assert.Single(ArchiveDiscovery.Find(root, build));

        Assert.Equal(ArchiveRole.Level, found.Role);
    }

    [Fact]
    public void Find_LocalizedSuffixWithMatchingBase_IsLocalized()
    {
        string build = Build("font.HIP", "font_DE.HIP");

        var localized = ArchiveDiscovery.Find(root, build).Single(a => a.RelativePath.EndsWith("font_DE.HIP"));

        Assert.Equal(ArchiveRole.Localized, localized.Role);
        Assert.Equal("DE", localized.Language);
        Assert.Equal("build/font", localized.PairGroup);
    }

    [Fact]
    public void Find_LocalizedLookingSuffixWithoutMatchingBase_IsLevel()
    {
        string build = Build("foo_de.HIP");

        var found = Assert.Single(ArchiveDiscovery.Find(root, build));

        Assert.Equal(ArchiveRole.Level, found.Role);
        Assert.Null(found.Language);
    }

    [Fact]
    public void Find_NonArchiveFiles_AreIgnored()
    {
        string build = Build("hb01.HIP", "hb01.sdf", "hb01.sdf.log", "readme.txt");

        var found = ArchiveDiscovery.Find(root, build);

        Assert.Equal(["hb01.HIP"], found.Select(a => Path.GetFileName(a.RelativePath)));
    }

    [Fact]
    public void Find_ArchivesInSubdirectories_AreFoundRecursively()
    {
        string buildDirectory = Path.Combine(root, "build");
        Directory.CreateDirectory(Path.Combine(buildDirectory, "hb"));
        File.WriteAllText(Path.Combine(buildDirectory, "hb", "hb01.HIP"), "");

        var found = Assert.Single(ArchiveDiscovery.Find(root, "build"));

        Assert.Equal("build/hb/hb01.HIP", found.RelativePath);
    }

    [Fact]
    public void Find_MissingBuildDirectory_ReturnsEmpty()
    {
        var found = ArchiveDiscovery.Find(root, "does-not-exist");

        Assert.Empty(found);
    }
}

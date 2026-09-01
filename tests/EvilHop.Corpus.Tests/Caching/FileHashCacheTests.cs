using EvilHop.Corpus.Caching;

namespace EvilHop.Corpus.Tests.Caching;

public class FileHashCacheTests : IDisposable
{
    private static readonly DateTime OriginalTimestamp = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LaterTimestamp = new(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc);

    private readonly string directory = Path.Combine(Path.GetTempPath(), $"evilhop-corpus-tests-{Guid.NewGuid()}");
    private readonly string filePath;
    private readonly string cachePath;

    public FileHashCacheTests()
    {
        Directory.CreateDirectory(directory);
        filePath = Path.Combine(directory, "archive.hip");
        cachePath = Path.Combine(directory, "file-hashes.json");
        WriteFile("original content", OriginalTimestamp);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private void WriteFile(string content, DateTime lastWriteTimeUtc)
    {
        File.WriteAllText(filePath, content);
        File.SetLastWriteTimeUtc(filePath, lastWriteTimeUtc);
    }

    [Fact]
    public void GetOrCompute_NewFile_ReturnsItsHash()
    {
        var cache = new FileHashCache(cachePath);

        string sha256 = cache.GetOrCompute(filePath);

        Assert.Equal(7, sha256.Length);
    }

    [Fact]
    public void GetOrCompute_SameSizeAndTimestampButDifferentContent_ReturnsTheStaleCachedHash()
    {
        var cache = new FileHashCache(cachePath);
        string original = cache.GetOrCompute(filePath);

        WriteFile("changedcontent!!", OriginalTimestamp); // same length, same timestamp, different bytes

        string second = cache.GetOrCompute(filePath);

        Assert.Equal(original, second);
    }

    [Fact]
    public void GetOrCompute_DifferentLastWriteTime_RecomputesTheHash()
    {
        var cache = new FileHashCache(cachePath);
        string original = cache.GetOrCompute(filePath);

        WriteFile("different content", LaterTimestamp);

        string second = cache.GetOrCompute(filePath);

        Assert.NotEqual(original, second);
    }

    [Fact]
    public void GetOrCompute_DifferentSize_RecomputesTheHash()
    {
        var cache = new FileHashCache(cachePath);
        string original = cache.GetOrCompute(filePath);

        WriteFile("much longer content than before", OriginalTimestamp);

        string second = cache.GetOrCompute(filePath);

        Assert.NotEqual(original, second);
    }

    [Fact]
    public void Save_ThenReloaded_ReusesTheCachedHashAcrossInstances()
    {
        var first = new FileHashCache(cachePath);
        string original = first.GetOrCompute(filePath);
        first.Save();

        WriteFile("changedcontent!!", OriginalTimestamp); // same length, same timestamp, different bytes
        var second = new FileHashCache(cachePath);

        Assert.Equal(original, second.GetOrCompute(filePath));
    }

    [Fact]
    public void Save_NothingComputed_DoesNotCreateAFile()
    {
        var cache = new FileHashCache(cachePath);

        cache.Save();

        Assert.False(File.Exists(cachePath));
    }
}

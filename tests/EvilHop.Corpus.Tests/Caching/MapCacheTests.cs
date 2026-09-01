using EvilHop.Corpus.Caching;

namespace EvilHop.Corpus.Tests.Caching;

public class MapCacheTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"evilhop-corpus-tests-{Guid.NewGuid()}");
    private readonly MapCache cache;

    public MapCacheTests() => cache = new MapCache(directory);

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void TryGet_NothingCached_ReturnsFalse()
    {
        bool found = cache.TryGet("blockFields", "aaa111", "bbb222", out string? json);

        Assert.False(found);
        Assert.Null(json);
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsTheStoredJson()
    {
        cache.Set("blockFields", "aaa111", "bbb222", """{"PVER.subVersion":[2]}""");

        bool found = cache.TryGet("blockFields", "aaa111", "bbb222", out string? json);

        Assert.True(found);
        Assert.Equal("""{"PVER.subVersion":[2]}""", json);
    }

    [Fact]
    public void TryGet_DifferentArchiveHash_ReturnsFalse()
    {
        cache.Set("blockFields", "aaa111", "bbb222", "{}");

        bool found = cache.TryGet("blockFields", "ccc333", "bbb222", out _);

        Assert.False(found);
    }

    [Fact]
    public void TryGet_DifferentInputFingerprint_ReturnsFalse()
    {
        cache.Set("blockFields", "aaa111", "bbb222", "{}");

        bool found = cache.TryGet("blockFields", "aaa111", "ddd444", out _);

        Assert.False(found);
    }

    [Fact]
    public void TryGet_DifferentFacetId_ReturnsFalse()
    {
        cache.Set("blockFields", "aaa111", "bbb222", "{}");

        bool found = cache.TryGet("assetFields", "aaa111", "bbb222", out _);

        Assert.False(found);
    }

    [Fact]
    public void Set_CalledTwiceForTheSameKey_OverwritesTheStoredJson()
    {
        cache.Set("blockFields", "aaa111", "bbb222", "{}");
        cache.Set("blockFields", "aaa111", "bbb222", """{"PVER.subVersion":[2]}""");

        cache.TryGet("blockFields", "aaa111", "bbb222", out string? json);

        Assert.Equal("""{"PVER.subVersion":[2]}""", json);
    }
}

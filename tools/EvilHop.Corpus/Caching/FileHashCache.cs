using System.Security.Cryptography;
using System.Text.Json;

namespace EvilHop.Corpus.Caching;

/// <summary>
/// A gitignored, on-disk cache mapping a file's path to the sha256 it produced the last time it was
/// read, keyed by size and last-write time. <c>artifacts/</c> is large and its files essentially
/// never change once captured, so re-hashing an unchanged file on every run - just to find out its
/// <see cref="MapCache"/> key is what it already was - would dominate every run's cost for no
/// reason. A stat is enough to confirm the cached hash is still valid; only a changed or previously
/// unseen file is actually read.
/// </summary>
/// <param name="path">The file this cache's entries are persisted to.</param>
public sealed class FileHashCache(string path)
{
    private readonly Dictionary<string, Entry> _entries = File.Exists(path)
        ? JsonSerializer.Deserialize<Dictionary<string, Entry>>(File.ReadAllText(path)) ?? []
        : [];

    private bool _dirty;

    private readonly record struct Entry(long Size, DateTime LastWriteTimeUtc, string Sha256);

    /// <summary>
    /// Returns <paramref name="filePath"/>'s sha256, from the cache if its size and last-write time
    /// still match what produced the cached hash, otherwise by reading and hashing it.
    /// </summary>
    /// <param name="filePath">The file to hash.</param>
    /// <returns>The first 7 hex characters of the file's SHA-256.</returns>
    public string GetOrCompute(string filePath)
    {
        var info = new FileInfo(filePath);
        string key = info.FullName;

        if (_entries.TryGetValue(key, out var cached) &&
            cached.Size == info.Length && cached.LastWriteTimeUtc == info.LastWriteTimeUtc)
            return cached.Sha256;

        using var stream = File.OpenRead(filePath);
        string sha256 = Convert.ToHexStringLower(SHA256.HashData(stream))[..7];

        _entries[key] = new Entry(info.Length, info.LastWriteTimeUtc, sha256);
        _dirty = true;
        return sha256;
    }

    /// <summary>
    /// Persists every entry computed or reused this run, if any were computed fresh. A no-op when
    /// every lookup was a cache hit.
    /// </summary>
    public void Save()
    {
        if (!_dirty) return;

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, JsonSerializer.Serialize(_entries));
    }
}

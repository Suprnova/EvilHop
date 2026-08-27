namespace EvilHop.Corpus.Archives;

/// <summary>
/// A single archive file discovered under a corpus root.
/// </summary>
/// <param name="FullPath">The absolute filesystem path.</param>
/// <param name="BuildKey">
/// The corpus-relative build key, e.g. <c>n100f/release/GC/NTSC-U/US</c> - the given root's own
/// directory name (<c>game</c>) followed by up to <c>build/platform/region/language</c>. Deeper
/// directories (e.g. per-level subfolders) fold into their nearest build-key ancestor rather than
/// each becoming their own build.
/// </param>
/// <param name="RelativePath">
/// The full corpus-relative path, e.g. <c>n100f/release/GC/NTSC-U/US/B0/b001.HIP</c>. Used as the
/// exemplar path for observations traced back to this file - always a real, loadable path, even
/// when it is deeper than <see cref="BuildKey"/>.
/// </param>
internal sealed record DiscoveredArchive(string FullPath, string BuildKey, string RelativePath);

/// <summary>
/// Recursively discovers <c>*.HIP</c>/<c>*.HOP</c> archives under one or more corpus roots and
/// derives their build attribution. Streams results - the corpus is too large to hold in memory.
/// </summary>
internal static class ArchiveWalker
{
    private static readonly string[] ArchiveExtensions = [".hip", ".hop"];

    /// <summary>The number of directory segments below <c>game</c> that make up a build key: <c>build/platform/region/language</c>.</summary>
    private const int MaxBuildKeyDepth = 4;

    /// <summary>
    /// Discovers every archive under <paramref name="roots"/>, in a deterministic order.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">Thrown when a root does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a root contains no archives.</exception>
    public static IEnumerable<DiscoveredArchive> Discover(IEnumerable<string> roots)
    {
        foreach (var root in roots)
        {
            var fullRoot = Path.GetFullPath(root);
            if (!Directory.Exists(fullRoot))
                throw new DirectoryNotFoundException($"Corpus root '{root}' does not exist.");

            string gameName = new DirectoryInfo(fullRoot).Name;

            var files = Directory.EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories)
                .Where(file => ArchiveExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (files.Count == 0)
                throw new InvalidOperationException($"Corpus root '{root}' contains no .HIP or .HOP files.");

            foreach (var file in files)
                yield return Describe(fullRoot, gameName, file);
        }
    }

    private static DiscoveredArchive Describe(string fullRoot, string gameName, string file)
    {
        var relativeDir = Path.GetRelativePath(fullRoot, Path.GetDirectoryName(file)!);
        string[] segments = relativeDir == "."
            ? []
            : relativeDir.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);

        string buildKey = string.Join('/', [gameName, .. segments.Take(MaxBuildKeyDepth)]);
        string relativePath = string.Join('/', [gameName, .. segments, Path.GetFileName(file)]);
        return new DiscoveredArchive(file, buildKey, relativePath);
    }
}

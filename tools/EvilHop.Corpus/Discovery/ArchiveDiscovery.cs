using EvilHop.Validation;
using System.Text.RegularExpressions;

namespace EvilHop.Corpus.Discovery;

/// <summary>
/// An archive found on disk, with the role its filename implies under universal convention: a
/// <c>.HOP</c> is <see cref="ArchiveRole.Paired"/> with the <c>.HIP</c> of the same base name, and
/// <c>&lt;name&gt;_XX.HIP</c> is <see cref="ArchiveRole.Localized"/>, joining <c>&lt;name&gt;</c>'s
/// pair group.
/// </summary>
/// <param name="FullPath">The archive's absolute path on disk.</param>
/// <param name="RelativePath">The archive's path relative to the artifact root, forward-slashed.</param>
/// <param name="Role">The role the filename implies.</param>
/// <param name="Language">The language code, when <see cref="Role"/> is <see cref="ArchiveRole.Localized"/>.</param>
/// <param name="PairGroup">
/// The shared base path joining this archive to its <c>.HIP</c>/<c>.HOP</c>/localized siblings, when
/// it has any.
/// </param>
public sealed partial record DiscoveredArchive(
    string FullPath, string RelativePath, ArchiveRole Role, string? Language = null, string? PairGroup = null);

/// <summary>
/// Walks a build's directory for the archives it contains, classifying each by filename convention
/// alone. Only <c>.HIP</c> and <c>.HOP</c> files are archives; everything else a real build directory
/// carries (loose assets, dev tooling, other project files) is not.
/// </summary>
public static partial class ArchiveDiscovery
{
    [GeneratedRegex(@"^(?<base>.+)_(?<language>[A-Za-z]{2})$")]
    private static partial Regex LocalizedSuffix();

    /// <summary>
    /// Finds every archive under <paramref name="buildDirectory"/>, recursively.
    /// </summary>
    /// <param name="artifactRoot">The artifact root every relative path is resolved against.</param>
    /// <param name="buildDirectory">The build's directory, relative to <paramref name="artifactRoot"/>.</param>
    /// <returns>Every discovered archive, classified.</returns>
    public static IEnumerable<DiscoveredArchive> Find(string artifactRoot, string buildDirectory)
    {
        string root = Path.Combine(artifactRoot, buildDirectory);
        if (!Directory.Exists(root)) yield break;

        var byDirectory = Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".hip", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".hop", StringComparison.OrdinalIgnoreCase))
            .GroupBy(Path.GetDirectoryName!);

        foreach (var siblings in byDirectory)
        {
            var hipBasenames = siblings
                .Where(path => path.EndsWith(".hip", StringComparison.OrdinalIgnoreCase))
                .Select(path => Path.GetFileNameWithoutExtension(path)!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string path in siblings)
                yield return Classify(path, artifactRoot, hipBasenames);
        }
    }

    private static DiscoveredArchive Classify(string path, string artifactRoot, HashSet<string> hipBasenames)
    {
        string relativePath = Path.GetRelativePath(artifactRoot, path).Replace('\\', '/');
        string directory = Path.GetDirectoryName(relativePath)!.Replace('\\', '/');
        string basename = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);

        if (extension.Equals(".hop", StringComparison.OrdinalIgnoreCase) && hipBasenames.Contains(basename))
            return new DiscoveredArchive(path, relativePath, ArchiveRole.Paired, PairGroup: $"{directory}/{basename}");

        var match = LocalizedSuffix().Match(basename);
        if (match.Success && hipBasenames.Contains(match.Groups["base"].Value))
        {
            string baseName = match.Groups["base"].Value;
            return new DiscoveredArchive(
                path, relativePath, ArchiveRole.Localized,
                Language: match.Groups["language"].Value.ToUpperInvariant(),
                PairGroup: $"{directory}/{baseName}");
        }

        return new DiscoveredArchive(path, relativePath, ArchiveRole.Level);
    }
}

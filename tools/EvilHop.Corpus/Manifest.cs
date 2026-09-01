using EvilHop.Common;
using EvilHop.Serialization;
using EvilHop.Validation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvilHop.Corpus;

/// <summary>
/// The hand-authored, committed statement of what the corpus is: build directories, the always-loaded
/// hypothesis for each, conditionally-loaded cohorts, and per-archive overrides. Covers only what
/// filename convention can't supply - everything else about an archive is worked out by the tool
/// itself.
/// </summary>
/// <param name="Schema">The manifest format's schema version.</param>
/// <param name="DefaultGlobals">
/// The archives hypothesized to be loaded by every other archive in every build, matched by filename
/// case-insensitively. A build's own <see cref="ManifestBuild.Globals"/> adds to this rather than
/// replacing it, so the common case - every build sharing the same handful of shared archives - never
/// needs repeating per build.
/// </param>
/// <param name="Builds">The declared builds, each a directory of archives sharing one profile.</param>
/// <param name="Cohorts">Archives loaded together by a subset of levels, but not universally.</param>
/// <param name="Overrides">Per-archive pins that convention and sniffing can't (yet) supply.</param>
public sealed record Manifest(
    int Schema,
    IReadOnlyList<string>? DefaultGlobals = null,
    IReadOnlyList<ManifestBuild>? Builds = null,
    IReadOnlyList<ManifestCohort>? Cohorts = null,
    IReadOnlyList<ManifestOverride>? Overrides = null)
{
    /// <inheritdoc cref="DefaultGlobals" />
    public IReadOnlyList<string> DefaultGlobals { get; init; } = DefaultGlobals ?? [];

    /// <inheritdoc cref="Builds" />
    public IReadOnlyList<ManifestBuild> Builds { get; init; } = Builds ?? [];

    /// <inheritdoc cref="Cohorts" />
    public IReadOnlyList<ManifestCohort> Cohorts { get; init; } = Cohorts ?? [];

    /// <inheritdoc cref="Overrides" />
    public IReadOnlyList<ManifestOverride> Overrides { get; init; } = Overrides ?? [];

    /// <summary>An empty manifest, declaring no builds, cohorts, or overrides.</summary>
    public static Manifest Empty { get; } = new(Schema: 1);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Loads and parses the manifest at <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path to the manifest JSON file.</param>
    /// <returns>The parsed <see cref="Manifest"/>.</returns>
    public static Manifest Load(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<Manifest>(stream, JsonOptions)
            ?? throw new InvalidDataException($"'{path}' does not contain a manifest.");
    }
}

/// <summary>
/// One build: a directory of archives sharing a single profile, plus any always-loaded archives
/// beyond <see cref="Manifest.DefaultGlobals"/>.
/// </summary>
/// <param name="Id">The build's identifier, such as <c>"bfbb-gc-ntsc-release"</c>.</param>
/// <param name="Directory">The build's directory, relative to the artifact root.</param>
/// <param name="Globals">
/// Archives hypothesized to be loaded by every other archive in this build, on top of
/// <see cref="Manifest.DefaultGlobals"/> - for a build that genuinely carries something beyond the
/// common set. One-way: these can't see each other's assets, only their own.
/// </param>
public sealed record ManifestBuild(string Id, string Directory, IReadOnlyList<string>? Globals = null)
{
    /// <inheritdoc cref="Globals" />
    public IReadOnlyList<string> Globals { get; init; } = Globals ?? [];
}

/// <summary>
/// A manifest-declared group of archives loaded together by a subset of levels, but not universally.
/// </summary>
/// <param name="Id">The cohort's identifier.</param>
/// <param name="Archive">The archive that anchors the cohort, such as <c>"PL01.HIP"</c>.</param>
/// <param name="Members">
/// Filename glob patterns matching the archives that load <paramref name="Archive"/>, and which it
/// therefore sees in return.
/// </param>
public sealed record ManifestCohort(string Id, string Archive, IReadOnlyList<string>? Members = null)
{
    /// <inheritdoc cref="Members" />
    public IReadOnlyList<string> Members { get; init; } = Members ?? [];
}

/// <summary>
/// A pin on a specific archive that convention and format sniffing can't yet supply.
/// </summary>
/// <param name="Path">The path to the archive being overridden, relative to the artifact root.</param>
/// <param name="Game">
/// The game to read the archive's serializer for, if not what its directory implies. Affects only
/// how the archive is parsed - which inventory it's grouped into is always the directory-implied
/// game, so a format-compatible oddity like BFBB's <c>font2.HIP</c> doesn't leak into another game's
/// corpus.
/// </param>
/// <param name="Role">The archive's role, if not what its filename implies.</param>
/// <param name="Quirks">
/// <see cref="FormatQuirks"/> this archive's build carries beyond what its game and platform imply.
/// </param>
/// <param name="Note">Why this override exists.</param>
public sealed record ManifestOverride(
    string Path, GameVersion? Game = null, ArchiveRole? Role = null, FormatQuirks? Quirks = null, string? Note = null);

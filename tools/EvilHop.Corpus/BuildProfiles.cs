using EvilHop.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvilHop.Corpus;

internal sealed record ProfileOverride(bool? StreamDataHasPaddingField, PlatformFieldOrder? PlatformFieldOrder, bool? EntityHasPadding)
{
    public FormatProfile ApplyTo(FormatProfile profile) => profile with
    {
        StreamDataHasPaddingField = StreamDataHasPaddingField ?? profile.StreamDataHasPaddingField,
        PlatformFieldOrder = PlatformFieldOrder ?? profile.PlatformFieldOrder,
        EntityHasPadding = EntityHasPadding ?? profile.EntityHasPadding
    };
}

internal sealed record BuildProfileOverride(string PathPrefix, ProfileOverride Profile);

/// <summary>
/// Per-archive <see cref="FormatProfile"/> quirk overrides, keyed by a <see cref="DiscoveredArchive.RelativePath"/>
/// prefix. Committed at <c>tools/EvilHop.Corpus/BuildProfiles.json</c> - the corpus tool's own record
/// of builds whose bytes don't match their game's default profile, kept here rather than in
/// <c>artifacts/</c>, which is gitignored and rebuilt per contributor and so cannot carry a finding
/// forward. <c>src/EvilHop</c> gets no equivalent lookup table; a library consumer with one odd file
/// constructs <c>new N100FSerializer(profile with { … })</c> directly.
/// </summary>
internal sealed class BuildProfiles
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IReadOnlyList<BuildProfileOverride> _overrides;

    private BuildProfiles(IReadOnlyList<BuildProfileOverride> overrides) => _overrides = overrides;

    /// <summary>
    /// Loads the committed manifest, copied beside the tool's executable as <c>BuildProfiles.json</c>.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown when the manifest is missing.</exception>
    public static BuildProfiles LoadDefault()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "BuildProfiles.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Build profile manifest not found at '{path}'.", path);

        return Load(File.ReadAllText(path));
    }

    /// <summary>
    /// Parses a manifest from <paramref name="json"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when an entry's <c>pathPrefix</c> is empty or whitespace.</exception>
    public static BuildProfiles Load(string json)
    {
        var overrides = JsonSerializer.Deserialize<List<BuildProfileOverride>>(json, JsonOptions) ?? [];

        foreach (var entry in overrides)
            if (string.IsNullOrWhiteSpace(entry.PathPrefix))
                throw new ArgumentException("Build profile manifest entries must have a non-empty 'pathPrefix'.");

        return new BuildProfiles(overrides);
    }

    /// <summary>
    /// Resolves <paramref name="default"/> against <paramref name="relativePath"/>, applying the
    /// first entry whose <c>pathPrefix</c> matches (plain, case-insensitive <c>StartsWith</c>). Entry
    /// order is significant. Returns <paramref name="default"/> unchanged when nothing matches.
    /// </summary>
    public FormatProfile Resolve(FormatProfile @default, string relativePath)
    {
        var match = _overrides.FirstOrDefault(o => relativePath.StartsWith(o.PathPrefix, StringComparison.OrdinalIgnoreCase));
        return match is null ? @default : match.Profile.ApplyTo(@default);
    }
}

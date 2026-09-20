using EvilHop.Common;
using EvilHop.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace EvilHop.Corpus.Archives;

internal sealed record ProfileOverride(bool? StreamDataHasPaddingField, PlatformFieldOrder? PlatformFieldOrder, bool? EntityHasPadding, bool? EntityHasExtendedFields, bool? PickupTypesHasPulseFields, bool? LinkHasExtendedFields, bool? TriggerHasDirectionAndFlags, bool? EnvironmentHasExtendedFields, bool? NPCHasExtendedFields, bool? SurfaceHasDamageFields, bool? VillainHasTaskWidgetSecondId, bool? TimerHasRandomRange, bool? DestructibleObjectHasSwapEffects, bool? BoulderHasSoundFalloff, bool? ShrapnelHasExtendedFragFields, bool? ShrapnelSoundHasExtendedFields, Platform? Platform)
{
    public FormatProfile ApplyTo(FormatProfile profile) => profile with
    {
        StreamDataHasPaddingField = StreamDataHasPaddingField ?? profile.StreamDataHasPaddingField,
        PlatformFieldOrder = PlatformFieldOrder ?? profile.PlatformFieldOrder,
        EntityHasPadding = EntityHasPadding ?? profile.EntityHasPadding,
        EntityHasExtendedFields = EntityHasExtendedFields ?? profile.EntityHasExtendedFields,
        PickupTypesHasPulseFields = PickupTypesHasPulseFields ?? profile.PickupTypesHasPulseFields,
        LinkHasExtendedFields = LinkHasExtendedFields ?? profile.LinkHasExtendedFields,
        TriggerHasDirectionAndFlags = TriggerHasDirectionAndFlags ?? profile.TriggerHasDirectionAndFlags,
        EnvironmentHasExtendedFields = EnvironmentHasExtendedFields ?? profile.EnvironmentHasExtendedFields,
        NPCHasExtendedFields = NPCHasExtendedFields ?? profile.NPCHasExtendedFields,
        SurfaceHasDamageFields = SurfaceHasDamageFields ?? profile.SurfaceHasDamageFields,
        VillainHasTaskWidgetSecondId = VillainHasTaskWidgetSecondId ?? profile.VillainHasTaskWidgetSecondId,
        TimerHasRandomRange = TimerHasRandomRange ?? profile.TimerHasRandomRange,
        DestructibleObjectHasSwapEffects = DestructibleObjectHasSwapEffects ?? profile.DestructibleObjectHasSwapEffects,
        BoulderHasSoundFalloff = BoulderHasSoundFalloff ?? profile.BoulderHasSoundFalloff,
        ShrapnelHasExtendedFragFields = ShrapnelHasExtendedFragFields ?? profile.ShrapnelHasExtendedFragFields,
        ShrapnelSoundHasExtendedFields = ShrapnelSoundHasExtendedFields ?? profile.ShrapnelSoundHasExtendedFields,
        Platform = Platform ?? profile.Platform
    };
}

internal sealed record BuildProfileOverride(string PathPattern, ProfileOverride Profile);

/// <summary>
/// Per-archive <see cref="FormatProfile"/> quirk overrides, keyed by a <see cref="DiscoveredArchive.RelativePath"/>
/// pattern. Committed at <c>tools/EvilHop.Corpus/BuildProfiles.json</c> - the corpus tool's own record
/// of builds whose bytes don't match their game's default profile, kept here rather than in
/// <c>artifacts/</c>, which is gitignored and rebuilt per contributor and so cannot carry a finding
/// forward. <c>src/EvilHop</c> gets no equivalent lookup table; a library consumer with one odd file
/// constructs <c>new N100FSerializer(profile with { … })</c> directly.
/// </summary>
/// <remarks>
/// A <c>pathPattern</c> with no <c>*</c> matches as a plain, case-insensitive prefix - every archive
/// under that directory, the original and still the common case. One containing <c>*</c>/<c>**</c> is
/// matched as a glob against the whole path instead: <c>*</c> stands in for exactly one path segment,
/// <c>**</c> for any number of them (including zero). Reach for a glob when a quirk recurs under many
/// build directories that don't share a prefix - e.g. one specific leftover level's file, repeated
/// under every platform/region a game shipped.
/// </remarks>
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
    /// <exception cref="ArgumentException">Thrown when an entry's <c>pathPattern</c> is empty or whitespace.</exception>
    public static BuildProfiles Load(string json)
    {
        var overrides = JsonSerializer.Deserialize<List<BuildProfileOverride>>(json, JsonOptions) ?? [];

        foreach (var entry in overrides)
            if (string.IsNullOrWhiteSpace(entry.PathPattern))
                throw new ArgumentException("Build profile manifest entries must have a non-empty 'pathPattern'.");

        return new BuildProfiles(overrides);
    }

    /// <summary>
    /// Resolves <paramref name="default"/> against <paramref name="relativePath"/>, applying the
    /// first entry whose <c>pathPattern</c> matches. Entry order is significant - list the most
    /// specific pattern first. Returns <paramref name="default"/> unchanged when nothing matches.
    /// </summary>
    public FormatProfile Resolve(FormatProfile @default, string relativePath)
    {
        var match = _overrides.FirstOrDefault(o => Matches(o.PathPattern, relativePath));
        return match is null ? @default : match.Profile.ApplyTo(@default);
    }

    private static bool Matches(string pattern, string relativePath) =>
        pattern.Contains('*')
            ? ToRegex(pattern).IsMatch(relativePath)
            : relativePath.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);

    private static Regex ToRegex(string pattern)
    {
        string body = Regex.Escape(pattern)
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*");
        return new Regex($"^{body}$", RegexOptions.IgnoreCase);
    }
}

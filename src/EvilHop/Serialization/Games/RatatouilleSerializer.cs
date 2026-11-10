using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.Ratatouille"/>.
/// </summary>
/// <param name="profile"><inheritdoc cref="Serializer.Profile"/></param>
public sealed class RatatouilleSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every Ratatouille build reads correctly under once a platform is stated.</summary>
    internal static FormatProfile DefaultProfile { get; } = new(
        GameVersion.Ratatouille,
        null,
        PlatformFieldOrder.LanguageRegion,
        StreamDataHasPaddingField: true);

    /// <summary>
    /// Initializes a block-only instance of <see cref="RatatouilleSerializer"/>: no platform is stated, so assets cannot be opened.
    /// </summary>
    public RatatouilleSerializer() : this(DefaultProfile) { }
}

using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.Incredibles"/>.
/// </summary>
/// <param name="profile"><inheritdoc cref="Serializer.Profile"/></param>
public sealed class IncrediblesSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every Incredibles build reads correctly under once a platform is stated.</summary>
    internal static FormatProfile DefaultProfile { get; } = new(
        GameVersion.Incredibles,
        null,
        PlatformFieldOrder.LanguageRegion,
        StreamDataHasPaddingField: true);

    /// <summary>
    /// Initializes a block-only instance of <see cref="IncrediblesSerializer"/>: no platform is stated, so assets cannot be opened.
    /// </summary>
    public IncrediblesSerializer() : this(DefaultProfile) { }
}

using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.ROTU"/>.
/// </summary>
/// <param name="profile"><inheritdoc cref="Serializer.Profile"/></param>
public sealed class ROTUSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every ROTU build reads correctly under once a platform is stated.</summary>
    internal static FormatProfile DefaultProfile { get; } = new(
        GameVersion.ROTU,
        null,
        PlatformFieldOrder.LanguageRegion,
        StreamDataHasPaddingField: true);

    /// <summary>
    /// Initializes a block-only instance of <see cref="ROTUSerializer"/>: no platform is stated, so assets cannot be opened.
    /// </summary>
    public ROTUSerializer() : this(DefaultProfile) { }
}

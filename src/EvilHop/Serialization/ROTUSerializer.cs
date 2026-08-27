using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.ROTU"/>.
/// </summary>
/// <param name="profile"><inheritdoc cref="Serializer.Profile"/></param>
public sealed class ROTUSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every ROTU build reads correctly under.</summary>
    public static FormatProfile DefaultProfile { get; } = new(
        GameVersion.ROTU,
        PlatformFieldOrder.LanguageRegion,
        StreamDataHasPaddingField: true);

    /// <summary>
    /// Initializes a new instance of <see cref="ROTUSerializer"/> with <see cref="DefaultProfile"/>.
    /// </summary>
    public ROTUSerializer() : this(DefaultProfile) { }
}

using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.Incredibles"/>.
/// </summary>
/// <param name="profile">The format quirks and game identity this serializer reads with.</param>
public sealed class IncrediblesSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every Incredibles build reads correctly under.</summary>
    public static FormatProfile DefaultProfile { get; } = new(
        GameVersion.Incredibles,
        PlatformFieldOrder.LanguageRegion,
        StreamDataHasPaddingField: true);

    /// <summary>
    /// Initializes a new instance of <see cref="IncrediblesSerializer"/> with <see cref="DefaultProfile"/>.
    /// </summary>
    public IncrediblesSerializer() : this(DefaultProfile) { }
}

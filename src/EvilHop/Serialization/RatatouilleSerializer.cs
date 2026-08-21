using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.Ratatouille"/>.
/// </summary>
/// <param name="profile">The format quirks and game identity this serializer reads with.</param>
public sealed class RatatouilleSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every Ratatouille build reads correctly under.</summary>
    public static FormatProfile DefaultProfile { get; } = new(
        GameVersion.Ratatouille,
        PlatformFieldOrder.LanguageRegion,
        StreamDataHasPaddingField: true);

    /// <summary>
    /// Initializes a new instance of <see cref="RatatouilleSerializer"/> with <see cref="DefaultProfile"/>.
    /// </summary>
    public RatatouilleSerializer() : this(DefaultProfile) { }
}

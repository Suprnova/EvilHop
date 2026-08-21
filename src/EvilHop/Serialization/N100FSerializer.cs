using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.N100F"/>. 
/// </summary>
/// <param name="profile">The format quirks and game identity this serializer reads with.</param>
public sealed class N100FSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every N100F build reads correctly under, except the 2001-06-11 prototype.</summary>
    public static FormatProfile DefaultProfile { get; } = new(
        GameVersion.N100F,
        PlatformFieldOrder.PlatformNameRegionLanguage,
        StreamDataHasPaddingField: true);

    /// <summary>
    /// Initializes a new instance of <see cref="N100FSerializer"/> with <see cref="DefaultProfile"/>.
    /// </summary>
    public N100FSerializer() : this(DefaultProfile) { }
}

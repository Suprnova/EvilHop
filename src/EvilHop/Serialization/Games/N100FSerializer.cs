using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.N100F"/>. 
/// </summary>
/// <param name="profile"><inheritdoc cref="Serializer.Profile"/></param>
public sealed class N100FSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every N100F build reads correctly under, except the 2001-06-11 prototype, once a platform is stated.</summary>
    internal static FormatProfile DefaultProfile { get; } = new(
        GameVersion.N100F,
        null,
        PlatformFieldOrder.PlatformNameRegionLanguage,
        StreamDataHasPaddingField: true);

    /// <summary>
    /// Initializes a block-only instance of <see cref="N100FSerializer"/>: no platform is stated, so assets cannot be opened.
    /// </summary>
    public N100FSerializer() : this(DefaultProfile) { }
}

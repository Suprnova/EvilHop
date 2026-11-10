using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.BFBB"/>. 
/// </summary>
/// /// <param name="profile"><inheritdoc cref="Serializer.Profile"/></param>
public sealed class BFBBSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every BFBB build reads correctly under once a platform is stated.</summary>
    internal static FormatProfile DefaultProfile { get; } = new(
        GameVersion.BFBB,
        null,
        PlatformFieldOrder.PlatformNameRegionLanguage,
        StreamDataHasPaddingField: true,
        EntityHasPadding: true);

    /// <summary>
    /// Initializes a block-only instance of <see cref="BFBBSerializer"/>: no platform is stated, so assets cannot be opened.
    /// </summary>
    public BFBBSerializer() : this(DefaultProfile) { }
}

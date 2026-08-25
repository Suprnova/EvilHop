using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.BFBB"/>. 
/// </summary>
/// <param name="profile">The format quirks and game identity this serializer reads with.</param>
public sealed class BFBBSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every BFBB build reads correctly under.</summary>
    public static FormatProfile DefaultProfile { get; } = new(
        GameVersion.BFBB,
        PlatformFieldOrder.PlatformNameRegionLanguage,
        StreamDataHasPaddingField: true,
        EntityHasPadding: true);

    /// <summary>
    /// Initializes a new instance of <see cref="BFBBSerializer"/> with <see cref="DefaultProfile"/>.
    /// </summary>
    public BFBBSerializer() : this(DefaultProfile) { }
}

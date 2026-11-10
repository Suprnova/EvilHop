using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.TSSM"/>.
/// </summary>
/// <param name="profile"><inheritdoc cref="Serializer.Profile"/></param>
public sealed class TSSMSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every TSSM build reads correctly under once a platform is stated.</summary>
    internal static FormatProfile DefaultProfile { get; } = new(
        GameVersion.TSSM,
        null,
        PlatformFieldOrder.LanguageRegion,
        StreamDataHasPaddingField: true);

    /// <summary>
    /// Initializes a block-only instance of <see cref="TSSMSerializer"/>: no platform is stated, so assets cannot be opened.
    /// </summary>
    public TSSMSerializer() : this(DefaultProfile) { }
}

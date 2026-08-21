using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives built for <see cref="GameVersion.TSSM"/>.
/// </summary>
/// <param name="profile">The format quirks and game identity this serializer reads with.</param>
public sealed class TSSMSerializer(FormatProfile profile) : Serializer(profile)
{
    /// <summary>The profile every TSSM build reads correctly under.</summary>
    public static FormatProfile DefaultProfile { get; } = new(
        GameVersion.TSSM,
        PlatformFieldOrder.LanguageRegion,
        StreamDataHasPaddingField: true);

    /// <summary>
    /// Initializes a new instance of <see cref="TSSMSerializer"/> with <see cref="DefaultProfile"/>.
    /// </summary>
    public TSSMSerializer() : this(DefaultProfile) { }
}

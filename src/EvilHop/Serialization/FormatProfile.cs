using EvilHop.Common;

namespace EvilHop.Serialization;

/// <summary>
/// The format quirks a serializer reads a HIP archive with, beyond the shared block envelope every
/// game agrees on.
/// </summary>
/// <param name="Game">
/// The game this profile belongs to.
/// </param>
/// <param name="PlatformFieldOrder">
/// Which field a <c>PLAT</c> block's run of strings maps to. Unreachable for games with no
/// <c>PLAT</c> block.
/// </param>
/// <param name="StreamDataHasPaddingField">
/// Whether a <c>DPAK</c> block's content leads with a padding-amount field before its data.
/// </param>
/// <param name="EntityHasPadding">
/// Whether an <c>EntityAsset</c>'s on-disk layout inserts four bytes of padding after its four flag
/// bytes. True for <see cref="GameVersion.BFBB"/> release builds, false for every other game and,
/// per the wiki, for BFBB beta builds too.
/// </param>
/// <remarks>
/// Constructed exactly once per game as a <c>DefaultProfile</c> and adjusted everywhere else with
/// the <see langword="with"/> keyword.
/// </remarks>
public sealed record FormatProfile(
    GameVersion Game,
    PlatformFieldOrder PlatformFieldOrder,
    bool StreamDataHasPaddingField,
    bool EntityHasPadding = false);

/// <summary>
/// Which field a <c>PLAT</c> block's run of strings maps to.
/// </summary>
public enum PlatformFieldOrder
{
    /// <summary>
    /// <c>PlatformId, PlatformName, Region, Language, GameName</c>.
    /// </summary>
    PlatformNameRegionLanguage,

    /// <summary>
    /// <c>PlatformId, Language, Region, GameName</c>.
    /// </summary>
    LanguageRegion
}

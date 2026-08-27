using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Serialization;

/// <summary>
/// The format quirks a serializer reads a HIP archive with, beyond the shared block envelope every
/// game agrees on.
/// </summary>
/// <param name="Game">
/// The game this profile belongs to.
/// </param>
/// <param name="PlatformFieldOrder">
/// Which fields a <see cref="PackagePlatform"/>'s strings maps to.
/// </param>
/// <param name="StreamDataHasPaddingField">
/// Whether a <see cref="StreamData"/> content leads with a padding-amount field before its data.
/// </param>
/// <param name="EntityHasPadding">
/// Whether a <see cref="EntityAsset"/> on-disk layout inserts four bytes of padding after its four
/// flag bytes. True for <see cref="GameVersion.BFBB"/> release builds, false for every other game,
/// including beta builds.
/// </param>
/// <param name="Endianness">
/// The byte order of an asset's own fields, as opposed to the block envelope's, which is always
/// big-endian regardless of platform. GameCube archives are big-endian; PS2 and Xbox archives are
/// little-endian. <c>DefaultProfile</c> assumes GameCube.
/// </param>
/// <remarks>
/// Constructed exactly once per game as a <c>DefaultProfile</c> and adjusted everywhere else with
/// the <see langword="with"/> keyword.
/// </remarks>
/// TODO: Deriving per platform is unresolved. Move Endianness to be located after Game once
/// resolved. On the fence about storing platform as an enum and deriving endianness from it.
/// Capturing the platform seems smart, but doesn't allow setting platform and endianness
/// independently (though why would anyone need that?).
public sealed record FormatProfile(
    GameVersion Game,
    PlatformFieldOrder PlatformFieldOrder,
    bool StreamDataHasPaddingField,
    bool EntityHasPadding = false,
    Endianness Endianness = Endianness.Big);

/// <summary>
/// Which field a <see cref="PackagePlatform"/> block's run of strings maps to.
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

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
/// <param name="Platform">
/// The console this build targets. Every archive observed agrees with its console's hardware byte
/// order and disc sector size, so <see cref="Platform"/> alone determines both rather than letting
/// them vary independently.
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
/// <param name="EntityHasExtendedFields">
/// Whether an <see cref="EntityAsset"/> on-disk layout includes <c>SurfaceId</c>, <c>ColorMultiplier</c>,
/// <c>SeeThroughSpeed</c>, and <c>AnimListId</c> after <c>Scale</c>/before <c>ModelId</c>. False only for
/// <see cref="GameVersion.N100F"/>'s 2001-06-11 prototype, whose entities are just flags, angle,
/// position, scale, and a model ID; true everywhere else, including every later N100F build.
/// </param>
/// <param name="PickupTypesHasPulseFields">
/// Whether a <see cref="PickupTypeEntry"/> carries <see cref="PickupTypeEntry.PulseModelId"/>,
/// <see cref="PickupTypeEntry.PulseTime"/>, <see cref="PickupTypeEntry.PulseAddScale"/>,
/// <see cref="PickupTypeEntry.PulseMoveDown"/>, and <see cref="PickupTypeEntry.ColorMultiplier"/>.
/// True everywhere except Incredibles' <c>prototype_2004-07-19</c> build, whose pickup pulse effect
/// and tint hadn't been added yet.
/// </param>
/// <remarks>
/// Constructed exactly once per game as a <c>DefaultProfile</c> and adjusted everywhere else with
/// the <see langword="with"/> keyword. Every <c>DefaultProfile</c> targets <see cref="Common.Platform.GameCube"/>.
/// </remarks>
public sealed record FormatProfile(
    GameVersion Game,
    Platform Platform,
    PlatformFieldOrder PlatformFieldOrder,
    bool StreamDataHasPaddingField,
    bool EntityHasPadding = false,
    bool EntityHasExtendedFields = true,
    bool PickupTypesHasPulseFields = true)
{
    /// <summary>
    /// The byte order of an asset's own fields, as opposed to the block envelope's, which is always
    /// big-endian regardless of platform.
    /// </summary>
    public Endianness Endianness => Platform == Platform.GameCube ? Endianness.Big : Endianness.Little;
}

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

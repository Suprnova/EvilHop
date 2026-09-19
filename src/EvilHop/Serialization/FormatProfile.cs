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
/// <param name="EntityHasAnimListId">
/// Whether an <see cref="EntityAsset"/> on-disk layout includes <c>AnimListId</c> after <c>ModelId</c> -
/// independent of <see cref="EntityHasExtendedFields"/>, which must be true for this to apply at all.
/// True for every real build; false only via a <c>BuildProfiles.json</c> override for BFBB's leftover
/// <c>gl/Working</c>/<c>gl/New Folder</c> archives, whose entities are frozen at a layout that predates
/// the field.
/// </param>
/// <param name="PickupTypesHasPulseFields">
/// Whether a <see cref="PickupTypeEntry"/> carries <see cref="PickupTypeEntry.PulseModelId"/>,
/// <see cref="PickupTypeEntry.PulseTime"/>, <see cref="PickupTypeEntry.PulseAddScale"/>,
/// <see cref="PickupTypeEntry.PulseMoveDown"/>, and <see cref="PickupTypeEntry.ColorMultiplier"/>.
/// True everywhere except Incredibles' <c>prototype_2004-07-19</c> build, whose pickup pulse effect
/// and tint hadn't been added yet.
/// </param>
/// <param name="LinkHasExtendedFields">
/// Whether a <see cref="Link"/> is followed by <see cref="Link.ParamWidgetAssetId"/> and
/// <see cref="Link.CheckAssetId"/> - 32 bytes per link rather than 24. False only for
/// <see cref="GameVersion.N100F"/>'s 2001-06-11 prototype; true everywhere else. Not read from this
/// record directly - passed explicitly at whichever <see cref="Assets.Serialization.LinkSerialization"/>
/// call sites a build known to differ actually reaches.
/// </param>
/// <param name="TriggerHasDirectionAndFlags">
/// Whether a <see cref="TriggerAsset"/> carries a <see cref="TriggerAsset.Direction"/> and
/// <see cref="TriggerAsset.Flags"/> after its four trigger positions. False only for
/// <see cref="GameVersion.N100F"/>'s 2001-06-11 prototype, whose triggers are just the four
/// positions; true everywhere else.
/// </param>
/// <param name="EnvironmentHasExtendedFields">
/// Whether an <see cref="EnvironmentAsset"/> carries anything beyond <see cref="EnvironmentAsset.BspId"/>
/// and <see cref="EnvironmentAsset.StartCameraId"/> - climate, lighting, and the secondary BSP/mapper
/// IDs. False only for <see cref="GameVersion.N100F"/>'s 2001-06-11 prototype, whose environments are
/// just those two IDs; true everywhere else.
/// </param>
/// <param name="NPCHasExtendedFields">
/// Whether an <see cref="NPCAsset"/> carries its combat/AI stat block (<see cref="NPCAsset.ActivateRadius"/>
/// through <see cref="NPCAsset.MinGameDifficulty"/>). False only for <see cref="GameVersion.N100F"/>'s
/// 2001-06-11 prototype: real archives from that build carry a fixed 72-byte region there whose field
/// boundaries can't be determined from the identical placeholder data every observed instance has, so
/// it's preserved as <see cref="Asset.GetUnparsedTail"/> instead of being decoded. True everywhere else.
/// </param>
/// <param name="SurfaceHasDamageFields">
/// Whether a <see cref="SurfaceAsset"/> carries <see cref="SurfaceAsset.DamageTimer"/> and
/// <see cref="SurfaceAsset.DamageBounce"/> after <see cref="SurfaceAsset.WallJumpScaleY"/>. True for
/// every real build; false only via a <c>BuildProfiles.json</c> override for BFBB's unused <c>b301</c>
/// level and its leftover <c>gl/Working</c>/<c>gl/New Folder</c> archives, whose <c>SURF</c>s are
/// frozen at a layout that predates those two fields.
/// </param>
/// <param name="VillainHasTaskWidgetSecondId">
/// Whether a <see cref="VillainAsset"/> carries <see cref="VillainAsset.TaskWidgetSecondId"/> after
/// <see cref="VillainAsset.TaskWidgetPrimeId"/>. True for every real build; false only via a
/// <c>BuildProfiles.json</c> override for BFBB's leftover <c>gl/Working</c>/<c>gl/New Folder</c>
/// archives, whose <c>VILN</c>s are frozen at a layout that predates the field.
/// </param>
/// <param name="TimerHasRandomRange">
/// Whether a <see cref="TimerAsset"/> carries <see cref="TimerAsset.RandomRange"/> after
/// <see cref="TimerAsset.Seconds"/>. Independent of <see cref="GameVersion.N100F"/>, which never has
/// it regardless of this switch. True for every real build; false only via a
/// <c>BuildProfiles.json</c> override for BFBB's leftover <c>gl/Working</c>/<c>gl/New Folder</c>
/// archives, whose <c>TIMR</c>s are frozen at a layout that predates the field.
/// </param>
/// <param name="DestructibleObjectHasSwapEffects">
/// Whether a <see cref="DestructibleObjectAsset"/> carries <see cref="DestructibleObjectAsset.HitSfxId"/>,
/// <see cref="DestructibleObjectAsset.HitModelId"/>, and <see cref="DestructibleObjectAsset.DestroyModelId"/>
/// after <see cref="DestructibleObjectAsset.DestroySfxId"/> - independent of
/// <see cref="GameVersion.BFBB"/>, which must be the active game for any of the six BFBB-only fields
/// to be read at all. True for every real build; false only via a <c>BuildProfiles.json</c> override
/// for BFBB's leftover <c>gl/Working</c>/<c>gl/New Folder</c> archives, whose <c>DSTR</c>s are frozen
/// at a layout that predates the three fields.
/// </param>
/// <param name="BoulderHasSoundFalloff">
/// Whether a <see cref="BoulderAsset"/> carries <see cref="BoulderAsset.MaxSoundVelocity"/>,
/// <see cref="BoulderAsset.InnerRadius"/>, and <see cref="BoulderAsset.OuterRadius"/> after
/// <see cref="BoulderAsset.MinSoundVelocity"/> - independent of <see cref="GameVersion.BFBB"/>, which
/// must be the active game for <see cref="BoulderAsset.InnerRadius"/>/<see cref="BoulderAsset.OuterRadius"/>
/// to be read at all. True for every real build; false only via a <c>BuildProfiles.json</c> override
/// for BFBB's leftover <c>gl/Working</c>/<c>gl/New Folder</c> archives, whose <c>BOUL</c>s are frozen
/// at a layout that predates the three fields.
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
    bool EntityHasAnimListId = true,
    bool PickupTypesHasPulseFields = true,
    bool LinkHasExtendedFields = true,
    bool TriggerHasDirectionAndFlags = true,
    bool EnvironmentHasExtendedFields = true,
    bool NPCHasExtendedFields = true,
    bool SurfaceHasDamageFields = true,
    bool VillainHasTaskWidgetSecondId = true,
    bool TimerHasRandomRange = true,
    bool DestructibleObjectHasSwapEffects = true,
    bool BoulderHasSoundFalloff = true)
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

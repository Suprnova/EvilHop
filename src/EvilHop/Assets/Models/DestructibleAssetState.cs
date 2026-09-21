using EvilHop.Common;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// One state of a <see cref="DestructibleAsset"/>: a model, plus the effects and sounds used while
/// it is active.
/// </summary>
public sealed class DestructibleAssetState
{
    /// <summary>
    /// Unknown. A percentage value.
    /// </summary>
    public uint Percent { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> used while in this state.
    /// </summary>
    public AssetId ModelId { get; set; }

    /// <summary>
    /// Unknown. A <see cref="AssetType.Shrapnel"/> reference.
    /// </summary>
    public AssetId ShrapnelId { get; set; }

    /// <summary>
    /// Unknown. A <see cref="AssetType.Shrapnel"/> reference, for this state's "hit" variant.
    /// </summary>
    public AssetId HitShrapnelId { get; set; }

    /// <summary>
    /// Unknown. A <see cref="AssetType.SoundGroup"/> reference, for this state's "idle" variant.
    /// </summary>
    public AssetId IdleSoundGroupId { get; set; }

    /// <summary>
    /// Unknown. A <see cref="AssetType.SoundGroup"/> reference, for this state's "fx" variant.
    /// </summary>
    public AssetId FxSoundGroupId { get; set; }

    /// <summary>
    /// Unknown. A <see cref="AssetType.SoundGroup"/> reference, for this state's "hit" variant.
    /// </summary>
    public AssetId HitSoundGroupId { get; set; }

    /// <summary>
    /// Unknown. A <see cref="AssetType.SoundGroup"/> reference, for this state's "switch fx" variant.
    /// </summary>
    public AssetId SwitchFxSoundGroupId { get; set; }

    /// <summary>
    /// Unknown. A <see cref="AssetType.SoundGroup"/> reference, for this state's "switch hit" variant.
    /// </summary>
    public AssetId SwitchHitSoundGroupId { get; set; }

    /// <summary>
    /// Unknown. A <see cref="AssetType.Dynamic"/> rumble effect reference, for this state's "hit"
    /// variant.
    /// </summary>
    public AssetId HitRumbleId { get; set; }

    /// <summary>
    /// Unknown. A <see cref="AssetType.Dynamic"/> rumble effect reference, for this state's "switch"
    /// variant.
    /// </summary>
    public AssetId SwitchRumbleId { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public uint FxFlags { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/>s attached to this state.
    /// </summary>
    public Collection<AssetId> AnimationIds { get; } = [];
}

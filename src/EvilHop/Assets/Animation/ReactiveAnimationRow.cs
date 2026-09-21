using EvilHop.Common;

namespace EvilHop.Assets;

/// <summary>
/// One <see cref="ReactiveAnimationAsset"/> row: the models, animations, and sounds to swap to for
/// one reactive behavior.
/// </summary>
public sealed class ReactiveAnimationRow
{
    /// <summary>
    /// The <see cref="AssetType.Model"/> used while the reacting object is static.
    /// </summary>
    public AssetId StaticModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> used for the reacting object's bounding/collision
    /// representation.
    /// </summary>
    public AssetId BoundModelId { get; set; }

    /// <summary>
    /// The distance from the camera beyond which the reacting object's level of detail drops.
    /// </summary>
    public float LodDistance { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/> played while idle.
    /// </summary>
    public AssetId IdleAnimationId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/> played while something moves through the reacting
    /// object.
    /// </summary>
    public AssetId MoveThroughAnimationId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/> played when the reacting object is hit.
    /// </summary>
    public AssetId HitAnimationId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SoundGroup"/> played while idle.
    /// </summary>
    public AssetId IdleSoundGroupId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SoundGroup"/> played while something moves through the reacting
    /// object.
    /// </summary>
    public AssetId MoveThroughSoundGroupId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SoundGroup"/> played when the reacting object is hit.
    /// </summary>
    public AssetId HitSoundGroupId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> swapped to once the reacting object has burnt up.
    /// </summary>
    public AssetId BurntModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/> played while the reacting object burns.
    /// </summary>
    public AssetId BurnAnimationId { get; set; }

    /// <summary>
    /// How much fuel the reacting object's fire has before burning out.
    /// </summary>
    /// TODO: figure out what this actually means.
    public float BurnFuel { get; set; }

    /// <summary>
    /// The size of the flame effect while the reacting object burns.
    /// </summary>
    public float BurnFlameSize { get; set; }

    /// <summary>
    /// The scale of the particles emitted while the reacting object burns.
    /// </summary>
    public float BurnEmitScale { get; set; }

    /// <summary>
    /// The radius within which something is considered to be moving through the reacting object.
    /// </summary>
    public float MoveThroughRadius { get; set; }
}

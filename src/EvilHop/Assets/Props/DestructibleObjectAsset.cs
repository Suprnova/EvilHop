using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityAsset"/> that can be damaged and destroyed, optionally spawning shrapnel,
/// playing sound effects, or swapping to a replacement model along the way.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/DSTR">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class DestructibleObjectAsset() : EntityAsset(AssetType.DestructibleObject, baseType: 0x1B), IHasModel, IHasAnimList
{
    /// <summary>
    /// The playback speed of the animation referenced by <see cref="IHasAnimList.AnimListId"/>.
    /// </summary>
    public float AnimationSpeed { get; set; }

    /// <summary>
    /// The animation state this object starts in.
    /// </summary>
    public uint InitialAnimationState { get; set; }

    /// <summary>
    /// The number of hits this object can take before it is destroyed.
    /// </summary>
    public uint Health { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the item spawned when this object is destroyed, if any.
    /// </summary>
    public AssetId SpawnItemId { get; set; }

    /// <summary>
    /// Which kinds of hits this object reacts to.
    /// </summary>
    public DamageSource HitFlags { get; set; }

    /// <summary>
    /// This object's collision type, separate from <see cref="Physical.IEntityAsset.CollisionFlags"/>.
    /// </summary>
    public byte CollisionType { get; set; }

    /// <summary>
    /// Which particle effect plays when this object is destroyed.
    /// </summary>
    public Effect FxType { get; set; }

    /// <summary>
    /// The radius, in world units, of the blast damage dealt when this object is destroyed.
    /// </summary>
    public float BlastRadius { get; set; }

    /// <summary>
    /// The strength of the blast damage dealt when this object is destroyed.
    /// </summary>
    public float BlastStrength { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Shrapnel"/> spawned when this object is
    /// destroyed, if any. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId DestroyShrapnelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Shrapnel"/> spawned when this object is
    /// hit but not destroyed, if any. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId HitShrapnelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the sound effect played when this object is destroyed, if any.
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId DestroySfxId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the sound effect played when this object is hit but not
    /// destroyed, if any. Not present in <see cref="GameVersion.N100F"/>, nor - per
    /// <see cref="FormatProfile.DestructibleObjectHasSwapEffects"/> - in BFBB's leftover
    /// <c>gl/Working</c>/<c>gl/New Folder</c> archives.
    /// </summary>
    public AssetId HitSfxId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> this object swaps to when hit
    /// but not destroyed, if any. Not present in <see cref="GameVersion.N100F"/>, nor - per
    /// <see cref="FormatProfile.DestructibleObjectHasSwapEffects"/> - in BFBB's leftover
    /// <c>gl/Working</c>/<c>gl/New Folder</c> archives.
    /// </summary>
    public AssetId HitModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> this object swaps to when
    /// destroyed, if any. Not present in <see cref="GameVersion.N100F"/>, nor - per
    /// <see cref="FormatProfile.DestructibleObjectHasSwapEffects"/> - in BFBB's leftover
    /// <c>gl/Working</c>/<c>gl/New Folder</c> archives.
    /// </summary>
    public AssetId DestroyModelId { get; set; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasAnimList.AnimListId { get => Physical.AnimListId; set => Physical.AnimListId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.DestructibleObject"/> is known to be read
    /// by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
    };

    /// <summary>
    /// Flags determining which player attacks or physics impacts can damage this destructible object.
    /// </summary>
    [Flags]
    public enum DamageSource : uint
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Reacts to Patrick's slam attack. <see cref="GameVersion.BFBB"/> only.
        /// </summary>
        PatrickSlam = 1 << 10,
        /// <summary>
        /// Reacts to being thrown. <see cref="GameVersion.BFBB"/> only.
        /// </summary>
        Throw = 1 << 11,
        /// <summary>
        /// Reacts to a bubble bounce. <see cref="GameVersion.BFBB"/> only.
        /// </summary>
        BubbleBounce = 1 << 13,
        /// <summary>
        /// Reacts to a bubble bash attack. <see cref="GameVersion.BFBB"/> only.
        /// </summary>
        BubbleBash = 1 << 14,
        /// <summary>
        /// Unknown. Plays a random hit sound stream when set alongside a reacted-to hit.
        /// <see cref="GameVersion.BFBB"/> only.
        /// </summary>
        Unknown = 1 << 15,
    }

    /// <summary>
    /// Specifies the visual and sound effect spawned upon destruction.
    /// </summary>
    public enum Effect : byte
    {
        /// <summary>
        /// No effect plays.
        /// </summary>
        None = 0,
        /// <summary>
        /// A dust cloud effect plays.
        /// </summary>
        Dust = 1,
        /// <summary>
        /// An explosion effect plays.
        /// </summary>
        Explosion = 2,
        /// <summary>
        /// A web effect plays.
        /// </summary>
        Web = 3,
    }
}

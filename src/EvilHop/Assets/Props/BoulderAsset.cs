using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A physics-driven rolling/bouncing sphere, such as a boulder thrown by a cannon.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/BOUL">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class BoulderAsset() : EntityAsset(AssetType.Boulder, baseType: 0x2F), IHasModel, IHasAnimList
{
    /// <summary>
    /// The downward acceleration applied to this boulder.
    /// </summary>
    public float Gravity { get; set; }

    /// <summary>
    /// This boulder's mass, used to scale forces applied to it.
    /// </summary>
    public float Mass { get; set; }

    /// <summary>
    /// How much of this boulder's velocity is preserved along the collision normal when it bounces
    /// off a surface.
    /// </summary>
    public float Bounce { get; set; }

    /// <summary>
    /// How quickly this boulder's velocity decays while rolling against a surface.
    /// </summary>
    public float Friction { get; set; }

    /// <summary>
    /// If this boulder's downward velocity is below this threshold when it hits the ground, that
    /// velocity is reset to 0. Only present in <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public float StaticFriction { get; set; }

    /// <summary>
    /// The maximum speed this boulder can move at.
    /// </summary>
    public float MaxVelocity { get; set; }

    /// <summary>
    /// The maximum speed this boulder can rotate at.
    /// </summary>
    public float MaxAngularVelocity { get; set; }

    /// <summary>
    /// How strongly this boulder's rotation axis follows its direction of travel, versus staying
    /// where it was.
    /// </summary>
    public float Stickiness { get; set; }

    /// <summary>
    /// The downward velocity threshold, on landing, below which <see cref="Bounce"/> is not applied.
    /// </summary>
    public float BounceDamping { get; set; }

    /// <summary>
    /// This boulder's behavior flags.
    /// </summary>
    public BoulderFlags Flags { get; set; }

    /// <summary>
    /// The lifetime, in seconds, before this boulder is destroyed, if
    /// <see cref="BoulderFlags.DieAfterKillTimer"/> is set. If 0, the lifetime is infinite.
    /// </summary>
    public float KillTimer { get; set; }

    /// <summary>
    /// The number of hits this boulder can take from a damaging surface (see
    /// <see cref="BoulderFlags.DieOnDamagingSurface"/>) before it is destroyed.
    /// </summary>
    public uint Hitpoints { get; set; }

    /// <summary>
    /// The sound played when this boulder bounces off a surface, if any. References an
    /// <see cref="AssetType.Sound"/> in <see cref="GameVersion.BFBB"/>, or an
    /// <see cref="AssetType.SoundGroup"/> in every other supported game.
    /// </summary>
    public AssetId BounceSoundId { get; set; }

    /// <summary>
    /// The volume <see cref="BounceSoundId"/> plays at. Only present in
    /// <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public float Volume { get; set; }

    /// <summary>
    /// The downward velocity below which <see cref="BounceSoundId"/> does not play.
    /// </summary>
    public float MinSoundVelocity { get; set; }

    /// <summary>
    /// The downward velocity at and above which <see cref="BounceSoundId"/> plays at full
    /// <see cref="Volume"/>. Not present - per <see cref="FormatProfile.BoulderHasSoundFalloff"/> -
    /// in BFBB's leftover <c>gl/Working</c>/<c>gl/New Folder</c> archives.
    /// </summary>
    public float MaxSoundVelocity { get; set; }

    /// <summary>
    /// The distance from this boulder at which <see cref="BounceSoundId"/> is at full volume.
    /// Only present in <see cref="GameVersion.BFBB"/>, and not there either - per
    /// <see cref="FormatProfile.BoulderHasSoundFalloff"/> - in BFBB's leftover
    /// <c>gl/Working</c>/<c>gl/New Folder</c> archives.
    /// </summary>
    public float InnerRadius { get; set; }

    /// <summary>
    /// The distance from this boulder beyond which <see cref="BounceSoundId"/> is inaudible. Only
    /// present in <see cref="GameVersion.BFBB"/>, and not there either - per
    /// <see cref="FormatProfile.BoulderHasSoundFalloff"/> - in BFBB's leftover
    /// <c>gl/Working</c>/<c>gl/New Folder</c> archives.
    /// </summary>
    public float OuterRadius { get; set; }

    /// <summary>
    /// The distance from this boulder at which <see cref="BounceSoundId"/> falls off to silent. Not
    /// present in <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public float SoundRadius { get; set; }

    /// <summary>
    /// The skeleton bone <see cref="BounceSoundId"/> is played from. Not present in
    /// <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public byte BoneIndex { get; set; }

    /// <summary>
    /// The time, in seconds after spawning, before this boulder can collide with anything.
    /// Only present in <see cref="GameVersion.ROTU"/>.
    /// </summary>
    public float InitialNonCollideTime { get; set; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasAnimList.AnimListId { get => Physical.AnimListId; set => Physical.AnimListId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Boulder"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };
}

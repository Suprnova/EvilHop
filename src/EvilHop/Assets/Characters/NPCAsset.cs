using EvilHop.Common;

namespace EvilHop.Assets;

/// <summary>
/// A non-player character capable of detecting and pursuing the player, lobbing projectiles at it,
/// and using an extender attack.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/NPC">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class NPCAsset() : EntityAsset(AssetType.NPC, baseType: 0x02), IHasModel, Physical.INPCAsset
{
    /// <summary>The distance from this NPC within which the player must be for it to activate.</summary>
    public float ActivateRadius { get; set; }

    /// <summary>The field of view this NPC uses, alongside <see cref="ActivateRadius"/>, to detect the player.</summary>
    public float ActivateFOV { get; set; }

    /// <summary>The height of this NPC's detection volume.</summary>
    public float DetectHeight { get; set; }

    /// <summary>The vertical offset of this NPC's detection volume from its position.</summary>
    public float DetectHeightOffset { get; set; }

    /// <summary>This NPC's movement speed while not pursuing the player.</summary>
    public float SpeedMovement { get; set; }

    /// <summary>This NPC's movement speed while pursuing the player.</summary>
    public float SpeedPursue { get; set; }

    /// <summary>How quickly this NPC turns to face its direction of travel.</summary>
    public float SpeedTurn { get; set; }

    /// <summary>The distance beyond which this NPC gives up pursuing the player.</summary>
    public float PursuitRange { get; set; }

    /// <summary>How long this NPC remains dazed before recovering.</summary>
    public short DazedDuration { get; set; }

    /// <summary>How long this NPC gloats before returning to its normal behavior.</summary>
    public short GloatDuration { get; set; }

    /// <summary>How long this NPC remains gummed before recovering.</summary>
    public short GummedDuration { get; set; }

    /// <summary>How long this NPC remains bubbled before recovering.</summary>
    public short BubbleDuration { get; set; }

    /// <summary>This NPC's hit points.</summary>
    public byte Hitpoints { get; set; }

    /// <summary>This NPC's initial behavior state.</summary>
    public byte BehaviorState { get; set; }

    /// <summary>The speed a projectile lobbed by this NPC travels at.</summary>
    public float LobSpeed { get; set; }

    /// <summary>How long this NPC waits before it may lob another projectile.</summary>
    public float LobDurReload { get; set; }

    /// <summary>The maximum distance this NPC can lob a projectile.</summary>
    public float LobRange { get; set; }

    /// <summary>The number of projectiles this NPC lobs in a single salvo.</summary>
    public uint LobSalvo { get; set; }

    /// <summary>The <see cref="AssetType.Projectile"/> this NPC lobs, if any.</summary>
    public AssetId ProjectileTypeId { get; set; }

    /// <summary>The <see cref="AssetType.MovePoint"/> this NPC aims its lobbed projectiles at, if any.</summary>
    public AssetId BullseyeId { get; set; }

    /// <summary>The coefficient shaping a lobbed projectile's arc.</summary>
    public float LobArcness { get; set; }

    /// <summary>Scales how heavily a lobbed projectile behaves once at rest.</summary>
    public float LobHeavy { get; set; }

    /// <summary>The range of this NPC's extender attack.</summary>
    public float ExtenderRange { get; set; }

    /// <summary>The width of this NPC's extender attack.</summary>
    public float ExtenderWidth { get; set; }

    /// <summary>How long this NPC's extender attack lasts.</summary>
    public float ExtenderDuration { get; set; }

    /// <summary>The rate at which this NPC's extender attack fires.</summary>
    public float ExtenderRate { get; set; }

    /// <summary>How long this NPC waits before its extender attack may fire again.</summary>
    public float ExtenderReloadTime { get; set; }

    /// <summary>The <see cref="AssetType.MovePoint"/> this NPC follows, if any.</summary>
    public AssetId MovePointId { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public int MinPlayerPowerups { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public int MinGameDifficulty { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.INPCAsset Physical => this;

    private uint _villFlags;
    uint Physical.INPCAsset.VillFlags { get => _villFlags; set => _villFlags = value; }

    private AssetId _pathAssetId;
    AssetId Physical.INPCAsset.PathAssetId { get => _pathAssetId; set => _pathAssetId = value; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.NPC"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
    };
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="NPCAsset"/>'s underlying values.
    /// </summary>
    public interface INPCAsset : IEntityAsset
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        uint VillFlags { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        AssetId PathAssetId { get; set; }
    }
}

namespace EvilHop.Assets;

public partial class BoulderAsset
{
    /// <summary>
    /// Flags controlling boulder collision, despawn, and rolling behavior.
    /// </summary>
    [Flags]
    public enum BoulderFlags : uint
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// This boulder survives hits against walls.
        /// </summary>
        CanHitWalls = 1 << 0,
        /// <summary>
        /// This boulder damages the player on contact.
        /// </summary>
        DamagePlayer = 1 << 1,
        /// <summary>
        /// This boulder damages destructible objects on contact.
        /// </summary>
        DamageDestructibleObjects = 1 << 2,
        /// <summary>
        /// This boulder damages NPCs on contact.
        /// </summary>
        DamageNPCs = 1 << 3,
        /// <summary>
        /// This boulder is destroyed (after <see cref="Hitpoints"/> hits) when it touches a
        /// surface with a damaging surface type.
        /// </summary>
        DieOnDamagingSurface = 1 << 4,
        /// <summary>
        /// This boulder is destroyed when it touches an out-of-bounds surface.
        /// </summary>
        DieOnOutOfBoundsSurfaces = 1 << 5,
        /// <summary>
        /// This boulder is destroyed when it touches goo.
        /// </summary>
        DieInGoo = 1 << 6,
        /// <summary>
        /// This boulder reacts to being hit by the player's attack.
        /// </summary>
        DieOnPlayerAttack = 1 << 8,
        /// <summary>
        /// If set, this boulder is destroyed after <see cref="KillTimer"/> seconds. If
        /// unset, this has the same effect as a <see cref="KillTimer"/> of 0 (infinite
        /// lifetime).
        /// </summary>
        DieAfterKillTimer = 1 << 9,
    }
}

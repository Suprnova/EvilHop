using EvilHop.Common;

namespace EvilHop.Assets;

public partial class SurfaceAsset
{
    /// <summary>
    /// Defines the damage and hazard behavior applied to the player upon contacting a surface.
    /// </summary>
    /// <remarks>
    /// Each value is a category every consumer interprets for itself, so values the player cannot tell
    /// apart are not necessarily equivalent elsewhere: <see cref="AssetType.Boulder"/> destroys itself
    /// on <see cref="FatalDeathPlane"/> but merely loses a hit point on every other non-zero value.
    /// </remarks>
    public enum DamageKind : byte
    {
        /// <summary>
        /// Harmless.
        /// </summary>
        None = 0,
        /// <summary>
        /// Kills the player outright.
        /// </summary>
        Fatal1 = 1,
        /// <summary>
        /// Kills the player outright.
        /// </summary>
        Fatal2 = 2,
        /// <summary>
        /// Kills the player outright.
        /// </summary>
        Fatal3 = 3,
        /// <summary>
        /// Costs the player one hit point.
        /// </summary>
        Damage4 = 4,
        /// <summary>
        /// Kills the player outright, and destroys a <see cref="AssetType.Boulder"/> rather than
        /// costing it a hit point.
        /// </summary>
        FatalDeathPlane = 5,
        /// <summary>
        /// Costs the player one hit point.
        /// </summary>
        Damage6 = 6,
    }
}

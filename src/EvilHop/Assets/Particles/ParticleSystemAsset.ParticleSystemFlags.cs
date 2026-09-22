namespace EvilHop.Assets;

public partial class ParticleSystemAsset
{
    /// <summary>
    /// Flags controlling particle system simulation, rendering mode, and lifecycle.
    /// </summary>
    [Flags]
    public enum ParticleSystemFlags : byte
    {
        /// <summary>No flags are set.</summary>
        None = 0,

        /// <summary>This system is visible and its particles are rendered.</summary>
        Visible = 1 << 0,

        /// <summary>When set, particles do not age - their remaining lifetime never counts down.</summary>
        DisableAging = 1 << 1,

        /// <summary>When set, expired particles are not recycled back to life.</summary>
        DisableBack2Life = 1 << 2,

        /// <summary>
        /// Offsets sprite particles toward the camera's right. Takes priority over
        /// <see cref="PivotRightNegative"/> when both are set.
        /// </summary>
        PivotRight = 1 << 3,

        /// <summary>
        /// Offsets sprite particles away from the camera's up. Takes priority over
        /// <see cref="PivotUp"/> when both are set.
        /// </summary>
        PivotUpNegative = 1 << 4,

        /// <summary>
        /// Offsets sprite particles away from the camera's right. Ignored if <see cref="PivotRight"/> is
        /// also set.
        /// </summary>
        PivotRightNegative = 1 << 5,

        /// <summary>
        /// Offsets sprite particles toward the camera's up. Ignored if <see cref="PivotUpNegative"/> is
        /// also set.
        /// </summary>
        PivotUp = 1 << 6,

        /// <summary>
        /// Renders this system's particles through the particle tank rendering path instead of the
        /// default sprite loop.
        /// </summary>
        UsePTankRender = 1 << 7,
    }
}

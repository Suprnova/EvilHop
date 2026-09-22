namespace EvilHop.Assets;

public partial class ParticleInterpolation
{
    /// <summary>
    /// Defines the interpolation curve used to transition particle properties over their lifetime.
    /// </summary>
    /// <remarks>
    /// Real archives almost always store this as a BKDR hash of the mode's name; a raw value below 8
    /// selecting the same order is also accepted by the engine and appears in at least one archive
    /// checked, with <see cref="Time"/> reachable only that way - hashing the name <c>"Time"</c> does not
    /// map to it.
    /// </remarks>
    public enum ParticleInterpolationMode : uint
    {
        /// <summary>Always <see cref="Start"/>.</summary>
        ConstA = 0x48E48E7A,

        /// <summary>Always <see cref="End"/>.</summary>
        ConstB = 0x48E48E7B,

        /// <summary>
        /// A new random value between <see cref="Start"/> and
        /// <see cref="End"/>, refreshed every <see cref="Frequency"/> seconds.
        /// </summary>
        Random = 0x0FE111BF,

        /// <summary>
        /// Linearly interpolates between <see cref="Start"/> and
        /// <see cref="End"/> at <see cref="Frequency"/>.
        /// </summary>
        Linear = 0xB7353B79,

        /// <summary>
        /// Interpolates between <see cref="Start"/> and
        /// <see cref="End"/> using a sine curve at <see cref="InverseFrequency"/>.
        /// </summary>
        Sine = 0x0B326F01,

        /// <summary>
        /// Interpolates between <see cref="Start"/> and
        /// <see cref="End"/> using a cosine curve at <see cref="InverseFrequency"/>.
        /// </summary>
        Cosine = 0x498D7119,

        /// <summary>
        /// The current time of the interpolation. Unused - not reachable by hashing a mode name, and
        /// never observed in a real archive.
        /// </summary>
        /// TODO: validate against decompiled source
        Time = 6,

        /// <summary>
        /// <see cref="Start"/> until <c>time * </c><see cref="Frequency"/>
        /// reaches 0.5, then <see cref="End"/>.
        /// </summary>
        Step = 0x0B354BD4,
    }
}

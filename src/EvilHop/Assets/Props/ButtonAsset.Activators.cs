namespace EvilHop.Assets;

public partial class ButtonAsset
{
    /// <summary>
    /// Flags selecting what can press a <see cref="ButtonAsset"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every flag applies to every <see cref="ButtonKind"/>, but they come in two sorts. Most describe
    /// a single hit, pressing the button once. The flags describing something resting on top of the
    /// button (<see cref="PlayerStanding"/>, <see cref="EnemyStanding"/>, <see cref="BoulderResting"/>,
    /// <see cref="StoneTikiResting"/>, and <see cref="FruitResting"/>) press it continuously instead,
    /// holding a <see cref="ButtonKind.PressurePlate"/> down for as long as they stay.
    /// </para>
    /// <para>
    /// A single hit presses a <see cref="ButtonKind.PressurePlate"/> only momentarily, as nothing
    /// keeps holding it down afterwards.
    /// </para>
    /// </remarks>
    [Flags]
    public enum Activators : uint
    {
        /// <summary>
        /// Nothing presses the button; it only responds to events.
        /// </summary>
        None = 0,
        /// <summary>
        /// SpongeBob's bubble spin, or a slide into the button.
        /// </summary>
        /// TODO: validate against decompiled source
        BubbleSpin = 1 << 0,
        /// <summary>
        /// SpongeBob's bubble bounce.
        /// </summary>
        /// TODO: validate against decompiled source
        BubbleBounce = 1 << 1,
        /// <summary>
        /// SpongeBob's bubble bash.
        /// </summary>
        /// TODO: validate against decompiled source
        BubbleBash = 1 << 2,
        /// <summary>
        /// A rolling <see cref="BoulderAsset"/>, including SpongeBob's bubble bowl.
        /// </summary>
        Boulder = 1 << 3,
        /// <summary>
        /// SpongeBob's cruise bubble.
        /// </summary>
        CruiseBubble = 1 << 4,
        /// <summary>
        /// The player on a bungee.
        /// </summary>
        /// TODO: validate against decompiled source
        Bungee = 1 << 5,
        /// <summary>
        /// A thrown enemy, Tiki, or <see cref="DestructibleObjectAsset"/>.
        /// </summary>
        ThrownObject = 1 << 6,
        /// <summary>
        /// Thrown fruit.
        /// </summary>
        ThrownFruit = 1 << 7,
        /// <summary>
        /// Patrick's belly slam.
        /// </summary>
        /// TODO: validate against decompiled source
        PatrickSlam = 1 << 8,
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 1 << 9,
        /// <summary>
        /// The player standing on the button.
        /// </summary>
        PlayerStanding = 1 << 10,
        /// <summary>
        /// An enemy standing on the button.
        /// </summary>
        EnemyStanding = 1 << 11,
        /// <summary>
        /// A <see cref="BoulderAsset"/> resting on the button.
        /// </summary>
        BoulderResting = 1 << 12,
        /// <summary>
        /// A stone Tiki resting on the button.
        /// </summary>
        StoneTikiResting = 1 << 13,
        /// <summary>
        /// Sandy's melee attack, or her slide.
        /// </summary>
        /// TODO: validate against decompiled source
        SandyMelee = 1 << 14,
        /// <summary>
        /// Patrick's melee attack, or his slide.
        /// </summary>
        /// TODO: validate against decompiled source
        PatrickMelee = 1 << 15,
        /// <summary>
        /// Thrown fruit resting on the button.
        /// </summary>
        /// TODO: validate against decompiled source
        FruitResting = 1 << 16,
        /// <summary>
        /// Patrick's cartwheel.
        /// </summary>
        /// TODO: validate against decompiled source
        PatrickCartwheel = 1 << 17,
    }
}

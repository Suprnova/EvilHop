namespace EvilHop.Assets;

public partial class PickupAsset
{
    /// <summary>
    /// Defines the collectible item type and reward behavior for a pickup.
    /// </summary>
    public enum PickupKind : byte
    {
        /// <summary>Artwork.</summary>
        Artwork = 0x10,

        /// <summary>Underwear.</summary>
        Underwear = 0x13,

        /// <summary>A sock.</summary>
        Sock = 0x24,

        /// <summary>A steering wheel.</summary>
        SteeringWheel = 0x27,

        /// <summary>A clue.</summary>
        Clue = 0x28,

        /// <summary>Golden underwear.</summary>
        GoldenUnderwear = 0x2E,

        /// <summary>A green shiny object.</summary>
        GreenShinyObject = 0x34,

        /// <summary>A yellow shiny object.</summary>
        YellowShinyObject = 0x3B,

        /// <summary>A red shiny object.</summary>
        RedShinyObject = 0x3E,

        /// <summary>A SpongeBall.</summary>
        SpongeBall = 0x40,

        /// <summary>A savepoint.</summary>
        Savepoint = 0x5C,

        /// <summary>A shovel.</summary>
        Shovel = 0x80,

        /// <summary>A blue shiny object.</summary>
        BlueShinyObject = 0x81,

        /// <summary>A snack gate, using <see cref="PickupValue"/> as its Scooby Snack count.</summary>
        SnackGate = 0x86,

        /// <summary>A power crystal.</summary>
        PowerCrystal = 0xBB,

        /// <summary>A Scooby Snack.</summary>
        ScoobySnack = 0xBC,

        /// <summary>A purple shiny object.</summary>
        PurpleShinyObject = 0xCB,

        /// <summary>A golden spatula.</summary>
        GoldenSpatula = 0xDD,

        /// <summary>A box of Scooby Snacks.</summary>
        ScoobySnackBox = 0xEC,
    }
}

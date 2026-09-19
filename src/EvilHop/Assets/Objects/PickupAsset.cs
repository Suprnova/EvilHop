using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A collectible object in the game world. Its model and animation always come from
/// <c>pickups.MINF</c>, and its gameplay behavior - reward, sound, screen effects - is looked up in
/// <c>boot.hip</c>'s <see cref="AssetType.PickupTable"/> by <see cref="PickupHash"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PKUP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class PickupAsset() : EntityAsset(AssetType.Pickup, baseType: 0x04)
{
    /// <summary>
    /// Which kind of pickup this is.
    /// </summary>
    public PickupKind Kind { get; set; }

    private protected override byte Subtype { get => (byte)Kind; set => Kind = (PickupKind)value; }

    /// <summary>
    /// The hash <see cref="AssetType.PickupTable"/> entries are looked up by, matching one entry's
    /// own hash of a hardcoded pickup name.
    /// </summary>
    public uint PickupHash { get; set; }

    /// <summary>
    /// This pickup's flags.
    /// </summary>
    public PickupFlags Flags { get; set; }

    /// <summary>
    /// A context-dependent value: the Scooby Snack count for a snack gate, or 4 for most other
    /// pickup kinds.
    /// </summary>
    public short PickupValue { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Pickup"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
    };

    internal static PickupAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new PickupAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        asset.PickupHash = reader.ReadUInt32();
        asset.Flags = (PickupFlags)reader.ReadInt16();
        asset.PickupValue = reader.ReadInt16();

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PickupAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        writer.Write(asset.PickupHash);
        writer.Write((short)asset.Flags);
        writer.Write(asset.PickupValue);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// Flags controlling pickup spawning, persistence, and collection state.
/// </summary>
[Flags]
public enum PickupFlags : short
{
    /// <summary>No flags are set.</summary>
    None = 0,

    /// <summary>The pickup reappears after being collected, rather than staying gone.</summary>
    ReappearAfterCollecting = 1 << 0,

    /// <summary>The pickup can be collided with and collected as soon as the scene loads.</summary>
    EnabledOnStart = 1 << 1,
}

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

    /// <summary>A snack gate, using <see cref="PickupAsset.PickupValue"/> as its Scooby Snack count.</summary>
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

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
public sealed partial class PickupAsset() : EntityAsset(AssetType.Pickup, baseType: 0x04)
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

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
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

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
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
}

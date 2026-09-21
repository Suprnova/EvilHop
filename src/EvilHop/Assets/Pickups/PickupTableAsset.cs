using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Defines every <see cref="AssetType.Pickup"/> a level can spawn - its model, animation, and default
/// quantity - keyed by a name hash. Normally has only one instance in the whole game, in <c>boot.hip</c>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PICK">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class PickupTableAsset() : Asset(AssetType.PickupTable), IPhysicalPickupTableAsset
{
    /// <summary>
    /// The table's entries, one per pickup kind.
    /// </summary>
    public Collection<PickupTableEntry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalPickupTableAsset Physical => this;

    private uint _magic = 0x4B434950; // "KCIP"
    uint IPhysicalPickupTableAsset.Magic { get => _magic; set => _magic = value; }

    private uint? _overriddenEntryCount;
    uint IPhysicalPickupTableAsset.EntryCount
    {
        get => _overriddenEntryCount ?? (uint)Entries.Count;
        set => _overriddenEntryCount = value == (uint)Entries.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.PickupTable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static PickupTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new PickupTableAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Physical.Magic = reader.ReadUInt32();
        uint entryCount = reader.ReadUInt32();
        for (uint i = 0; i < entryCount; i++)
            asset.Entries.Add(PickupTableEntry.Read(reader, profile));
        asset.Physical.EntryCount = (uint)asset.Entries.Count;

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PickupTableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Magic);
        writer.Write(asset.Physical.EntryCount);
        foreach (var entry in asset.Entries)
            PickupTableEntry.Write(entry, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="PickupTableAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalPickupTableAsset : IPhysicalAsset
{
    /// <summary>
    /// A four-character magic number.
    /// </summary>
    uint Magic { get; set; }

    /// <summary>
    /// The number of <see cref="PickupTableAsset.Entries"/> stored for this asset, read directly
    /// from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="PickupTableAsset.Entries"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint EntryCount { get; set; }
}

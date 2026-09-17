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

    internal static PickupTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new PickupTableAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Physical.Magic = reader.ReadUInt32();
        uint entryCount = reader.ReadUInt32();
        for (uint i = 0; i < entryCount; i++)
        {
            asset.Entries.Add(new PickupTableEntry
            {
                PickupHash = reader.ReadUInt32(),
                PickupType = reader.ReadByte(),
                PickupIndex = reader.ReadByte(),
                Flags = (ushort)reader.ReadInt16(),
                Quantity = reader.ReadUInt32(),
                ModelId = reader.ReadAssetId(),
                AnimId = reader.ReadAssetId(),
            });
        }
        asset.Physical.EntryCount = (uint)asset.Entries.Count;

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PickupTableAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write(asset.Physical.Magic);
        writer.Write(asset.Physical.EntryCount);
        foreach (var entry in asset.Entries)
        {
            writer.Write(entry.PickupHash);
            writer.Write(entry.PickupType);
            writer.Write(entry.PickupIndex);
            writer.Write((short)entry.Flags);
            writer.Write(entry.Quantity);
            writer.Write(entry.ModelId);
            writer.Write(entry.AnimId);
        }

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

/// <summary>
/// One <see cref="PickupTableAsset"/> entry, describing a single kind of pickup.
/// </summary>
public sealed class PickupTableEntry
{
    /// <summary>
    /// The hash <see cref="AssetType.Pickup"/> assets use to identify this pickup kind.
    /// </summary>
    public uint PickupHash { get; set; }

    /// <summary>
    /// Unknown. Usually 0xCD (uninitialized) on disk - the retail game overwrites it at load for
    /// every entry whose <see cref="PickupHash"/> matches one of its own hardcoded pickup names.
    /// </summary>
    public byte PickupType { get; set; }

    /// <summary>
    /// Unknown. Usually 0xCD (uninitialized) on disk, overwritten the same way as
    /// <see cref="PickupType"/>.
    /// </summary>
    public byte PickupIndex { get; set; }

    /// <summary>
    /// Unknown. Usually 0.
    /// </summary>
    public ushort Flags { get; set; }

    /// <summary>
    /// How many of this pickup are granted at once. Usually 1.
    /// </summary>
    public uint Quantity { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> or <see cref="AssetType.ModelInfo"/> this pickup displays as.
    /// </summary>
    public AssetId ModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/> or <see cref="AssetType.AnimationList"/> this pickup
    /// plays, if any. Usually <see cref="AssetId.None"/>.
    /// </summary>
    public AssetId AnimId { get; set; }
}

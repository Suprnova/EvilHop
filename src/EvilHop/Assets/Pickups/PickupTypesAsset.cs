using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A table defining information regarding dynamic pickups, keyed by type hash.
/// Usually has only one instance in the entire game (in <c>boot.hip</c>).
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/TPIK">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class PickupTypesAsset() : BaseAsset(AssetType.PickupTypes, baseType: 0x00), Physical.IPickupTypesAsset
{
    /// <summary>
    /// The table's format version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The pickup type entries in the table.
    /// </summary>
    public Collection<Entry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IPickupTypesAsset Physical => this;

    private int? _overriddenRowCount;
    int Physical.IPickupTypesAsset.RowCount
    {
        get => _overriddenRowCount ?? Entries.Count;
        set => _overriddenRowCount = value == Entries.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.PickupTypes"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static PickupTypesAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new PickupTypesAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Version = reader.ReadInt32();
        int rowCount = reader.ReadInt32();
        for (int i = 0; i < rowCount; i++)
            asset.Entries.Add(Entry.Read(reader, profile));

        asset.Physical.RowCount = rowCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PickupTypesAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Version);
        writer.Write(asset.Physical.RowCount);
        foreach (var entry in asset.Entries)
            Entry.Write(entry, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="PickupTypesAsset"/>'s underlying values.
    /// </summary>
    public interface IPickupTypesAsset : IBaseAsset
    {
        /// <summary>
        /// The number of <see cref="PickupTypesAsset.Entries"/> stored for this asset, read directly
        /// from its leading count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="PickupTypesAsset.Entries"/>.Count exist, this field wins
        /// during serialization.
        /// </remarks>
        int RowCount { get; set; }
    }
}

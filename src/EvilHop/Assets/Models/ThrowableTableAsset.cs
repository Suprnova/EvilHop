using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A table mapping model IDs to throwable properties such as behavior type, shrapnel, damage, and blast radius.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/TRWT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class ThrowableTableAsset() : BaseAsset(AssetType.ThrowableTable, baseType: 0x00), Physical.IThrowableTableAsset
{
    /// <summary>
    /// The table's format version. Version 3 includes <see cref="Entry.DamageRadius"/>,
    /// while version 2 (used in prototypes) omits it.
    /// </summary>
    public int Version { get; set; } = 3;

    /// <summary>
    /// The throwable rows in the table.
    /// </summary>
    public Collection<Entry> Rows { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IThrowableTableAsset Physical => this;

    private int? _overriddenRowCount;
    int Physical.IThrowableTableAsset.RowCount
    {
        get => _overriddenRowCount ?? Rows.Count;
        set => _overriddenRowCount = value == Rows.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.ThrowableTable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };

    internal static ThrowableTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ThrowableTableAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Version = reader.ReadInt32();
        int rowCount = reader.ReadInt32();
        for (int i = 0; i < rowCount; i++)
            asset.Rows.Add(Entry.Read(reader, profile, asset.Version));

        asset.Physical.RowCount = rowCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ThrowableTableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Version);
        writer.Write(asset.Physical.RowCount);
        foreach (var row in asset.Rows)
            Entry.Write(row, writer, profile, asset.Version);

        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="ThrowableTableAsset"/>'s underlying values.
    /// </summary>
    public interface IThrowableTableAsset : IBaseAsset
    {
        /// <summary>
        /// The number of <see cref="ThrowableTableAsset.Rows"/> stored for this asset, read directly
        /// from its leading count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="ThrowableTableAsset.Rows"/>.Count exist, this field wins
        /// during serialization.
        /// </remarks>
        int RowCount { get; set; }
    }
}

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
public sealed class ThrowableTableAsset() : BaseAsset(AssetType.ThrowableTable, baseType: 0x00), IPhysicalThrowableTableAsset
{
    /// <summary>
    /// The table's format version. Version 3 includes <see cref="ThrowableTableRow.DamageRadius"/>,
    /// while version 2 (used in prototypes) omits it.
    /// </summary>
    public int Version { get; set; } = 3;

    /// <summary>
    /// The throwable rows in the table.
    /// </summary>
    public Collection<ThrowableTableRow> Rows { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalThrowableTableAsset Physical => this;

    private int? _overriddenRowCount;
    int IPhysicalThrowableTableAsset.RowCount
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

    internal static ThrowableTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new ThrowableTableAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Version = reader.ReadInt32();
        int rowCount = reader.ReadInt32();
        for (int i = 0; i < rowCount; i++)
        {
            var row = new ThrowableTableRow
            {
                ModelId = reader.ReadAssetId(),
                Type = reader.ReadUInt32(),
                ShrapnelId = reader.ReadAssetId(),
                Damage = reader.ReadInt32(),
            };
            if (asset.Version >= 3)
            {
                row.DamageRadius = reader.ReadSingle();
            }
            asset.Rows.Add(row);
        }

        asset.Physical.RowCount = rowCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ThrowableTableAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Version);
        writer.Write(asset.Physical.RowCount);
        foreach (var row in asset.Rows)
        {
            writer.Write(row.ModelId);
            writer.Write(row.Type);
            writer.Write(row.ShrapnelId);
            writer.Write(row.Damage);
            if (asset.Version >= 3)
            {
                writer.Write(row.DamageRadius);
            }
        }

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="ThrowableTableAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalThrowableTableAsset : IPhysicalBaseAsset
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

/// <summary>
/// One <see cref="ThrowableTableAsset"/> row, defining properties for a single throwable model.
/// </summary>
public sealed class ThrowableTableRow
{
    /// <summary>
    /// The <see cref="AssetType.Model"/> used for the throwable object.
    /// </summary>
    public AssetId ModelId { get; set; }

    /// <summary>
    /// The throwable behavior type index.
    /// </summary>
    public uint Type { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Shrapnel"/> spawned when the throwable object breaks.
    /// </summary>
    public AssetId ShrapnelId { get; set; }

    /// <summary>
    /// The amount of damage dealt on impact.
    /// </summary>
    public int Damage { get; set; }

    /// <summary>
    /// The blast radius of the damage on impact. Not present in version 2 assets.
    /// </summary>
    public float DamageRadius { get; set; }
}

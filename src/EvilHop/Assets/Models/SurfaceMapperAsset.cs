using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Assigns a <see cref="AssetType.Surface"/> to specific parts of a <see cref="AssetType.JSP"/>'s
/// mesh, by matching each entry's <see cref="SurfaceMapperEntry.MaterialIndex"/> against the JSP
/// info nodes' own material index.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/MAPR">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SurfaceMapperAsset() : Asset(AssetType.SurfaceMapper), IPhysicalSurfaceMapperAsset
{
    /// <summary>The asset's entries, each assigning one surface to a JSP material index.</summary>
    public Collection<SurfaceMapperEntry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalSurfaceMapperAsset Physical => this;

    private AssetId? _overriddenSelfId;
    AssetId IPhysicalSurfaceMapperAsset.SelfId
    {
        get => _overriddenSelfId ?? Id;
        set => _overriddenSelfId = value == Id ? null : value;
    }

    private uint? _overriddenCount;
    uint IPhysicalSurfaceMapperAsset.Count
    {
        get => _overriddenCount ?? (uint)Entries.Count;
        set => _overriddenCount = value == (uint)Entries.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SurfaceMapper"/> is known to be read by.
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

    internal static SurfaceMapperAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new SurfaceMapperAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Physical.SelfId = reader.ReadAssetId();
        uint count = reader.ReadUInt32();

        for (int i = 0; i < count; i++)
        {
            asset.Entries.Add(new SurfaceMapperEntry
            {
                SurfaceId = reader.ReadAssetId(),
                MaterialIndex = reader.ReadUInt32(),
            });
        }

        asset.Physical.Count = (uint)asset.Entries.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SurfaceMapperAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write(asset.Physical.SelfId);
        writer.Write(asset.Physical.Count);

        foreach (var entry in asset.Entries)
        {
            writer.Write(entry.SurfaceId);
            writer.Write(entry.MaterialIndex);
        }

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="SurfaceMapperAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalSurfaceMapperAsset : IPhysicalAsset
{
    /// <summary>
    /// The asset's own ID, stored a second time within its own data.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="Asset.Id"/> exist, this field wins during serialization.
    /// </remarks>
    AssetId SelfId { get; set; }

    /// <summary>
    /// The number of <see cref="SurfaceMapperAsset.Entries"/> stored for this asset, read directly
    /// from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="SurfaceMapperAsset.Entries"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint Count { get; set; }
}

/// <summary>
/// One <see cref="SurfaceMapperAsset"/> entry, assigning <see cref="SurfaceId"/> to every JSP info
/// node whose own material index matches <see cref="MaterialIndex"/>.
/// </summary>
/// <remarks>
/// In <see cref="GameVersion.TSSM"/> and later, <see cref="MaterialIndex"/> is instead a BKDR hash of
/// the target JSP info's asset name concatenated with its index, matching more than one JSP at once.
/// </remarks>
public sealed class SurfaceMapperEntry
{
    /// <summary>The <see cref="AssetType.Surface"/> applied to matching JSP info nodes.</summary>
    public AssetId SurfaceId { get; set; }

    /// <summary>
    /// The value matched against a JSP info node's own material index to decide whether
    /// <see cref="SurfaceId"/> applies to it.
    /// </summary>
    public uint MaterialIndex { get; set; }
}

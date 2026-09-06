using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Defines, for each of a set of base models, up to three lower-detail replacements to swap to as
/// the model moves farther from the camera, and the distance beyond which it fades out of view
/// entirely.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/LODT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class LODTableAsset : Asset, IPhysicalLODTableAsset
{
    /// <summary>
    /// The table's entries, each mapping one base model to its levels of detail.
    /// </summary>
    public Collection<LODTableEntry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalLODTableAsset Physical => this;

    private int? _overriddenCount;
    int IPhysicalLODTableAsset.Count
    {
        get => _overriddenCount ?? Entries.Count;
        // prevents equivalent count assignments from being interpretted as an "override"
        set => _overriddenCount = value == Entries.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.LODTable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };

    internal LODTableAsset() { }

    internal static LODTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new LODTableAsset();
        AssetFields.Populate(asset, header, debug);

        bool hasFlags = profile.Game != GameVersion.BFBB;
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            var entry = new LODTableEntry
            {
                BaseModelId = reader.ReadAssetId(),
                NoRenderDistance = reader.ReadSingle(),
                Flags = hasFlags ? reader.ReadUInt32() : 0,
                Lod1ModelId = reader.ReadAssetId(),
                Lod2ModelId = reader.ReadAssetId(),
                Lod3ModelId = reader.ReadAssetId(),
                Lod1Distance = reader.ReadSingle(),
                Lod2Distance = reader.ReadSingle(),
                Lod3Distance = reader.ReadSingle(),
            };
            asset.Entries.Add(entry);
        }

        asset.Physical.Count = asset.Entries.Count; // now agrees - lets it derive
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(LODTableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        bool hasFlags = profile.Game != GameVersion.BFBB;
        writer.Write(asset.Physical.Count);
        foreach (var entry in asset.Entries)
        {
            writer.Write(entry.BaseModelId);
            writer.Write(entry.NoRenderDistance);
            if (hasFlags) writer.Write(entry.Flags);
            writer.Write(entry.Lod1ModelId);
            writer.Write(entry.Lod2ModelId);
            writer.Write(entry.Lod3ModelId);
            writer.Write(entry.Lod1Distance);
            writer.Write(entry.Lod2Distance);
            writer.Write(entry.Lod3Distance);
        }
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="LODTableAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalLODTableAsset : IPhysicalAsset
{
    /// <summary>
    /// The number of <see cref="LODTableAsset.Entries"/> stored for this asset, read directly from
    /// its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="LODTableAsset.Entries"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    int Count { get; set; }
}

/// <summary>
/// One <see cref="LODTableAsset"/> entry: a base model plus up to three lower-detail replacements
/// for it, each swapped in once the camera passes its associated distance.
/// </summary>
public sealed class LODTableEntry
{
    /// <summary>
    /// The <see cref="AssetType.Model"/> used while nearer than <see cref="Lod1Distance"/>, or for
    /// the entry's entire range if no LOD levels are set.
    /// </summary>
    public AssetId BaseModelId { get; set; }

    /// <summary>
    /// The distance from the camera beyond which the model fades out of view entirely.
    /// </summary>
    public float NoRenderDistance { get; set; }

    /// <summary>
    /// Unknown. Not present in <see cref="GameVersion.BFBB"/>..
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> used once farther than <see cref="Lod1Distance"/>, if any.
    /// </summary>
    public AssetId Lod1ModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> used once farther than <see cref="Lod2Distance"/>, if any.
    /// </summary>
    public AssetId Lod2ModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> used once farther than <see cref="Lod3Distance"/>, if any.
    /// </summary>
    public AssetId Lod3ModelId { get; set; }

    /// <summary>
    /// The distance from the camera beyond which <see cref="Lod1ModelId"/> replaces
    /// <see cref="BaseModelId"/>.
    /// </summary>
    public float Lod1Distance { get; set; }

    /// <summary>
    /// The distance from the camera beyond which <see cref="Lod2ModelId"/> replaces
    /// <see cref="Lod1ModelId"/>.
    /// </summary>
    public float Lod2Distance { get; set; }

    /// <summary>
    /// The distance from the camera beyond which <see cref="Lod3ModelId"/> replaces
    /// <see cref="Lod2ModelId"/>.
    /// </summary>
    public float Lod3Distance { get; set; }
}

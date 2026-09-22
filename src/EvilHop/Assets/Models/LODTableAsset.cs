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
public sealed partial class LODTableAsset() : Asset(AssetType.LODTable), Physical.ILODTableAsset
{
    /// <summary>
    /// The table's entries, each mapping one base model to its levels of detail.
    /// </summary>
    public Collection<Entry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ILODTableAsset Physical => this;

    private int? _overriddenCount;
    int Physical.ILODTableAsset.Count
    {
        get => _overriddenCount ?? Entries.Count;
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

    internal static LODTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new LODTableAsset();
        AssetFields.Populate(asset, header, debug);

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
            asset.Entries.Add(Entry.Read(reader, profile));

        asset.Physical.Count = count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(LODTableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Count);
        foreach (var entry in asset.Entries)
            Entry.Write(entry, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="LODTableAsset"/>'s underlying values.
    /// </summary>
    public interface ILODTableAsset : IAsset
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
}

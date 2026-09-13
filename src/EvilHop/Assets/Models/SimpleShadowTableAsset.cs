using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Assigns level objects a separate model to render as their dropped shadow.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SHDW">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SimpleShadowTableAsset() : Asset(AssetType.SimpleShadowTable)
{
    /// <summary>
    /// The table's entries.
    /// </summary>
    public Collection<SimpleShadowTableEntry> Entries { get; } = [];

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SimpleShadowTable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
    };

    internal static SimpleShadowTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new SimpleShadowTableAsset();
        AssetFields.Populate(asset, header, debug);

        var count = reader.ReadUInt32();
        for (var i = 0; i < count; i++)
        {
            asset.Entries.Add(new SimpleShadowTableEntry
            {
                ModelId = reader.ReadAssetId(),
                ShadowModelId = reader.ReadAssetId(),
                Unknown = reader.ReadUInt32(),
            });
        }

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SimpleShadowTableAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write((uint)asset.Entries.Count);
        foreach (var entry in asset.Entries)
        {
            writer.Write(entry.ModelId);
            writer.Write(entry.ShadowModelId);
            writer.Write(entry.Unknown);
        }

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// One <see cref="SimpleShadowTableAsset"/> entry, assigning a shadow model to a single
/// <see cref="AssetType.Model"/>.
/// </summary>
public record struct SimpleShadowTableEntry
{
    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> this entry applies to.
    /// </summary>
    public AssetId ModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> rendered as <see cref="ModelId"/>'s
    /// shadow.
    /// </summary>
    public AssetId ShadowModelId { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public uint Unknown { get; set; }
}

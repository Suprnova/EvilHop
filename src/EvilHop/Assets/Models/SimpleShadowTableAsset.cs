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
public sealed class SimpleShadowTableAsset() : Asset(AssetType.SimpleShadowTable), Physical.ISimpleShadowTableAsset
{
    /// <summary>
    /// The table's entries.
    /// </summary>
    public Collection<SimpleShadowTableEntry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ISimpleShadowTableAsset Physical => this;

    private uint? _overriddenCount;
    uint Physical.ISimpleShadowTableAsset.Count
    {
        get => _overriddenCount ?? (uint)Entries.Count;
        set => _overriddenCount = value == (uint)Entries.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SimpleShadowTable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
    };

    internal static SimpleShadowTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SimpleShadowTableAsset();
        AssetFields.Populate(asset, header, debug);

        var count = reader.ReadUInt32();
        for (var i = 0; i < count; i++)
            asset.Entries.Add(SimpleShadowTableEntry.Read(reader, profile));

        asset.Physical.Count = count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SimpleShadowTableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Count);
        foreach (var entry in asset.Entries)
            SimpleShadowTableEntry.Write(entry, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="SimpleShadowTableAsset"/>'s underlying values.
    /// </summary>
    public interface ISimpleShadowTableAsset : IAsset
    {
        /// <summary>
        /// The number of <see cref="SimpleShadowTableAsset.Entries"/> stored for this asset, read directly from
        /// its leading count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="SimpleShadowTableAsset.Entries"/>.Count exist, this field wins during serialization.
        /// </remarks>
        uint Count { get; set; }
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

    internal static SimpleShadowTableEntry Read(EndianReader reader, FormatProfile _) => new()
    {
        ModelId = reader.ReadAssetId(),
        ShadowModelId = reader.ReadAssetId(),
        Unknown = reader.ReadUInt32(),
    };

    internal static void Write(SimpleShadowTableEntry value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.ModelId);
        writer.Write(value.ShadowModelId);
        writer.Write(value.Unknown);
    }
}

using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Supplies additional rendering information - blending, culling, lighting, and depth-testing - for
/// level objects, applied to an entire <see cref="AssetType.Model"/> asset or a subset of its atomics.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PIPT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class PipeInfoTableAsset() : Asset(AssetType.PipeInfoTable), Physical.IPipeInfoTableAsset
{
    /// <summary>
    /// The table's entries, each applying rendering information to one <see cref="AssetType.Model"/>
    /// asset or a subset of its atomics.
    /// </summary>
    public Collection<Entry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IPipeInfoTableAsset Physical => this;

    private int? _overriddenCount;
    int Physical.IPipeInfoTableAsset.Count
    {
        get => _overriddenCount ?? Entries.Count;
        set => _overriddenCount = value == Entries.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.PipeInfoTable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static PipeInfoTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new PipeInfoTableAsset();
        AssetFields.Populate(asset, header, debug);

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
            asset.Entries.Add(Entry.Read(reader, profile));

        asset.Physical.Count = count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PipeInfoTableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Count);
        foreach (var entry in asset.Entries)
            Entry.Write(entry, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// Defines the lighting mode applied to a model's selected atomics.
    /// </summary>
    public enum LightingMode : byte
    {
        /// <summary>Lit by the level's light kit only.</summary>
        LightKitOnly = 0,
        /// <summary>Lit by prebaked vertex lighting only.</summary>
        PrelightOnly = 1,
        /// <summary>Lit by both the level's light kit and prebaked vertex lighting.</summary>
        LightKitAndPrelight = 2,
        /// <summary>Unknown.</summary>
        Unknown = 3,
    }

    /// <summary>
    /// Defines the face culling mode applied to a model's selected atomics.
    /// </summary>
    public enum CullMode : byte
    {
        /// <summary>Unknown.</summary>
        Unknown = 0,
        /// <summary>No culling; both front and back faces are rendered.</summary>
        None = 1,
        /// <summary>Back-face culling.</summary>
        Back = 2,
        /// <summary>Rendered twice: once with front-face culling, then once with back-face culling.</summary>
        Dual = 3,
    }

    /// <summary>
    /// Defines the depth-buffer write behavior applied to a model's selected atomics.
    /// </summary>
    public enum ZWriteMode : byte
    {
        /// <summary>Z-write is enabled.</summary>
        Enabled = 0,
        /// <summary>Z-write is disabled.</summary>
        Disabled = 1,
        /// <summary>Rendered twice: once with z-write disabled, then once with z-write enabled.</summary>
        Dual = 2,
        /// <summary>Unknown.</summary>
        Unknown = 3,
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="PipeInfoTableAsset"/>'s underlying values.
    /// </summary>
    public interface IPipeInfoTableAsset : IAsset
    {
        /// <summary>
        /// The number of <see cref="PipeInfoTableAsset.Entries"/> stored for this asset, read directly
        /// from its leading count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="PipeInfoTableAsset.Entries"/>.Count exist, this field wins
        /// during serialization.
        /// </remarks>
        int Count { get; set; }
    }
}

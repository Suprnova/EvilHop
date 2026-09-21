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
public sealed class PipeInfoTableAsset() : Asset(AssetType.PipeInfoTable), IPhysicalPipeInfoTableAsset
{
    /// <summary>
    /// The table's entries, each applying rendering information to one <see cref="AssetType.Model"/>
    /// asset or a subset of its atomics.
    /// </summary>
    public Collection<PipeInfoEntry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalPipeInfoTableAsset Physical => this;

    private int? _overriddenCount;
    int IPhysicalPipeInfoTableAsset.Count
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
        {
            var entry = new PipeInfoEntry
            {
                ModelId = reader.ReadAssetId(),
                SubObjectBits = reader.ReadUInt32(),
                Flags = new PipeRenderFlags(reader.ReadUInt32()),
            };

            if (profile.Game is not GameVersion.BFBB)
            {
                entry.Layer = (PipeLayer)reader.ReadByte();
                entry.AlphaDiscard = reader.ReadByte();
                reader.ReadInt16(); // padding, always zero
            }

            asset.Entries.Add(entry);
        }

        asset.Physical.Count = count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PipeInfoTableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Count);
        foreach (var entry in asset.Entries)
        {
            writer.Write(entry.ModelId);
            writer.Write(entry.SubObjectBits);
            writer.Write(entry.Flags.Value);

            if (profile.Game is not GameVersion.BFBB)
            {
                writer.Write((byte)entry.Layer);
                writer.Write(entry.AlphaDiscard);
                writer.Write((short)0); // padding
            }
        }
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="PipeInfoTableAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalPipeInfoTableAsset : IPhysicalAsset
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

/// <summary>
/// One <see cref="PipeInfoTableAsset"/> entry, applying rendering information to a <see
/// cref="AssetType.Model"/> asset or a subset of its atomics.
/// </summary>
public sealed class PipeInfoEntry
{
    /// <summary>
    /// The <see cref="AssetType.Model"/> asset this entry applies rendering information to.
    /// </summary>
    public AssetId ModelId { get; set; }

    /// <summary>
    /// A bitmask selecting which of <see cref="ModelId"/>'s RenderWare atomics this entry applies to,
    /// one bit per atomic starting with the model's last atomic as the least significant bit (0x1)
    /// and ending with its first atomic as the most significant bit. 0xFFFFFFFF applies the entry to
    /// every atomic.
    /// </summary>
    public uint SubObjectBits { get; set; }

    /// <summary>
    /// Rendering flags applied to the selected atomics - alpha compare, fog, blending, lighting,
    /// culling, and depth-testing.
    /// </summary>
    public PipeRenderFlags Flags { get; set; }

    /// <summary>
    /// When to draw the selected atomics relative to other transparent geometry. Not present in
    /// <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public PipeLayer Layer { get; set; }

    /// <summary>
    /// Unknown. Not present in <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public byte AlphaDiscard { get; set; }
}

/// <summary>
/// Defines the rendering stage when a model's selected atomics are drawn relative to other
/// transparent geometry, from earliest to latest.
/// </summary>
public enum PipeLayer : byte
{
    /// <summary>Draw first.</summary>
    First = 0,
    /// <summary>Draw before pickups.</summary>
    PrePickup = 1,
    /// <summary>Draw just after pickups.</summary>
    PostPickup = 2,
    /// <summary>Draw before the out-of-bounds object.</summary>
    PreOob = 3,
    /// <summary>Draw just after the out-of-bounds object.</summary>
    PostOob = 4,
    /// <summary>Draw before cutscene objects.</summary>
    PreCutscene = 5,
    /// <summary>Draw just after cutscene objects.</summary>
    PostCutscene = 6,
    /// <summary>Draw before NPC objects.</summary>
    PreNpc = 7,
    /// <summary>Draw just after NPC objects.</summary>
    PostNpc = 8,
    /// <summary>Draw before shadows.</summary>
    PreShadow = 9,
    /// <summary>Draw just after shadows.</summary>
    PostShadow = 10,
    /// <summary>Draw before lightning, glares, etc.</summary>
    PreFx = 11,
    /// <summary>Draw after glares, etc.</summary>
    PostFx = 12,
    /// <summary>Draw before particles.</summary>
    PreParticles = 13,
    /// <summary>Draw after particles.</summary>
    PostParticles = 14,
    /// <summary>Draw before normal transparencies (4 stages).</summary>
    PreNormal4 = 15,
    /// <summary>Draw before normal transparencies (3 stages).</summary>
    PreNormal3 = 16,
    /// <summary>Draw before normal transparencies (2 stages).</summary>
    PreNormal2 = 17,
    /// <summary>Draw before normal transparencies.</summary>
    PreNormal = 18,
    /// <summary>Draw in the normal position.</summary>
    Normal = 19,
    /// <summary>Draw directly after transparencies.</summary>
    PostNormal = 20,
    /// <summary>Draw directly after transparencies (2 stages).</summary>
    PostNormal2 = 21,
    /// <summary>Draw directly after transparencies (3 stages).</summary>
    PostNormal3 = 22,
    /// <summary>Draw directly after transparencies (4 stages).</summary>
    PostNormal4 = 23,
    /// <summary>Draw before ptank effects.</summary>
    PrePtank = 24,
    /// <summary>Draw after ptank effects.</summary>
    PostPtank = 25,
    /// <summary>Draw before decals.</summary>
    PreDecal = 26,
    /// <summary>Draw after decals.</summary>
    PostDecal = 27,
    /// <summary>Draw before lasers, etc.</summary>
    PreLastFx = 28,
    /// <summary>Draw directly after lasers, etc.</summary>
    PostLastFx = 29,
    /// <summary>Draw before last.</summary>
    PreLast = 30,
    /// <summary>Draw last.</summary>
    Last = 31,
}

using EvilHop.Common;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Builds an <see cref="AssetType.NPC"/> (or similar entity)'s runtime animation table: a set of
/// named states, each mapping to one or more raw <see cref="AssetType.Animation"/> files.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="AnimationTableFile"/> references its raw animation data - and each
/// <see cref="AnimationTableState"/> its playback effects, if any - via offsets into a trailing pool
/// this type does not individually parse; see <see cref="Asset.GetUnparsedTail"/>.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/ATBL">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class AnimationTableAsset() : Asset(AssetType.AnimationTable), IPhysicalAnimationTableAsset
{
    /// <summary>
    /// Selects which game-specific "constructor" function builds this table's runtime
    /// <c>xAnimTable</c> - effectively which entity type this table belongs to.
    /// </summary>
    public uint ConstructFunc { get; set; }

    /// <summary>
    /// The raw <see cref="AssetType.Animation"/> ids <see cref="Files"/>' entries are built from.
    /// </summary>
    public Collection<AssetId> Raw { get; } = [];

    /// <summary>
    /// The table's animation files, each wrapping one or more of <see cref="Raw"/>'s entries.
    /// </summary>
    public Collection<AnimationTableFile> Files { get; } = [];

    /// <summary>
    /// The table's named states, each playing one of <see cref="Files"/>' entries.
    /// </summary>
    public Collection<AnimationTableState> States { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalAnimationTableAsset Physical => this;

    private uint? _overriddenRawCount;
    uint IPhysicalAnimationTableAsset.RawCount
    {
        get => _overriddenRawCount ?? (uint)Raw.Count;
        set => _overriddenRawCount = value == (uint)Raw.Count ? null : value;
    }

    private uint? _overriddenFileCount;
    uint IPhysicalAnimationTableAsset.FileCount
    {
        get => _overriddenFileCount ?? (uint)Files.Count;
        set => _overriddenFileCount = value == (uint)Files.Count ? null : value;
    }

    private uint? _overriddenStateCount;
    uint IPhysicalAnimationTableAsset.StateCount
    {
        get => _overriddenStateCount ?? (uint)States.Count;
        set => _overriddenStateCount = value == (uint)States.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.AnimationTable"/> is known to be read by.
    /// </summary>
    /// <remarks>
    /// <see cref="GameVersion.N100F"/> uses a revised <see cref="States"/> layout not modeled
    /// here, degrading to the generic shape.
    /// </remarks>
    // TODO: Partial implementation - N100F uses a revised States layout not modeled here.
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };
}

/// <summary>
/// An explicit interface used to interact with <see cref="AnimationTableAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalAnimationTableAsset : IPhysicalAsset
{
    /// <summary>
    /// The number of <see cref="AnimationTableAsset.Raw"/> ids stored for this asset, read directly
    /// from its header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="AnimationTableAsset.Raw"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint RawCount { get; set; }

    /// <summary>
    /// The number of <see cref="AnimationTableAsset.Files"/> stored for this asset, read directly
    /// from its header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="AnimationTableAsset.Files"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint FileCount { get; set; }

    /// <summary>
    /// The number of <see cref="AnimationTableAsset.States"/> stored for this asset, read directly
    /// from its header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="AnimationTableAsset.States"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint StateCount { get; set; }
}

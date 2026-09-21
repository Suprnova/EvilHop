using EvilHop.Common;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// Defines an NPC's combat behavior: a flat pool of named attack <see cref="States"/> (damage,
/// hitboxes, effects, ...), <see cref="Entries"/> selecting between them by controller input,
/// <see cref="Transitions"/> between states, and <see cref="Sections"/> grouping <see cref="Entries"/>
/// into named categories.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/ATKT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class AttackTableAsset() : Asset(AssetType.AttackTable), IPhysicalAttackTableAsset
{
    /// <summary>
    /// The table's named categories, each grouping a contiguous range of <see cref="Entries"/>.
    /// </summary>
    public Collection<AttackTableSection> Sections { get; } = [];

    /// <summary>
    /// The table's controller-input-selectable attacks, each playing one of <see cref="States"/>.
    /// </summary>
    public Collection<AttackTableEntry> Entries { get; } = [];

    /// <summary>
    /// The transitions allowed between <see cref="States"/>.
    /// </summary>
    public Collection<AttackTableTransition> Transitions { get; } = [];

    /// <summary>
    /// The table's named attack states, each describing damage, hitboxes, and effects for one attack.
    /// </summary>
    public Collection<AttackTableState> States { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalAttackTableAsset Physical => this;

    private ushort? _overriddenSectionCount;
    ushort IPhysicalAttackTableAsset.SectionCount
    {
        get => _overriddenSectionCount ?? (ushort)Sections.Count;
        set => _overriddenSectionCount = value == (ushort)Sections.Count ? null : value;
    }

    private ushort? _overriddenEntryCount;
    ushort IPhysicalAttackTableAsset.EntryCount
    {
        get => _overriddenEntryCount ?? (ushort)Entries.Count;
        set => _overriddenEntryCount = value == (ushort)Entries.Count ? null : value;
    }

    private ushort? _overriddenTransitionCount;
    ushort IPhysicalAttackTableAsset.TransitionCount
    {
        get => _overriddenTransitionCount ?? (ushort)Transitions.Count;
        set => _overriddenTransitionCount = value == (ushort)Transitions.Count ? null : value;
    }

    private ushort? _overriddenStateCount;
    ushort IPhysicalAttackTableAsset.StateCount
    {
        get => _overriddenStateCount ?? (ushort)States.Count;
        set => _overriddenStateCount = value == (ushort)States.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.AttackTable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };
}

/// <summary>
/// An explicit interface used to interact with <see cref="AttackTableAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalAttackTableAsset : IPhysicalAsset
{
    /// <summary>
    /// The number of <see cref="AttackTableAsset.Sections"/> stored for this asset, read directly
    /// from its header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="AttackTableAsset.Sections"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    ushort SectionCount { get; set; }

    /// <summary>
    /// The number of <see cref="AttackTableAsset.Entries"/> stored for this asset, read directly
    /// from its header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="AttackTableAsset.Entries"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    ushort EntryCount { get; set; }

    /// <summary>
    /// The number of <see cref="AttackTableAsset.Transitions"/> stored for this asset, read directly
    /// from its header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="AttackTableAsset.Transitions"/>.Count exist, this field
    /// wins during serialization.
    /// </remarks>
    ushort TransitionCount { get; set; }

    /// <summary>
    /// The number of <see cref="AttackTableAsset.States"/> stored for this asset, read directly from
    /// its header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="AttackTableAsset.States"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    ushort StateCount { get; set; }
}

/// <summary>
/// One <see cref="AttackTableAsset"/> section: a named category grouping a contiguous range of the
/// owning table's <see cref="AttackTableAsset.Entries"/>.
/// </summary>
public sealed class AttackTableSection
{
    /// <summary>
    /// A hash identifying this section, matched against link/animation lookups by name.
    /// </summary>
    public uint SectionId { get; set; }

    /// <summary>
    /// The index into the owning <see cref="AttackTableAsset"/>'s <see cref="AttackTableAsset.Entries"/>
    /// where this section's range begins.
    /// </summary>
    public ushort Start { get; set; }

    /// <summary>
    /// The number of <see cref="AttackTableAsset.Entries"/> in this section's range, starting at
    /// <see cref="Start"/>.
    /// </summary>
    public ushort Count { get; set; }
}

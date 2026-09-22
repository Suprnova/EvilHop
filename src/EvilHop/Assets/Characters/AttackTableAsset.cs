using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

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
public sealed partial class AttackTableAsset() : Asset(AssetType.AttackTable), Physical.IAttackTableAsset
{
    /// <summary>
    /// The table's named categories, each grouping a contiguous range of <see cref="Entries"/>.
    /// </summary>
    public Collection<Section> Sections { get; } = [];

    /// <summary>
    /// The table's controller-input-selectable attacks, each playing one of <see cref="States"/>.
    /// </summary>
    public Collection<Entry> Entries { get; } = [];

    /// <summary>
    /// The transitions allowed between <see cref="States"/>.
    /// </summary>
    public Collection<Transition> Transitions { get; } = [];

    /// <summary>
    /// The table's named attack states, each describing damage, hitboxes, and effects for one attack.
    /// </summary>
    public Collection<State> States { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IAttackTableAsset Physical => this;

    private ushort? _overriddenSectionCount;
    ushort Physical.IAttackTableAsset.SectionCount
    {
        get => _overriddenSectionCount ?? (ushort)Sections.Count;
        set => _overriddenSectionCount = value == (ushort)Sections.Count ? null : value;
    }

    private ushort? _overriddenEntryCount;
    ushort Physical.IAttackTableAsset.EntryCount
    {
        get => _overriddenEntryCount ?? (ushort)Entries.Count;
        set => _overriddenEntryCount = value == (ushort)Entries.Count ? null : value;
    }

    private ushort? _overriddenTransitionCount;
    ushort Physical.IAttackTableAsset.TransitionCount
    {
        get => _overriddenTransitionCount ?? (ushort)Transitions.Count;
        set => _overriddenTransitionCount = value == (ushort)Transitions.Count ? null : value;
    }

    private ushort? _overriddenStateCount;
    ushort Physical.IAttackTableAsset.StateCount
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

    internal static AttackTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new AttackTableAsset();
        AssetFields.Populate(asset, header, debug);

        ushort sectionCount = reader.ReadUInt16();
        ushort entryCount = reader.ReadUInt16();
        ushort transitionCount = reader.ReadUInt16();
        ushort stateCount = reader.ReadUInt16();

        for (int i = 0; i < sectionCount; i++)
            asset.Sections.Add(Section.Read(reader, profile));

        for (int i = 0; i < entryCount; i++)
            asset.Entries.Add(Entry.Read(reader, profile));

        for (int i = 0; i < transitionCount; i++)
            asset.Transitions.Add(Transition.Read(reader, profile));

        for (int i = 0; i < stateCount; i++)
            asset.States.Add(State.Read(reader, profile));

        asset.Physical.SectionCount = sectionCount;
        asset.Physical.EntryCount = entryCount;
        asset.Physical.TransitionCount = transitionCount;
        asset.Physical.StateCount = stateCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(AttackTableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.SectionCount);
        writer.Write(asset.Physical.EntryCount);
        writer.Write(asset.Physical.TransitionCount);
        writer.Write(asset.Physical.StateCount);

        foreach (var section in asset.Sections)
            Section.Write(section, writer, profile);

        foreach (var entry in asset.Entries)
            Entry.Write(entry, writer, profile);

        foreach (var transition in asset.Transitions)
            Transition.Write(transition, writer, profile);

        foreach (var state in asset.States)
            State.Write(state, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// One <see cref="AttackTableAsset"/> section: a named category grouping a contiguous range of the
    /// owning table's <see cref="Entries"/>.
    /// </summary>
    public sealed class Section
    {
        /// <summary>
        /// A hash identifying this section, matched against link/animation lookups by name.
        /// </summary>
        public uint SectionId { get; set; }

        /// <summary>
        /// The index into the owning <see cref="AttackTableAsset"/>'s <see cref="Entries"/>
        /// where this section's range begins.
        /// </summary>
        public ushort Start { get; set; }

        /// <summary>
        /// The number of <see cref="Entries"/> in this section's range, starting at
        /// <see cref="Start"/>.
        /// </summary>
        public ushort Count { get; set; }

        internal static Section Read(EndianReader reader, FormatProfile _) => new()
        {
            SectionId = reader.ReadUInt32(),
            Start = reader.ReadUInt16(),
            Count = reader.ReadUInt16(),
        };

        internal static void Write(Section section, EndianWriter writer, FormatProfile _)
        {
            writer.Write(section.SectionId);
            writer.Write(section.Start);
            writer.Write(section.Count);
        }
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="AttackTableAsset"/>'s underlying values.
    /// </summary>
    public interface IAttackTableAsset : IAsset
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
}

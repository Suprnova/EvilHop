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

/// <summary>
/// One <see cref="AnimationTableAsset"/> file: one or more of the table's <see cref="AnimationTableAsset.Raw"/>
/// animations, optionally blended together across a bilinear grid.
/// </summary>
public sealed class AnimationTableFile
{
    /// <summary>
    /// Playback flags. Known bits, from decompiled source: 0x1000 plays the animation in reverse;
    /// 0x2000 doubles the duration and plays the second half in reverse; 0x4000 marks
    /// <see cref="NumAnimsX"/>/<see cref="NumAnimsY"/> as an active bilinear blend grid; 0x8000 marks
    /// this as vertex/morph animation data rather than skeletal.
    /// </summary>
    /// TODO: should be a real flags enum
    public uint FileFlags { get; set; }

    /// <summary>
    /// The playback duration, in seconds.
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// The time, in seconds, playback starts at within the underlying raw animation(s).
    /// </summary>
    public float TimeOffset { get; set; }

    /// <summary>
    /// The bilinear blend grid's width, in raw animations. 1 for a non-blended file.
    /// </summary>
    public ushort NumAnimsX { get; set; }

    /// <summary>
    /// The bilinear blend grid's height, in raw animations. 1 for a non-blended file.
    /// </summary>
    public ushort NumAnimsY { get; set; }

    /// <summary>
    /// An internal byte offset, from the start of the owning <see cref="AnimationTableAsset"/>'s data,
    /// to this file's <see cref="NumAnimsX"/>*<see cref="NumAnimsY"/> indices into
    /// <see cref="AnimationTableAsset.Raw"/>. Not individually resolved; preserved via
    /// <see cref="Asset.GetUnparsedTail"/>.
    /// </summary>
    public uint RawDataOffset { get; set; }

    /// <summary>
    /// Unknown. Usually -1.
    /// </summary>
    public int Physics { get; set; }

    /// <summary>
    /// Unknown. Usually -1.
    /// </summary>
    public int StartPose { get; set; }

    /// <summary>
    /// Unknown. Usually -1.
    /// </summary>
    public int EndPose { get; set; }
}

/// <summary>
/// One <see cref="AnimationTableAsset"/> state: a named entry in the table's runtime state machine.
/// </summary>
public sealed class AnimationTableState
{
    /// <summary>
    /// A hash identifying this state, matched against link/animation-table lookups by name.
    /// </summary>
    public uint StateId { get; set; }

    /// <summary>
    /// The index into the owning <see cref="AnimationTableAsset"/>'s <see cref="AnimationTableAsset.Files"/>
    /// this state plays.
    /// </summary>
    public uint FileIndex { get; set; }

    /// <summary>
    /// The number of playback effects (sounds, etc.) attached to this state, starting at
    /// <see cref="EffectOffset"/>. Not individually resolved; preserved via
    /// <see cref="Asset.GetUnparsedTail"/>.
    /// </summary>
    public uint EffectCount { get; set; }

    /// <summary>
    /// An internal byte offset, from the start of the owning <see cref="AnimationTableAsset"/>'s data,
    /// to this state's <see cref="EffectCount"/> effect records. Not individually resolved; preserved
    /// via <see cref="Asset.GetUnparsedTail"/>.
    /// </summary>
    public uint EffectOffset { get; set; }

    /// <summary>
    /// The playback speed multiplier applied while in this state.
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// The id of a "sub-state" this state's file is additionally registered under, if any - see
    /// <see cref="SubStateCount"/>.
    /// </summary>
    public uint SubStateId { get; set; }

    /// <summary>
    /// The number of files sharing <see cref="SubStateId"/>, chosen between at random when this state
    /// is entered.
    /// </summary>
    public uint SubStateCount { get; set; }
}

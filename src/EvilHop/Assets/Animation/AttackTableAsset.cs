using EvilHop.Common;
using System.Collections.Immutable;
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

/// <summary>
/// One <see cref="AttackTableAsset"/> entry: an attack selectable by controller input, playing one of
/// the owning table's <see cref="AttackTableAsset.States"/>.
/// </summary>
public sealed class AttackTableEntry
{
    /// <summary>
    /// A hash identifying the <see cref="AttackTableAsset.States"/> entry (or the entity's animation
    /// table state) this attack plays.
    /// </summary>
    public uint AnimationStateId { get; set; }

    /// <summary>
    /// The start of this entry's range into an animation-related sub-list. The list itself isn't
    /// individually resolved by this type.
    /// </summary>
    public ushort AnimationStart { get; set; }

    /// <summary>
    /// The length of <see cref="AnimationStart"/>'s range.
    /// </summary>
    public ushort AnimationCount { get; set; }

    /// <summary>
    /// The index into the owning <see cref="AttackTableAsset"/>'s <see cref="AttackTableAsset.Transitions"/>
    /// where this entry's range begins, mirroring how <see cref="AttackTableSection.Start"/> ranges
    /// over <see cref="AttackTableAsset.Entries"/>.
    /// </summary>
    public ushort Start { get; set; }

    /// <summary>
    /// The number of <see cref="AttackTableAsset.Transitions"/> in this entry's range, starting at
    /// <see cref="Start"/>.
    /// </summary>
    public ushort Count { get; set; }

    /// <summary>
    /// Input flags that must be held for this entry to trigger. Always 0 in every sampled archive.
    /// </summary>
    public ushort OnFlags { get; set; }

    /// <summary>
    /// Input flags that must not be held for this entry to trigger. Always 0 in every sampled
    /// archive.
    /// </summary>
    public ushort OffFlags { get; set; }

    /// <summary>
    /// The controller input that triggers this entry.
    /// </summary>
    public byte Input { get; set; }

    /// <summary>
    /// The power level required to trigger this entry.
    /// </summary>
    public byte Power { get; set; }

    /// <summary>
    /// The time, in seconds into the current animation, at which this entry becomes selectable.
    /// </summary>
    public float StartTime { get; set; }
}

/// <summary>
/// One <see cref="AttackTableAsset"/> transition: an allowed switch between two of the owning table's
/// <see cref="AttackTableAsset.States"/>.
/// </summary>
public sealed class AttackTableTransition
{
    /// <summary>
    /// A hash identifying the <see cref="AttackTableAsset.States"/> entry this transition starts from.
    /// </summary>
    public uint SourceState { get; set; }

    /// <summary>
    /// A hash identifying the <see cref="AttackTableAsset.States"/> entry this transition ends at.
    /// </summary>
    public uint DestinationState { get; set; }

    /// <summary>
    /// The time, in seconds into <see cref="SourceState"/>'s animation, at which this transition
    /// becomes available.
    /// </summary>
    public float SourceTime { get; set; }

    /// <summary>
    /// The time, in seconds, this transition's blend takes to complete.
    /// </summary>
    public float ThroughTime { get; set; }

    /// <summary>
    /// The time, in seconds into <see cref="DestinationState"/>'s animation, playback resumes at.
    /// </summary>
    public float DestinationTime { get; set; }

    /// <summary>
    /// The time, in seconds, the blend between animations takes.
    /// </summary>
    public float BlendTime { get; set; }

    /// <summary>
    /// Unknown flags. Always 0 in every sampled archive.
    /// </summary>
    public uint Flags { get; set; }
}

/// <summary>
/// One hit detection bone for an <see cref="AttackTableState"/>: a skeleton bone plus an offset from
/// it, checked against a target's collision volume.
/// </summary>
public sealed class HitBoneInfo
{
    /// <summary>
    /// The skeleton bone index this hit check is relative to, or <c>0xFFFF</c> if unused.
    /// </summary>
    public ushort Bone { get; set; }

    /// <summary>
    /// The offset from <see cref="Bone"/> the hit check is performed at.
    /// </summary>
    public Vector3 Offset { get; set; }

    /// <summary>
    /// Unknown. Observed values are 0, 3, and 5.
    /// </summary>
    public short Atomic { get; set; }
}

/// <summary>
/// One <see cref="AttackTableAsset"/> state: the damage, hitboxes, and effects for a single attack
/// animation, matched by <see cref="AttackTableEntry.AnimationStateId"/> and
/// <see cref="AttackTableTransition"/>.
/// </summary>
public sealed class AttackTableState
{
    /// <summary>
    /// A hash identifying this state, matched against <see cref="AttackTableEntry.AnimationStateId"/>
    /// and <see cref="AttackTableTransition"/>.
    /// </summary>
    public uint StateId { get; set; }

    /// <summary>
    /// The distance the entity moves along its facing direction while in this state.
    /// </summary>
    public float MoveDistanceZ { get; set; }

    /// <summary>
    /// The distance the entity moves vertically while in this state.
    /// </summary>
    public float MoveDistanceY { get; set; }

    /// <summary>
    /// The time, in seconds, <see cref="MoveDistanceZ"/>/<see cref="MoveDistanceY"/> take to complete.
    /// </summary>
    public float MoveTime { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, the attack's hit window opens. <c>-1</c> if this
    /// state has no attack.
    /// </summary>
    public float AttackStart { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, the attack's hit window closes.
    /// </summary>
    public float AttackEnd { get; set; }

    /// <summary>
    /// The radius of the attack's hit check, centered on <see cref="HitBones"/>.
    /// </summary>
    public float AttackRadius { get; set; }

    private ImmutableArray<HitBoneInfo> _hitBones = ZeroedHitBones();

    /// <summary>
    /// The 4 bones <see cref="AttackRadius"/> is checked against.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 4.</exception>
    public ImmutableArray<HitBoneInfo> HitBones
    {
        get => _hitBones;
        set => _hitBones = value.Length == 4
            ? value
            : throw new ArgumentException($"{nameof(HitBones)} must contain exactly 4 elements.", nameof(value));
    }

    /// <summary>
    /// The damage dealt on a successful hit.
    /// </summary>
    public short Damage { get; set; }

    /// <summary>
    /// Unknown. Likely a hit sound or reaction category. Observed values are 9 and 12.
    /// </summary>
    public ushort Source { get; set; }

    /// <summary>
    /// An index into an effect table, played for the duration of this state.
    /// </summary>
    public ushort Effect { get; set; }

    /// <summary>
    /// An index into an effect table, played on a successful hit.
    /// </summary>
    public ushort HitEffect { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, <see cref="Effect"/> starts playing.
    /// </summary>
    public float EffectStart { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, <see cref="Effect"/> stops playing. <c>-1</c> if
    /// unused.
    /// </summary>
    public float EffectEnd { get; set; }

    private ImmutableArray<ushort> _effectBonesOutside = ZeroedEffectBones();

    /// <summary>
    /// The 2 bones <see cref="Effect"/> is attached to while playing outside the target.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 2.</exception>
    public ImmutableArray<ushort> EffectBonesOutside
    {
        get => _effectBonesOutside;
        set => _effectBonesOutside = value.Length == 2
            ? value
            : throw new ArgumentException($"{nameof(EffectBonesOutside)} must contain exactly 2 elements.", nameof(value));
    }

    private ImmutableArray<ushort> _effectBonesInside = ZeroedEffectBones();

    /// <summary>
    /// The 2 bones <see cref="Effect"/> is attached to while playing inside the target.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 2.</exception>
    public ImmutableArray<ushort> EffectBonesInside
    {
        get => _effectBonesInside;
        set => _effectBonesInside = value.Length == 2
            ? value
            : throw new ArgumentException($"{nameof(EffectBonesInside)} must contain exactly 2 elements.", nameof(value));
    }

    /// <summary>
    /// The time, in seconds into the animation, controller rumble starts.
    /// </summary>
    public float RumbleStartTime { get; set; }

    /// <summary>
    /// Unknown. Selects a controller rumble configuration. Always 0 in every sampled archive.
    /// </summary>
    public uint RumbleEmitterId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Shrapnel"/> spawned by this state, if any. Always
    /// <see cref="AssetId.None"/> in every sampled archive.
    /// </summary>
    public AssetId ShrapnelId { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, <see cref="ShrapnelId"/> spawns.
    /// </summary>
    public float ShrapnelStartTime { get; set; }

    /// <summary>
    /// The upward velocity applied to a hit target.
    /// </summary>
    public float VelocityUp { get; set; }

    /// <summary>
    /// The outward velocity applied to a hit target.
    /// </summary>
    public float VelocityAway { get; set; }

    /// <summary>
    /// Unknown flags.
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// The time, in seconds, a held input is required before this state can be interrupted by
    /// another action.
    /// </summary>
    public float HoldTime { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, a jump can interrupt this state.
    /// </summary>
    public float JumpBreakTime { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, a crouch can interrupt this state.
    /// </summary>
    public float CrouchBreakTime { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, camera turn-locking begins.
    /// </summary>
    public float TurnLockStart { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, camera turn-locking ends.
    /// </summary>
    public float TurnLockStop { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, a climax (combo finisher) camera move begins.
    /// </summary>
    public float ClimaxTime { get; set; }

    /// <summary>
    /// The camera offset used during the climax camera move.
    /// </summary>
    public Vector3 ClimaxOffset { get; set; }

    /// <summary>
    /// The rate, per second, this state drains from a resource (e.g. a power meter) while active.
    /// </summary>
    public float DrainRate { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, a screen blur effect starts fading in.
    /// </summary>
    public float BlurStart { get; set; }

    /// <summary>
    /// The time, in seconds into the animation, the screen blur effect starts fading out.
    /// </summary>
    public float BlurEnd { get; set; }

    /// <summary>
    /// The total duration, in seconds, the screen blur effect is visible for.
    /// </summary>
    public float BlurLife { get; set; }

    /// <summary>
    /// The peak opacity of the screen blur effect.
    /// </summary>
    public float BlurAlpha { get; set; }

    /// <summary>
    /// The time, in seconds, the screen blur effect takes to fade in.
    /// </summary>
    public float BlurFadeInTime { get; set; }

    /// <summary>
    /// The time, in seconds, the screen blur effect takes to fade out.
    /// </summary>
    public float BlurFadeOutTime { get; set; }

    /// <summary>
    /// Unknown. Always 0 in every sampled archive.
    /// </summary>
    public short FlashAlpha { get; set; }

    /// <summary>
    /// The time, in seconds, a screen flash effect lasts.
    /// </summary>
    public float FlashTime { get; set; }

    /// <summary>
    /// The combo score bonus awarded for this state.
    /// </summary>
    public float ComboBonus { get; set; }

    /// <summary>
    /// Unknown. Observed values are 0, 10, and 2560.
    /// </summary>
    public short ComboType { get; set; }

    /// <summary>
    /// The power meter bonus awarded for this state.
    /// </summary>
    public short PowerBonus { get; set; }

    private static ImmutableArray<HitBoneInfo> ZeroedHitBones() => [new(), new(), new(), new()];
    private static ImmutableArray<ushort> ZeroedEffectBones() => [0, 0];
}

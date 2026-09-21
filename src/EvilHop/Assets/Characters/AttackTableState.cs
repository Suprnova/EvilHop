using EvilHop.Common;
using System.Collections.Immutable;
using System.Numerics;

namespace EvilHop.Assets;

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
    /// Unknown.
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
    /// Unknown.
    /// </summary>
    public uint RumbleEmitterId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Shrapnel"/> spawned by this state, if any.
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
    /// Unknown.
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
    /// Unknown.
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
    /// Unknown.
    /// </summary>
    public short ComboType { get; set; }

    /// <summary>
    /// The power meter bonus awarded for this state.
    /// </summary>
    public short PowerBonus { get; set; }

    private static ImmutableArray<HitBoneInfo> ZeroedHitBones() => [new(), new(), new(), new()];
    private static ImmutableArray<ushort> ZeroedEffectBones() => [0, 0];
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
    /// Unknown.
    /// </summary>
    public short Atomic { get; set; }
}

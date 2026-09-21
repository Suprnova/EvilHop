namespace EvilHop.Assets;

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
    /// Input flags that must be held for this entry to trigger. Always 0.
    /// </summary>
    public ushort OnFlags { get; set; }

    /// <summary>
    /// Input flags that must not be held for this entry to trigger. Always 0.
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

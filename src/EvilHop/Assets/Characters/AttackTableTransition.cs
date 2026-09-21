namespace EvilHop.Assets;

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
    /// Unknown flags. Always 0.
    /// </summary>
    public uint Flags { get; set; }
}

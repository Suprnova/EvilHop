using EvilHop.Common;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A table of voice-line triggers, each playing a <see cref="AssetType.SoundGroup"/> in response to a
/// game event, subject to a probability and replay cooldown.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/ONEL">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class OneLinerAsset() : Asset(AssetType.OneLiner), IPhysicalOneLinerAsset
{
    /// <summary>The table's entries, each triggered independently.</summary>
    public Collection<OneLinerEntry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalOneLinerAsset Physical => this;

    private uint? _overriddenEntryCount;
    uint IPhysicalOneLinerAsset.EntryCount
    {
        get => _overriddenEntryCount ?? (uint)Entries.Count;
        set => _overriddenEntryCount = value == (uint)Entries.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.OneLiner"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };
}

/// <summary>
/// An explicit interface used to interact with <see cref="OneLinerAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalOneLinerAsset : IPhysicalAsset
{
    /// <summary>
    /// The number of <see cref="OneLinerAsset.Entries"/> stored for this asset, read directly from
    /// its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="OneLinerAsset.Entries"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint EntryCount { get; set; }
}

/// <summary>
/// One <see cref="OneLinerAsset"/> entry: a voice line played from <see cref="SoundGroupId"/> when
/// <see cref="EventType"/> occurs.
/// </summary>
public sealed class OneLinerEntry
{
    /// <summary>The <see cref="AssetType.SoundGroup"/> played when this entry triggers.</summary>
    public AssetId SoundGroupId { get; set; }

    /// <summary>How long, in seconds, to wait after triggering before <see cref="SoundGroupId"/> actually plays.</summary>
    public float SoundStartDelay { get; set; }

    /// <summary>
    /// A time window, in seconds, associated with this entry's play-count tracking.
    /// </summary>
    public float TimeSpan { get; set; }

    /// <summary>
    /// The time this entry last played, used to enforce <see cref="DelayBetweenPlays"/>.
    /// </summary>
    public float TimeLastPlayed { get; set; }

    /// <summary>
    /// The number of times this entry has played so far.
    /// </summary>
    public uint NumPlays { get; set; }

    /// <summary>The minimum time, in seconds, between two plays of this entry.</summary>
    public float DelayBetweenPlays { get; set; }

    /// <summary>The chance, from 0 to 1, that this entry plays when triggered.</summary>
    public float Probability { get; set; }

    /// <summary>This entry's default duration, in seconds.</summary>
    public float DefaultDuration { get; set; }

    /// <summary>
    /// The duration, in seconds, of this entry's last play.
    /// </summary>
    public float LastDuration { get; set; }

    /// <summary>
    /// The maximum number of times this entry may play.
    /// </summary>
    public uint MaxPlays { get; set; }

    /// <summary>The game event that triggers this entry.</summary>
    public short EventType { get; set; }

    /// <summary>Whether this entry plays through the music channel rather than the sound effect channel.</summary>
    public bool PlaysInMusicChannel { get; set; }

    /// <summary>Which condition, if any, gates this entry playing.</summary>
    public OneLinerPlayerType PlayerType { get; set; }

    /// <summary>
    /// The first parameter passed to <see cref="PlayerType"/>'s condition, if applicable.
    /// </summary>
    public int FirstParam { get; set; }

    /// <summary>
    /// The second parameter passed to <see cref="PlayerType"/>'s condition, if applicable.
    /// </summary>
    public float SecondParam { get; set; }
}

/// <summary>
/// Identifies the type of criteria for which this <see cref="OneLinerEntry"/> will play.
/// </summary>
public enum OneLinerPlayerType
{
    /// <summary>This entry has no gating condition - it is always eligible to play.</summary>
    Always = 0,

    /// <summary>Unknown.</summary>
    Counter = 1,

    /// <summary>Unknown.</summary>
    Checker = 2,

    /// <summary>
    /// This entry's eligibility is gated by evaluating <see cref="OneLinerEntry.FirstParam"/> and
    /// <see cref="OneLinerEntry.SecondParam"/> against an unconfirmed condition.
    /// </summary>
    Tester = 3,
}

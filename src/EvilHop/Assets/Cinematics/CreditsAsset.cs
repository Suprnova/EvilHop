using EvilHop.Common;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// The end-credits sequence played after completing the game: a set of independently-scrolling
/// <see cref="CreditsSection"/>s, optionally encrypted on disk.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/CRDT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class CreditsAsset() : Asset(AssetType.Credits), IPhysicalCreditsAsset
{
    private const int HeaderSize = 24;

    /// <summary>
    /// Whether this <see cref="CreditsAsset"/> is stored encrypted on disk.
    /// </summary>
    public bool IsEncrypted
    {
        get => Physical.State == CreditsState.Encrypted;
        set => Physical.State = value ? CreditsState.Encrypted : CreditsState.NotEncrypted;
    }

    /// <summary>
    /// The credits sequence's total duration, in seconds. Loops back to the start once elapsed.
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// This <see cref="CreditsAsset"/>'s independently-scrolling sections, shown one after another.
    /// </summary>
    public Collection<CreditsSection> Sections { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalCreditsAsset Physical => this;

    private uint _magic = 0xBEEEEEEF;
    uint IPhysicalCreditsAsset.Magic { get => _magic; set => _magic = value; }

    private uint _version;
    uint IPhysicalCreditsAsset.Version { get => _version; set => _version = value; }

    private AssetId _creditsId;
    AssetId IPhysicalCreditsAsset.CreditsId { get => _creditsId; set => _creditsId = value; }

    private CreditsState _state;
    CreditsState IPhysicalCreditsAsset.State { get => _state; set => _state = value; }

    private uint? _overriddenTotalSize;
    uint IPhysicalCreditsAsset.TotalSize
    {
        get => _overriddenTotalSize ?? ComputedTotalSize;
        set => _overriddenTotalSize = value == ComputedTotalSize ? null : value;
    }

    private uint ComputedTotalSize =>
        (uint)(HeaderSize + Sections.Sum(SectionByteLength) + GetUnparsedTail().Length);

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Credits"/> is known to be read by.
    /// </summary>
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
/// An explicit interface used to interact with <see cref="CreditsAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalCreditsAsset : IPhysicalAsset
{
    /// <summary>
    /// A magic number used to validate the payload.
    /// </summary>
    uint Magic { get; set; }

    /// <summary>
    /// The credits format version. 256 (1.0) in <see cref="GameVersion.BFBB"/> and
    /// <see cref="GameVersion.Incredibles"/>; 512 (2.0) from <see cref="GameVersion.TSSM"/> onward.
    /// </summary>
    uint Version { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    AssetId CreditsId { get; set; }

    /// <summary>
    /// Whether the body following this header is encrypted.
    /// </summary>
    CreditsState State { get; set; }

    /// <summary>
    /// The total size, in bytes, of this <see cref="CreditsAsset"/>'s entire on-disk representation,
    /// including this header.
    /// </summary>
    /// <remarks>
    /// When disagreements with the actual encoded size exist, this field wins during serialization.
    /// </remarks>
    uint TotalSize { get; set; }
}

/// <summary>
/// Defines the playback and display state of a credits entry.
/// </summary>
public enum CreditsState : uint
{
    /// <summary>The body following the header is stored as-is.</summary>
    NotEncrypted = 1,
    /// <summary>The body following the header is encrypted and must be decrypted before use.</summary>
    Encrypted = 3,
}

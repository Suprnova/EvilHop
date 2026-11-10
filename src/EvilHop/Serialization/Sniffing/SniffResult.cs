using EvilHop.Blocks;
using EvilHop.Common;

namespace EvilHop.Serialization.Sniffing;

/// <summary>
/// How confidently <see cref="Sniffer.Sniff(Stream)"/> was able to identify a stream.
/// </summary>
public enum SniffConfidence
{
    /// <summary>The stream isn't recognizable as a HIP archive at all.</summary>
    Unrecognized,

    /// <summary>Two or more games tied for the best score; <see cref="SniffResult.Candidates"/> lists all of them.</summary>
    Ambiguous,

    /// <summary>Exactly one game scored best.</summary>
    Resolved
}

/// <summary>
/// Which evidence in the stream named its platform.
/// </summary>
public enum PlatformSource
{
    /// <summary>Nothing in the stream named a platform.</summary>
    None,

    /// <summary>
    /// The <c>PFLG</c> block's platform bit. Used only when <see cref="PlatStamp"/> has nothing to say.
    /// </summary>
    PackFlags,

    /// <summary>The <c>PLAT</c> block's platform identifier. Wins over <see cref="PackFlags"/>.</summary>
    PlatStamp
}

/// <summary>
/// One game considered during sniffing, and how well it fit the observed <see cref="SniffSignals"/>.
/// </summary>
/// <param name="Game">The candidate game.</param>
/// <param name="Score">
/// This candidate's fit, in <c>[0, 1]</c>. Only candidates tied for the highest score across all six
/// games appear in <see cref="SniffResult.Candidates"/>.
/// </param>
/// <param name="Profile">
/// The <see cref="FormatProfile"/> this candidate implies: <see cref="Game"/>'s default profile,
/// overridden with whatever the observed <see cref="SniffSignals"/> imply. Its
/// <see cref="FormatProfile.Platform"/> is <see langword="null"/> when no signal named one.
/// </param>
public sealed record SniffCandidate(GameVersion Game, double Score, FormatProfile Profile);

/// <summary>
/// The raw evidence <see cref="Sniffer.Sniff(Stream)"/> gathered from a stream, before scoring. Holds
/// evidence only, no verdicts.
/// </summary>
/// <param name="HasHipaMagic">Whether the stream opened with a valid <c>HIPA</c> tag and zero size.</param>
/// <param name="ClientVersion">The <c>PVER</c> block's <see cref="Blocks.ClientVersion"/>, if read.</param>
/// <param name="Flags">The <c>PFLG</c> block's <see cref="PackFlags"/>, if read.</param>
/// <param name="PlatformStrings">
/// Every <see cref="Primitives.EvilString"/> present in the <c>PLAT</c> block, in on-disk order.
/// Empty when the archive has no <c>PLAT</c> block at all.
/// </param>
/// <param name="Created">The <c>PCRT</c> block's creation timestamp, if read.</param>
/// <param name="LayerTypeRawValues">
/// Every <c>LHDR</c>'s raw, unmapped layer type value observed. Confirmation-only - never fed into
/// scoring.
/// </param>
/// <param name="LayerDebugValues">
/// Every <c>LDBG</c> value observed. Confirmation-only - never fed into scoring.
/// </param>
/// <param name="AssetTypes">The distinct raw <c>AHDR</c> type FourCCs observed, across every asset.</param>
/// <param name="DpakPaddingObserved">
/// Whether the <c>DPAK</c> block's first four content bytes after its padding-amount field were all
/// <c>0x33</c>. <see langword="null"/> when unobserved (no assets, too little content to peek, or the
/// bytes observed weren't a positive match) rather than a definite "false" - see
/// <see cref="SniffScorer"/> for why that distinction matters.
/// </param>
public sealed record SniffSignals(
    bool HasHipaMagic,
    ClientVersion? ClientVersion,
    PackFlags? Flags,
    IReadOnlyList<string> PlatformStrings,
    DateTimeOffset? Created,
    IReadOnlyList<uint> LayerTypeRawValues,
    IReadOnlyList<uint> LayerDebugValues,
    IReadOnlySet<string> AssetTypes,
    bool? DpakPaddingObserved);

/// <summary>
/// The result of <see cref="Sniffer.Sniff(Stream)"/> inferring a stream's game and format from its bytes.
/// </summary>
/// <param name="Confidence">How confidently <see cref="Profile"/> was inferred.</param>
/// <param name="Candidates">
/// Every game tied for the top score: one entry when <see cref="SniffConfidence.Resolved"/>, several
/// when <see cref="SniffConfidence.Ambiguous"/>, none when <see cref="SniffConfidence.Unrecognized"/>.
/// </param>
/// <param name="Signals">The raw evidence the scoring above was computed from.</param>
/// <param name="PlatformSource">Which evidence named the platform in every <see cref="Candidates"/> profile.</param>
/// <param name="GameFixed">
/// Whether the caller stated the game, so <see cref="Candidates"/> holds that game and no scoring
/// chose it.
/// </param>
public sealed record SniffResult(
    SniffConfidence Confidence,
    IReadOnlyList<SniffCandidate> Candidates,
    SniffSignals Signals,
    PlatformSource PlatformSource,
    bool GameFixed)
{
    /// <summary>
    /// Whether both facts were established rather than guessed: the caller stated the game, and the
    /// stream itself named the platform. Anything else is weak.
    /// </summary>
    public bool IsStrong => GameFixed && PlatformSource is not PlatformSource.None;

    /// <summary>
    /// The inferred <see cref="FormatProfile"/>, bound to the first tied <see cref="Candidates"/>
    /// entry. <see langword="null"/> only when <see cref="Confidence"/> is
    /// <see cref="SniffConfidence.Unrecognized"/>, since <see cref="Candidates"/> is empty then.
    /// </summary>
    public FormatProfile? Profile => Candidates.Count > 0 ? Candidates[0].Profile : null;
}

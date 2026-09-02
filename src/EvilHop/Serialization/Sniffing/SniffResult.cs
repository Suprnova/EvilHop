using EvilHop.Blocks;
using EvilHop.Common;

namespace EvilHop.Serialization.Sniffing;

/// <summary>
/// How confidently <see cref="Sniffer.Sniff"/> was able to identify a stream.
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
/// One game considered during sniffing, and how well it fit the observed <see cref="SniffSignals"/>.
/// </summary>
/// <param name="Game">The candidate game.</param>
/// <param name="Score">
/// This candidate's fit, in <c>[0, 1]</c>. Only candidates tied for the highest score across all six
/// games appear in <see cref="SniffResult.Candidates"/>.
/// </param>
/// <param name="Profile">
/// The <see cref="FormatProfile"/> this candidate implies: <see cref="Game"/>'s <c>DefaultProfile</c>,
/// overridden with whatever the observed <see cref="SniffSignals"/> imply.
/// </param>
public sealed record SniffCandidate(GameVersion Game, double Score, FormatProfile Profile);

/// <summary>
/// The raw evidence <see cref="Sniffer.Sniff"/> gathered from a stream, before scoring. Holds
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
/// The result of <see cref="Sniffer.Sniff"/> inferring a stream's game and format from its bytes.
/// </summary>
/// <param name="Confidence">How confidently <see cref="Profile"/> was inferred.</param>
/// <param name="Candidates">
/// Every game tied for the top score: one entry when <see cref="SniffConfidence.Resolved"/>, several
/// when <see cref="SniffConfidence.Ambiguous"/>, none when <see cref="SniffConfidence.Unrecognized"/>.
/// </param>
/// <param name="Signals">The raw evidence the scoring above was computed from.</param>
public sealed record SniffResult(
    SniffConfidence Confidence,
    IReadOnlyList<SniffCandidate> Candidates,
    SniffSignals Signals)
{
    /// <summary>
    /// The inferred <see cref="FormatProfile"/>, bound to the first tied <see cref="Candidates"/>
    /// entry. <see langword="null"/> only when <see cref="Confidence"/> is
    /// <see cref="SniffConfidence.Unrecognized"/>, since <see cref="Candidates"/> is empty then.
    /// </summary>
    public FormatProfile? Profile => Candidates.Count > 0 ? Candidates[0].Profile : null;
}

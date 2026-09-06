using EvilHop.Blocks;
using EvilHop.Common;

namespace EvilHop.Serialization.Sniffing;

/// <summary>
/// Scores every <see cref="GameVersion"/> against a <see cref="SniffSignals"/> and picks the tied
/// top-scoring set.
/// </summary>
internal static class SniffScorer
{
    /// <summary>
    /// Scores all six games against <paramref name="signals"/> and returns the tied top-scoring
    /// candidates as a <see cref="SniffResult"/>.
    /// </summary>
    public static SniffResult Score(SniffSignals signals)
    {
        var candidates = Enum.GetValues<GameVersion>()
            .Select(game => new SniffCandidate(game, ComputeScore(game, signals), SniffProfileBuilder.Build(game, signals)))
            .ToList();

        double topScore = candidates.Max(c => c.Score);
        var tied = candidates.Where(c => Math.Abs(c.Score - topScore) < 1e-9).ToList();

        var confidence = tied.Count == 1 ? SniffConfidence.Resolved : SniffConfidence.Ambiguous;
        return new SniffResult(confidence, tied, signals);
    }

    /// <summary>
    /// Four weighted sub-scores in <c>[0, 1]</c> (client version, PLAT shape, asset-type marker
    /// fraction, created-date range), averaged over whichever aren't <see langword="null"/> - a
    /// candidate with no applicable evidence at all scores 0 rather than throwing away the average.
    /// Created-date range outweighs the marker fraction rather than the reverse: re-verifying
    /// against real corpus archives showed most individual files simply don't reference any of
    /// their game's marker asset types at all (they're rare, not per-file), so with marker and date
    /// weighted close together, a marker-less archive would let the two marker-less candidates
    /// (N100F, Ratatouille) out-score the correct marker-bearing one on averaging alone even when
    /// its date lands squarely in the right range.
    /// </summary>
    private static double ComputeScore(GameVersion game, SniffSignals signals)
    {
        var signature = GameSignatures.For(game);

        double? clientVersionFit = signals.ClientVersion is ClientVersion clientVersion
            ? (signature.ValidClientVersions.Contains(clientVersion) ? 1.0 : 0.0)
            : null;

        PlatShape? observedShape = signals.PlatformStrings.Count switch
        {
            0 => PlatShape.None,
            4 => PlatShape.FourString,
            5 => PlatShape.FiveString,
            _ => null
        };
        double? platShapeFit = observedShape is PlatShape shape ? (shape == signature.PlatShape ? 1.0 : 0.0) : null;

        double? markerFit = signature.AssetTypeMarkers is string[] markers
            ? (double)markers.Count(signals.AssetTypes.Contains) / markers.Length
            : null;

        double? dateFit = signals.Created is DateTimeOffset created
            ? (created >= signature.CreatedRangeStart && created <= signature.CreatedRangeEnd ? 1.0 : 0.0)
            : null;

        (double Weight, double? Score)[] subscores =
        [
            (3, clientVersionFit),
            (3, platShapeFit),
            (1, markerFit),
            (3, dateFit)
        ];

        double totalWeight = subscores.Where(s => s.Score is not null).Sum(s => s.Weight);
        if (totalWeight == 0) return 0;

        return subscores.Where(s => s.Score is not null).Sum(s => s.Weight * s.Score!.Value) / totalWeight;
    }
}

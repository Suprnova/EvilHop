using EvilHop.Common;

namespace EvilHop.Serialization.Sniffing;

/// <summary>
/// Best-effort inference of a HIP/HOP stream's <see cref="Common.GameVersion"/>/<see cref="FormatProfile"/>
/// from its bytes alone - no <see cref="Serializer"/> required up front.
/// </summary>
public static class Sniffer
{
    /// <summary>
    /// Sniffs <paramref name="stream"/>, choosing the game by scoring. A single forward-only, budget-tracked scan, so it works
    /// over non-seekable streams too. Never reads the asset payload itself, and never throws for
    /// malformed input; a truncated or unrecognizable stream instead yields
    /// <see cref="SniffConfidence.Unrecognized"/>.
    /// </summary>
    /// <param name="stream">
    /// The stream to sniff. Its <see cref="Stream.Position"/> is restored afterward when
    /// <see cref="Stream.CanSeek"/> is <see langword="true"/>; a non-seekable stream is left
    /// consumed, since those bytes can't be un-read from the real source.
    /// </param>
    /// <returns>The inferred <see cref="SniffResult"/>.</returns>
    public static SniffResult Sniff(Stream stream) => Sniff(stream, null);

    /// <summary>
    /// Sniffs <paramref name="stream"/> for a caller-stated <paramref name="game"/>: no game is scored
    /// or chosen, so what remains to learn is the platform. The result is
    /// <see cref="SniffResult.IsStrong"/> when the stream names it. Otherwise behaves as
    /// <see cref="Sniff(Stream)"/>.
    /// </summary>
    /// <param name="stream"><inheritdoc cref="Sniff(Stream)" path="/param[@name='stream']"/></param>
    /// <param name="game">The game the caller states the stream was built for.</param>
    /// <returns>The inferred <see cref="SniffResult"/>.</returns>
    public static SniffResult Sniff(Stream stream, GameVersion game) => Sniff(stream, (GameVersion?)game);

    private static SniffResult Sniff(Stream stream, GameVersion? game)
    {
        ArgumentNullException.ThrowIfNull(stream);

        long? originalPosition = stream.CanSeek ? stream.Position : null;
        try
        {
            var (signals, gatePassed) = SniffScanner.Scan(stream);
            if (!gatePassed)
                return new SniffResult(SniffConfidence.Unrecognized, [], signals, PlatformSource.None, game is not null);

            return game is { } fixedGame ? SniffScorer.ScoreFixed(fixedGame, signals) : SniffScorer.Score(signals);
        }
        finally
        {
            if (originalPosition is long position) stream.Position = position;
        }
    }
}

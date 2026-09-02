namespace EvilHop.Serialization.Sniffing;

/// <summary>
/// Best-effort inference of a HIP/HOP stream's <see cref="Common.GameVersion"/>/<see cref="FormatProfile"/>
/// from its bytes alone - no <see cref="Serializer"/> required up front.
/// </summary>
public static class Sniffer
{
    /// <summary>
    /// Sniffs <paramref name="stream"/>. A single forward-only, budget-tracked scan, so it works
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
    public static SniffResult Sniff(Stream stream)
    {
        long? originalPosition = stream.CanSeek ? stream.Position : null;
        try
        {
            var (signals, gatePassed) = SniffScanner.Scan(stream);
            return gatePassed
                ? SniffScorer.Score(signals)
                : new SniffResult(SniffConfidence.Unrecognized, [], signals);
        }
        finally
        {
            if (originalPosition is long position) stream.Position = position;
        }
    }
}

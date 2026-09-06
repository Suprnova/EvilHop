using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Serialization.Sniffing;

/// <summary>
/// Walks a HIP/HOP stream's block envelope structure to gather <see cref="SniffSignals"/>, without
/// needing to know which game or <see cref="FormatProfile"/> it belongs to yet.
/// </summary>
internal static class SniffScanner
{
    /// <summary>
    /// Scans <paramref name="stream"/>: verifies the <c>HIPA</c>/<c>PACK</c> gate, then - if it
    /// passes - walks <c>PACK</c>, <c>DICT</c>, and <c>STRM</c> for whatever signals can be gathered.
    /// Never throws; a truncated or malformed structure past the gate simply stops early and returns
    /// whatever was gathered before the failure.
    /// </summary>
    /// <returns>
    /// The gathered <see cref="SniffSignals"/>, and whether the <c>HIPA</c>/<c>PACK</c> gate passed
    /// at all. <see langword="false"/> means the stream isn't recognizable as a HIP archive at all,
    /// and should be treated as <see cref="SniffConfidence.Unrecognized"/> rather than scored.
    /// </returns>
    public static (SniffSignals Signals, bool GatePassed) Scan(Stream stream)
    {
        var counting = new CountingStream(stream);
        using var reader = new EndianReader(counting, Endianness.Big, leaveOpen: true);

        bool hasHipaMagic = false;
        try
        {
            var hipa = ReadEnvelope(reader);
            if (hipa.Tag != "HIPA" || hipa.Size != 0)
                return (Empty(hasHipaMagic: false), false);
            hasHipaMagic = true;

            var pack = ReadEnvelope(reader);
            if (pack.Tag != "PACK")
                return (Empty(hasHipaMagic: true), false);

            var signals = new SniffSignalsBuilder { HasHipaMagic = true };
            try
            {
                WalkPack(reader, pack, signals);
                WalkDict(reader, signals);
                WalkStrm(reader, signals);
            }
            catch
            {
                // Truncated or malformed past the HIPA/PACK gate - score whatever signals were
                // gathered before the failure, rather than throwing.
            }

            return (signals.Build(), true);
        }
        catch
        {
            return (Empty(hasHipaMagic), false);
        }
    }

    private static SniffSignals Empty(bool hasHipaMagic) =>
        new(hasHipaMagic, null, null, [], null, [], [], new HashSet<string>(), null);

    private static void WalkPack(EndianReader reader, BlockEnvelope pack, SniffSignalsBuilder signals)
    {
        while (reader.BaseStream.Position < pack.ContentEnd)
        {
            var child = ReadEnvelope(reader);
            switch (child.Tag)
            {
                case "PVER":
                    reader.ReadUInt32(); // SubVersion, unused
                    signals.ClientVersion = (ClientVersion)reader.ReadUInt32();
                    break;
                case "PFLG":
                    signals.Flags = (PackFlags)reader.ReadUInt32();
                    break;
                case "PCRT":
                    signals.Created = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32());
                    break;
                case "PLAT":
                    // Slot 0 is PlatformId under both PlatformFieldOrder values, so the strings can
                    // be read positionally without knowing which order this archive uses yet.
                    while (reader.BaseStream.Position < child.ContentEnd)
                        signals.PlatformStrings.Add(reader.ReadEvilString());
                    break;
            }
            SkipToEnd(reader, child);
        }
    }

    private static void WalkDict(EndianReader reader, SniffSignalsBuilder signals)
    {
        var dict = ReadEnvelope(reader);
        if (dict.Tag != "DICT")
            throw new FormatException($"Expected 'DICT', found '{dict.Tag}'.");

        while (reader.BaseStream.Position < dict.ContentEnd)
        {
            var child = ReadEnvelope(reader);
            switch (child.Tag)
            {
                case "ATOC":
                    WalkAtoc(reader, child, signals);
                    break;
                case "LTOC":
                    WalkLtoc(reader, child, signals);
                    break;
            }
            SkipToEnd(reader, child);
        }
    }

    private static void WalkAtoc(EndianReader reader, BlockEnvelope atoc, SniffSignalsBuilder signals)
    {
        while (reader.BaseStream.Position < atoc.ContentEnd)
        {
            var child = ReadEnvelope(reader);
            if (child.Tag == "AHDR")
            {
                reader.ReadUInt32(); // Id, unused
                signals.AssetTypes.Add(Serializer.ReadTag(reader)); // Type, kept as a raw FourCC - ADBG is never entered
                signals.AssetHeaderCount++;
            }
            SkipToEnd(reader, child);
        }
    }

    private static void WalkLtoc(EndianReader reader, BlockEnvelope ltoc, SniffSignalsBuilder signals)
    {
        while (reader.BaseStream.Position < ltoc.ContentEnd)
        {
            var child = ReadEnvelope(reader);
            if (child.Tag == "LHDR")
            {
                signals.LayerTypeRawValues.Add(reader.ReadUInt32());
                uint assetCount = reader.ReadUInt32();
                reader.ReadBytes(checked((int)(assetCount * 4))); // discard the asset ids

                var ldbg = ReadEnvelope(reader);
                if (ldbg.Tag == "LDBG")
                    signals.LayerDebugValues.Add(reader.ReadUInt32());
                SkipToEnd(reader, ldbg);
            }
            SkipToEnd(reader, child);
        }
    }

    private static void WalkStrm(EndianReader reader, SniffSignalsBuilder signals)
    {
        var strm = ReadEnvelope(reader);
        if (strm.Tag != "STRM")
            throw new FormatException($"Expected 'STRM', found '{strm.Tag}'.");

        while (reader.BaseStream.Position < strm.ContentEnd)
        {
            var child = ReadEnvelope(reader);
            if (child.Tag == "DPAK" && signals.AssetHeaderCount != 0 && child.Size >= 8)
            {
                // Never reads the asset payload itself - just enough to see whether the padding
                // fill run is present.
                byte[] peek = reader.ReadBytes(8);
                bool allPaddingFill = peek[4] == 0x33 && peek[5] == 0x33 && peek[6] == 0x33 && peek[7] == 0x33;
                signals.DpakPaddingObserved = allPaddingFill ? true : null; // not a positive "false" - see SniffScorer
            }
            SkipToEnd(reader, child);
        }
    }

    private static BlockEnvelope ReadEnvelope(EndianReader reader)
    {
        string tag = Serializer.ReadTag(reader);
        uint size = reader.ReadUInt32();
        return new BlockEnvelope(tag, size, reader.BaseStream.Position);
    }

    private static void SkipToEnd(EndianReader reader, BlockEnvelope envelope)
    {
        long remaining = envelope.ContentEnd - reader.BaseStream.Position;
        if (remaining < 0)
            throw new FormatException($"Block '{envelope.Tag}' overran its declared size.");
        if (remaining > 0)
            reader.ReadBytes((int)remaining);
    }

    private readonly record struct BlockEnvelope(string Tag, uint Size, long ContentStart)
    {
        public long ContentEnd => ContentStart + Size;
    }

    /// <summary>
    /// Accumulates <see cref="SniffSignals"/> while scanning, plus the AHDR count used only to
    /// decide whether <see cref="WalkStrm"/> should peek the DPAK block at all.
    /// </summary>
    private sealed class SniffSignalsBuilder
    {
        public bool HasHipaMagic { get; init; }
        public ClientVersion? ClientVersion { get; set; }
        public PackFlags? Flags { get; set; }
        public List<string> PlatformStrings { get; } = [];
        public DateTimeOffset? Created { get; set; }
        public List<uint> LayerTypeRawValues { get; } = [];
        public List<uint> LayerDebugValues { get; } = [];
        public HashSet<string> AssetTypes { get; } = [];
        public bool? DpakPaddingObserved { get; set; }
        public int AssetHeaderCount { get; set; }

        public SniffSignals Build() => new(
            HasHipaMagic, ClientVersion, Flags, PlatformStrings, Created,
            LayerTypeRawValues, LayerDebugValues, AssetTypes, DpakPaddingObserved);
    }
}

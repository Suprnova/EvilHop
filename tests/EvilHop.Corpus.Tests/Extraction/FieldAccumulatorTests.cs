using EvilHop.Corpus.Extraction;

namespace EvilHop.Corpus.Tests.Extraction;

public class FieldAccumulatorTests
{
    [Fact]
    public void ToSummary_UnderCap_RecordsFullSet()
    {
        var accumulator = new FieldAccumulator(ValueKind.Numeric);

        accumulator.Record(1u, "n100f/release", "n100f/release/a.HIP");
        accumulator.Record(2u, "n100f/release", "n100f/release/a.HIP");
        accumulator.Record(1u, "bfbb/release", "bfbb/release/b.HIP");

        var summary = accumulator.ToSummary();

        Assert.Equal("set", summary.Kind);
        Assert.Equal(2, summary.Values!.Count);
        Assert.Equal(2, summary.Values["1"].Count);
        Assert.Equal(["bfbb/release", "n100f/release"], summary.Values["1"].Builds.OrderBy(b => b));
        Assert.Equal("n100f/release/a.HIP", summary.Values["1"].Exemplar);
        Assert.Equal(1, summary.Values["2"].Count);
    }

    [Fact]
    public void ToSummary_AtCapBoundary_StaysSet()
    {
        var accumulator = new FieldAccumulator(ValueKind.Numeric);
        for (uint i = 0; i < 64; i++) accumulator.Record(i, "build", "path");

        var summary = accumulator.ToSummary();

        Assert.Equal("set", summary.Kind);
        Assert.Equal(64, summary.Values!.Count);
    }

    [Fact]
    public void ToSummary_OverCap_DegradesToSummaryWithExactDistinctAndMinMax()
    {
        var accumulator = new FieldAccumulator(ValueKind.Numeric);
        for (uint i = 0; i < 65; i++) accumulator.Record(i, "build", "path");

        var summary = accumulator.ToSummary();

        Assert.Equal("summary", summary.Kind);
        Assert.Null(summary.Values);
        Assert.Equal(65, summary.Distinct);
        Assert.Equal(0u, summary.Min!.GetValue<uint>());
        Assert.Equal(64u, summary.Max!.GetValue<uint>());
    }

    [Fact]
    public void ToSummary_TextOverCap_RecordsLengthRangeNotMinMax()
    {
        var accumulator = new FieldAccumulator(ValueKind.Text);
        for (int i = 0; i < 65; i++) accumulator.Record(new string('a', i), "build", "path");

        var summary = accumulator.ToSummary();

        Assert.Equal("summary", summary.Kind);
        Assert.Null(summary.Min);
        Assert.Null(summary.Max);
        Assert.Equal(0, summary.MinLength);
        Assert.Equal(64, summary.MaxLength);
    }

    [Fact]
    public void ToSummary_CollectionOverCap_RecordsElementCountRange()
    {
        var accumulator = new FieldAccumulator(ValueKind.Collection);
        for (int i = 0; i < 65; i++) accumulator.Record(Enumerable.Range(0, i).ToArray(), "build", "path");

        var summary = accumulator.ToSummary();

        Assert.Equal("summary", summary.Kind);
        Assert.Equal(0, summary.MinLength);
        Assert.Equal(64, summary.MaxLength);
    }

    [Fact]
    public void ToSummary_Bytes_NeverRecordsContentsOnlyLengthRange()
    {
        var accumulator = new FieldAccumulator(ValueKind.Bytes);
        accumulator.Record(new byte[3], "build", "path");
        accumulator.Record(new byte[7], "build", "path");
        accumulator.Record(new byte[7], "build", "path");

        var summary = accumulator.ToSummary();

        Assert.Equal("summary", summary.Kind);
        Assert.Null(summary.Values);
        Assert.Null(summary.Distinct);
        Assert.Equal(3, summary.MinLength);
        Assert.Equal(7, summary.MaxLength);
    }

    [Fact]
    public void ToSummary_PrintableFourCcEnumKey_FormatsAsAscii()
    {
        var accumulator = new FieldAccumulator(ValueKind.Numeric);

        // Texture = 0x52575458, which reads as the ASCII FourCC "RWTX".
        accumulator.Record(EvilHop.Common.AssetType.Texture, "build", "path");

        var summary = accumulator.ToSummary();

        Assert.Equal(["RWTX"], summary.Values!.Keys);
    }

    [Fact]
    public void ToSummary_NonPrintableEnumKey_FallsBackToHex()
    {
        var accumulator = new FieldAccumulator(ValueKind.Numeric);

        // Unknown = 0x00000000, all NUL bytes - not printable ASCII.
        accumulator.Record(EvilHop.Common.AssetType.Unknown, "build", "path");

        var summary = accumulator.ToSummary();

        Assert.Equal(["0x00000000"], summary.Values!.Keys);
    }

    [Fact]
    public void ToJson_PlainNumericKeys_SortByMagnitudeNotLexicographically()
    {
        var accumulator = new FieldAccumulator(ValueKind.Numeric);
        foreach (uint value in new uint[] { 0, 10, 12, 2, 20 })
            accumulator.Record(value, "build", "path");

        var json = accumulator.ToSummary().ToJson();

        Assert.Equal(["0", "2", "10", "12", "20"], json["values"]!.AsObject().Select(p => p.Key));
    }

    [Fact]
    public void ToSummary_HexKind_FormatsKeyAsTwoDigitHex()
    {
        var accumulator = new FieldAccumulator(ValueKind.Hex);

        accumulator.Record((byte)0x33, "build", "path");

        var summary = accumulator.ToSummary();

        Assert.Equal(["0x33"], summary.Values!.Keys);
    }

    [Fact]
    public void ToJson_HexKind_SortsByMagnitude()
    {
        var accumulator = new FieldAccumulator(ValueKind.Hex);
        foreach (byte value in new byte[] { 0x33, 0x02, 0xFF })
            accumulator.Record(value, "build", "path");

        var json = accumulator.ToSummary().ToJson();

        Assert.Equal(["0x02", "0x33", "0xFF"], json["values"]!.AsObject().Select(p => p.Key));
    }
}

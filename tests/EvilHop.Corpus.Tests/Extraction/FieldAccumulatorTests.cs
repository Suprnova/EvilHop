using EvilHop.Corpus.Extraction;

namespace EvilHop.Corpus.Tests.Extraction;

public class FieldAccumulatorTests
{
    [Fact]
    public void ToSummary_UnderCap_RecordsFullSet()
    {
        var accumulator = new FieldAccumulator(FieldKind.Numeric, "Test.Field");

        accumulator.Record(1u, "n100f/release", "n100f/release/a.HIP");
        accumulator.Record(2u, "n100f/release", "n100f/release/a.HIP");
        accumulator.Record(1u, "bfbb/release", "bfbb/release/b.HIP");

        var set = Assert.IsType<ValueSet>(accumulator.ToSummary());

        Assert.Equal(2, set.Values.Count);
        Assert.Equal(2, set.Values["1"].Count);
        Assert.Equal(["bfbb/release", "n100f/release"], set.Values["1"].Builds.OrderBy(b => b));
        Assert.Equal("n100f/release/a.HIP", set.Values["1"].Exemplar);
        Assert.Equal(1, set.Values["2"].Count);
    }

    [Fact]
    public void ToSummary_AtCapBoundary_StaysSet()
    {
        var accumulator = new FieldAccumulator(FieldKind.Numeric, "Test.Field");
        for (uint i = 0; i < 70; i++) accumulator.Record(i, "build", "path");

        var set = Assert.IsType<ValueSet>(accumulator.ToSummary());

        Assert.Equal(70, set.Values.Count);
    }

    [Fact]
    public void ToSummary_OverCapButNeverDegradesField_StaysSet()
    {
        var accumulator = new FieldAccumulator(FieldKind.Numeric, "AssetHeader.Type");
        for (uint i = 0; i < 71; i++) accumulator.Record(i, "build", "path");

        var set = Assert.IsType<ValueSet>(accumulator.ToSummary());

        Assert.Equal(71, set.Values.Count);
    }

    [Fact]
    public void ToSummary_OverCap_DegradesToSummaryWithExactDistinctAndMinMax()
    {
        var accumulator = new FieldAccumulator(FieldKind.Numeric, "Test.Field");
        for (uint i = 0; i < 71; i++) accumulator.Record(i, "build", "path");

        var digest = Assert.IsType<ValueDigest>(accumulator.ToSummary());
        var json = digest.ToJson();

        Assert.Equal(71, digest.Distinct);
        Assert.Equal(0u, json["min"]!.GetValue<uint>());
        Assert.Equal(70u, json["max"]!.GetValue<uint>());
    }

    [Fact]
    public void ToSummary_TextOverCap_RecordsLengthRangeNotMinMax()
    {
        var accumulator = new FieldAccumulator(FieldKind.Text, "Test.Field");
        for (int i = 0; i < 71; i++) accumulator.Record(new string('a', i), "build", "path");

        var digest = Assert.IsType<ValueDigest>(accumulator.ToSummary());
        var json = digest.ToJson();

        Assert.False(json.ContainsKey("min"));
        Assert.False(json.ContainsKey("max"));
        Assert.Equal(0, json["minLength"]!.GetValue<int>());
        Assert.Equal(70, json["maxLength"]!.GetValue<int>());
    }

    [Fact]
    public void ToSummary_CollectionOverCap_RecordsElementCountRange()
    {
        var accumulator = new FieldAccumulator(FieldKind.Collection, "Test.Field");
        for (int i = 0; i < 71; i++) accumulator.Record(Enumerable.Range(0, i).ToArray(), "build", "path");

        var digest = Assert.IsType<ValueDigest>(accumulator.ToSummary());
        var json = digest.ToJson();

        Assert.Equal(0, json["minLength"]!.GetValue<int>());
        Assert.Equal(70, json["maxLength"]!.GetValue<int>());
    }

    [Fact]
    public void ToSummary_Bytes_NeverRecordsContentsOnlyLengthRange()
    {
        var accumulator = new FieldAccumulator(FieldKind.Bytes, "Test.Field");
        accumulator.Record(new byte[3], "build", "path");
        accumulator.Record(new byte[7], "build", "path");
        accumulator.Record(new byte[7], "build", "path");

        var digest = Assert.IsType<ValueDigest>(accumulator.ToSummary());
        var json = digest.ToJson();

        Assert.Null(digest.Distinct);
        Assert.False(json.ContainsKey("distinct"));
        Assert.Equal(3, json["minLength"]!.GetValue<int>());
        Assert.Equal(7, json["maxLength"]!.GetValue<int>());
    }

    [Fact]
    public void ToSummary_PrintableFourCcEnumKey_FormatsAsAscii()
    {
        var accumulator = new FieldAccumulator(FieldKind.Numeric, "Test.Field");

        // Texture = 0x52575458, which reads as the ASCII FourCC "RWTX".
        accumulator.Record(EvilHop.Common.AssetType.Texture, "build", "path");

        var set = Assert.IsType<ValueSet>(accumulator.ToSummary());

        Assert.Equal(["RWTX"], set.Values.Keys);
    }

    [Fact]
    public void ToSummary_NonPrintableEnumKey_FallsBackToHex()
    {
        var accumulator = new FieldAccumulator(FieldKind.Numeric, "Test.Field");
        accumulator.Record((EvilHop.Common.AssetType)0x00000000, "build", "path");

        var set = Assert.IsType<ValueSet>(accumulator.ToSummary());

        Assert.Equal(["0x00000000"], set.Values.Keys);
    }

    [Fact]
    public void ToJson_PlainNumericKeys_SortByMagnitudeNotLexicographically()
    {
        var accumulator = new FieldAccumulator(FieldKind.Numeric, "Test.Field");
        foreach (uint value in new uint[] { 0, 10, 12, 2, 20 })
            accumulator.Record(value, "build", "path");

        var json = accumulator.ToSummary().ToJson();

        Assert.Equal(["0", "2", "10", "12", "20"], json["values"]!.AsObject().Select(p => p.Key));
    }

    [Fact]
    public void ToSummary_HexKind_FormatsKeyAsTwoDigitHex()
    {
        var accumulator = new FieldAccumulator(FieldKind.Hex, "Test.Field");

        accumulator.Record((byte)0x33, "build", "path");

        var set = Assert.IsType<ValueSet>(accumulator.ToSummary());

        Assert.Equal(["0x33"], set.Values.Keys);
    }

    [Fact]
    public void ToJson_HexKind_SortsByMagnitude()
    {
        var accumulator = new FieldAccumulator(FieldKind.Hex, "Test.Field");
        foreach (byte value in new byte[] { 0x33, 0x02, 0xFF })
            accumulator.Record(value, "build", "path");

        var json = accumulator.ToSummary().ToJson();

        Assert.Equal(["0x02", "0x33", "0xFF"], json["values"]!.AsObject().Select(p => p.Key));
    }
}

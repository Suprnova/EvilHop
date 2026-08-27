using EvilHop.Blocks;
using EvilHop.Corpus.Archives;
using EvilHop.Corpus.Invariants;

namespace EvilHop.Corpus.Tests.Invariants;

public class PaddingIsHomogeneousInvariantTests
{
    private static ArchiveContext ArchiveOf(params Block[] roots) => new()
    {
        BuildKey = "n100f/release",
        RelativePath = "n100f/release/boot.HIP",
        Roots = roots,
        ArchiveLength = 0
    };

    private static StreamData PaddedWith(byte fill, int length)
    {
        var data = BlockFactory.Create<StreamData>();
        data.Padding = [.. Enumerable.Repeat(fill, length)];
        return data;
    }

    [Fact]
    public void Check_SingleRepeatedFillByte_Passes()
    {
        var invariant = new PaddingIsHomogeneousInvariant();

        invariant.Check(ArchiveOf(PaddedWith(0x33, 8)));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_MixedPaddingBytes_RecordsViolation()
    {
        var data = BlockFactory.Create<StreamData>();
        data.Padding = [0x33, 0x33, 0x00];

        var invariant = new PaddingIsHomogeneousInvariant();
        invariant.Check(ArchiveOf(data));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }

    [Fact]
    public void ToJson_FillBytes_FormatsValuesAsHexNotDecimal()
    {
        var invariant = new PaddingIsHomogeneousInvariant();

        invariant.Check(ArchiveOf(PaddedWith(0x33, 4)));

        var fillBytes = invariant.ToJson()["fillBytes"]!;
        Assert.Equal("set", (string)fillBytes["kind"]!);
        Assert.Equal(["0x33"], fillBytes["values"]!.AsObject().Select(p => p.Key));
    }
}

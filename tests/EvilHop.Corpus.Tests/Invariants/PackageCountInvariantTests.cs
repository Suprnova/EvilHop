using EvilHop.Blocks;
using EvilHop.Corpus.Archives;
using EvilHop.Corpus.Invariants;

namespace EvilHop.Corpus.Tests.Invariants;

file static class TestArchive
{
    public static ArchiveContext Of(params Block[] roots) => new()
    {
        BuildKey = "n100f/release",
        RelativePath = "n100f/release/boot.HIP",
        Roots = roots,
        ArchiveLength = 0
    };
}

public class PackageCountsMatchTreeInvariantTests
{
    [Fact]
    public void Check_CountsMatchTree_Passes()
    {
        var counts = BlockFactory.Create<PackageCount>();
        counts.AssetCount = 1;
        counts.LayerCount = 1;

        var header = BlockFactory.CreateAssetHeader(1, "a");
        var layer = BlockFactory.Create<LayerHeader>();

        var invariant = new PackageCountsMatchTreeInvariant();
        invariant.Check(TestArchive.Of(counts, header, layer));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(2, outcomes["passing"]!.GetValue<long>());
        Assert.Equal(0, outcomes["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_AssetCountDoesNotMatchTree_RecordsViolation()
    {
        var counts = BlockFactory.Create<PackageCount>();
        counts.AssetCount = 2;

        var header = BlockFactory.CreateAssetHeader(1, "a");

        var invariant = new PackageCountsMatchTreeInvariant();
        invariant.Check(TestArchive.Of(counts, header));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }
}

public class PackageMaxSizesMatchTreeInvariantTests
{
    [Fact]
    public void Check_MaxAssetSizeMatchesLargestAsset_Passes()
    {
        var counts = BlockFactory.Create<PackageCount>();
        counts.MaxAssetSize = 20;

        var small = BlockFactory.CreateAssetHeader(1, "a", size: 10);
        var large = BlockFactory.CreateAssetHeader(2, "b", size: 20);

        var invariant = new PackageMaxSizesMatchTreeInvariant();
        invariant.Check(TestArchive.Of(counts, small, large));

        Assert.Equal(0, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_MaxAssetSizeDoesNotMatchLargestAsset_RecordsViolation()
    {
        var counts = BlockFactory.Create<PackageCount>();
        counts.MaxAssetSize = 999;

        var header = BlockFactory.CreateAssetHeader(1, "a", size: 10);

        var invariant = new PackageMaxSizesMatchTreeInvariant();
        invariant.Check(TestArchive.Of(counts, header));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_MaxXFormAssetSizeIgnoresAssetsWithoutReadTransform()
    {
        var counts = BlockFactory.Create<PackageCount>();
        counts.MaxAssetSize = 999;
        counts.MaxXFormAssetSize = 10;

        var transformed = BlockFactory.CreateAssetHeader(1, "a", size: 10);
        transformed.Flags = AssetFlags.ReadTransform;
        var untransformed = BlockFactory.CreateAssetHeader(2, "b", size: 999);

        var invariant = new PackageMaxSizesMatchTreeInvariant();
        invariant.Check(TestArchive.Of(counts, transformed, untransformed));

        Assert.Equal(0, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_MaxLayerSizeSumsSizePlusPlusAcrossAssetIds()
    {
        // Layer size is Σ(Size + Plus) over the layer's own AssetIds listing, not a first-to-last
        // byte extent - the two only disagree when an ID repeats or the last asset has non-zero
        // Plus, which n100f/prototype_2001-06-11 does both of (see the invariant's doc comment).
        var counts = BlockFactory.Create<PackageCount>();
        counts.MaxAssetSize = 20;
        counts.MaxLayerSize = 10 + 5 + 20 + 0;

        var first = BlockFactory.CreateAssetHeader(1, "a", size: 10, plus: 5);
        var last = BlockFactory.CreateAssetHeader(2, "b", size: 20, plus: 0);
        var layer = BlockFactory.Create<LayerHeader>();
        layer.AssetIds = [1, 2];

        var invariant = new PackageMaxSizesMatchTreeInvariant();
        invariant.Check(TestArchive.Of(counts, first, last, layer));

        Assert.Equal(0, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_MaxLayerSizeCountsRepeatedAssetIdOncePerListing()
    {
        var counts = BlockFactory.Create<PackageCount>();
        counts.MaxAssetSize = 10;
        counts.MaxLayerSize = (10 + 5) * 2; // "a" listed twice

        var header = BlockFactory.CreateAssetHeader(1, "a", size: 10, plus: 5);
        var layer = BlockFactory.Create<LayerHeader>();
        layer.AssetIds = [1, 1];

        var invariant = new PackageMaxSizesMatchTreeInvariant();
        invariant.Check(TestArchive.Of(counts, header, layer));

        Assert.Equal(0, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_MaxLayerSizeTakesLargestAcrossLayers()
    {
        var counts = BlockFactory.Create<PackageCount>();
        counts.MaxAssetSize = 30;
        counts.MaxLayerSize = 30;

        var small = BlockFactory.CreateAssetHeader(1, "a", size: 10);
        var large = BlockFactory.CreateAssetHeader(2, "b", size: 30);

        var smallLayer = BlockFactory.Create<LayerHeader>();
        smallLayer.AssetIds = [1];
        var largeLayer = BlockFactory.Create<LayerHeader>();
        largeLayer.AssetIds = [2];

        var invariant = new PackageMaxSizesMatchTreeInvariant();
        invariant.Check(TestArchive.Of(counts, small, large, smallLayer, largeLayer));

        Assert.Equal(0, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_MaxLayerSizeDoesNotMatchComputedSum_RecordsViolation()
    {
        var counts = BlockFactory.Create<PackageCount>();
        counts.MaxAssetSize = 10;
        counts.MaxLayerSize = 999;

        var header = BlockFactory.CreateAssetHeader(1, "a", size: 10);
        var layer = BlockFactory.Create<LayerHeader>();
        layer.AssetIds = [1];

        var invariant = new PackageMaxSizesMatchTreeInvariant();
        invariant.Check(TestArchive.Of(counts, header, layer));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }
}

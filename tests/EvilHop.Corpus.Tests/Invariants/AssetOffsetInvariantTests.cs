using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Corpus.Invariants;

namespace EvilHop.Corpus.Tests.Invariants;

file static class TestArchive
{
    public static ArchiveContext Of(long archiveLength, params Block[] roots) => new()
    {
        BuildKey = "n100f/release",
        RelativePath = "n100f/release/boot.HIP",
        Roots = roots,
        ArchiveLength = archiveLength
    };
}

public class AssetChecksumMatchesDataInvariantTests
{
    private static (AssetHeader Header, StreamData Data) BuildAsset(byte[] fullData, uint offset, uint size, uint checksum)
    {
        var streamData = BlockFactory.Create<StreamData>();
        streamData.Data = fullData;

        var header = BlockFactory.CreateAssetHeader(1, "a", offset, size);
        header.Debug.Checksum = checksum;
        return (header, streamData);
    }

    [Fact]
    public void Check_ChecksumMatchesComputedCrc_Passes()
    {
        byte[] data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var (header, streamData) = BuildAsset(data, offset: 90, size: 5, checksum: Crc32Mpeg2.Compute(data.AsSpan(0, 5)));

        var invariant = new AssetChecksumMatchesDataInvariant();
        invariant.Check(TestArchive.Of(archiveLength: 100, header, streamData));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(1, outcomes["passing"]!.GetValue<long>());
        Assert.Equal(0, outcomes["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_ChecksumDoesNotMatch_RecordsViolation()
    {
        byte[] data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var (header, streamData) = BuildAsset(data, offset: 90, size: 5, checksum: 0xDEADBEEF);

        var invariant = new AssetChecksumMatchesDataInvariant();
        invariant.Check(TestArchive.Of(archiveLength: 100, header, streamData));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(1, outcomes["violated"]!.GetValue<long>());
    }
}

public class AssetOffsetsInBoundsInvariantTests
{
    [Fact]
    public void Check_OffsetSizePlusWithinArchiveLength_Passes()
    {
        var header = BlockFactory.CreateAssetHeader(1, "a", offset: 80, size: 10, plus: 5);
        var invariant = new AssetOffsetsInBoundsInvariant();

        invariant.Check(TestArchive.Of(archiveLength: 95, header));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_OffsetSizePlusExceedsArchiveLength_RecordsViolation()
    {
        var header = BlockFactory.CreateAssetHeader(1, "a", offset: 80, size: 10, plus: 10);
        var invariant = new AssetOffsetsInBoundsInvariant();

        invariant.Check(TestArchive.Of(archiveLength: 95, header));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }
}

public class PlusMatchesAlignmentInvariantTests
{
    private static AssetHeader Header(uint id, uint offset, uint size, uint plus, int alignment)
    {
        var header = BlockFactory.CreateAssetHeader(id, "a", offset, size, plus);
        header.Debug.Alignment = alignment;
        return header;
    }

    [Fact]
    public void Check_PlusPadsEndToNextAssetsAlignmentBoundary_Passes()
    {
        // offset 0 + size 10 = 10; next 32-byte boundary is 32, so plus should be 22.
        var header = Header(id: 1, offset: 0, size: 10, plus: 22, alignment: 4);
        var next = Header(id: 2, offset: 32, size: 0, plus: 0, alignment: 32);
        var invariant = new PlusMatchesAlignmentInvariant();

        invariant.Check(TestArchive.Of(archiveLength: 1000, header, next));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_PlusMatchesOwnAlignmentButNotNextAssets_RecordsViolation()
    {
        // Plus pads to this asset's own 32-byte alignment (22), but the next asset needs 4-byte
        // alignment, which offset 10 already satisfies - the next-asset rule expects 0, not 22.
        var header = Header(id: 1, offset: 0, size: 10, plus: 22, alignment: 32);
        var next = Header(id: 2, offset: 32, size: 0, plus: 0, alignment: 4);
        var invariant = new PlusMatchesAlignmentInvariant();

        invariant.Check(TestArchive.Of(archiveLength: 1000, header, next));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_NextAssetDeclaresNonPositiveAlignment_FallsBackToSixteenByteDefault()
    {
        // offset 0 + size 10 = 10; the next asset's alignment isn't usable (-1 means "use the
        // type's default"), so the 16-byte default applies - next 16-byte boundary is 16, plus 6.
        var header = Header(id: 1, offset: 0, size: 10, plus: 6, alignment: 4);
        var next = Header(id: 2, offset: 16, size: 0, plus: 0, alignment: -1);
        var invariant = new PlusMatchesAlignmentInvariant();

        invariant.Check(TestArchive.Of(archiveLength: 1000, header, next));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_OwnAlignmentNonPositive_IsSkipped()
    {
        var header = Header(id: 1, offset: 0, size: 10, plus: 0, alignment: -1);
        var next = Header(id: 2, offset: 10, size: 0, plus: 0, alignment: 4);
        var invariant = new PlusMatchesAlignmentInvariant();

        invariant.Check(TestArchive.Of(archiveLength: 1000, header, next));

        Assert.Equal(0, invariant.ToJson()["checked"]!.GetValue<long>());
    }

    [Fact]
    public void Check_LastAssetInLayerWithZeroPlus_IsSkippedRegardlessOfAlignment()
    {
        // A layer's own trailing padding is real, but never attributed to an asset's Plus - only
        // non-last assets carry their alignment padding that way.
        var header = Header(id: 1, offset: 0, size: 10, plus: 0, alignment: 32);
        var next = Header(id: 2, offset: 10, size: 0, plus: 0, alignment: 4);
        var layer = BlockFactory.Create<LayerHeader>();
        layer.AssetIds = [header.Id];
        var invariant = new PlusMatchesAlignmentInvariant();

        invariant.Check(TestArchive.Of(archiveLength: 1000, header, next, layer));

        Assert.Equal(0, invariant.ToJson()["checked"]!.GetValue<long>());
    }

    [Fact]
    public void Check_PhysicallyLastAssetInArchive_IsSkippedForLackOfANextAsset()
    {
        var header = Header(id: 1, offset: 0, size: 10, plus: 0, alignment: 32);
        var invariant = new PlusMatchesAlignmentInvariant();

        invariant.Check(TestArchive.Of(archiveLength: 1000, header));

        Assert.Equal(0, invariant.ToJson()["checked"]!.GetValue<long>());
    }
}

public class LastAssetInLayerHasZeroPlusInvariantTests
{
    private static LayerHeader Layer(params uint[] assetIds)
    {
        var layer = BlockFactory.Create<LayerHeader>();
        layer.AssetIds = assetIds;
        return layer;
    }

    [Fact]
    public void Check_LastAssetInLayerHasZeroPlus_Passes()
    {
        var first = BlockFactory.CreateAssetHeader(1, "a", plus: 5);
        var last = BlockFactory.CreateAssetHeader(2, "b", plus: 0);
        var invariant = new LastAssetInLayerHasZeroPlusInvariant();

        invariant.Check(TestArchive.Of(0, first, last, Layer(1, 2)));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_LastAssetInLayerHasNonZeroPlus_RecordsViolation()
    {
        var first = BlockFactory.CreateAssetHeader(1, "a", plus: 0);
        var last = BlockFactory.CreateAssetHeader(2, "b", plus: 3);
        var invariant = new LastAssetInLayerHasZeroPlusInvariant();

        invariant.Check(TestArchive.Of(0, first, last, Layer(1, 2)));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["violated"]!.GetValue<long>());
    }
}

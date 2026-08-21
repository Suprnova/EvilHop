using EvilHop.Corpus.Invariants;

namespace EvilHop.Corpus.Tests.Invariants;

public class AssetDataLocatorTests
{
    [Fact]
    public void DataStart_ArchiveLongerThanData_ComputesTrailingOffset()
    {
        long start = AssetDataLocator.DataStart(archiveLength: 1000, dataLength: 200);

        Assert.Equal(800, start);
    }

    [Fact]
    public void TryGetRange_OffsetWithinData_ReturnsMatchingRange()
    {
        // A 1000-byte archive whose last 200 bytes are STRM/DPAK.Data; an asset stored at
        // absolute offset 850, size 50, therefore lives at Data[50..100].
        byte[] data = new byte[200];

        bool resolved = AssetDataLocator.TryGetRange(archiveLength: 1000, data, offset: 850, size: 50, out var range);

        Assert.True(resolved);
        Assert.Equal(50, range.Start.Value);
        Assert.Equal(100, range.End.Value);
    }

    [Fact]
    public void TryGetRange_OffsetBeforeDataStart_ReturnsFalse()
    {
        byte[] data = new byte[200];

        bool resolved = AssetDataLocator.TryGetRange(archiveLength: 1000, data, offset: 799, size: 50, out _);

        Assert.False(resolved);
    }

    [Fact]
    public void TryGetRange_SizeExtendsPastDataEnd_ReturnsFalse()
    {
        byte[] data = new byte[200];

        bool resolved = AssetDataLocator.TryGetRange(archiveLength: 1000, data, offset: 970, size: 50, out _);

        Assert.False(resolved);
    }
}

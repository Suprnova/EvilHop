using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Corpus.Archives;
using EvilHop.Serialization;

namespace EvilHop.Corpus.Tests.Archives;

public class RoundTripTests
{
    /// <summary>A <see cref="AssetType.Marker"/>'s twelve bytes: the big-endian floats 1, 2, 3.</summary>
    private static byte[] MarkerBytes => [0x3F, 0x80, 0, 0, 0x40, 0, 0, 0, 0x40, 0x40, 0, 0];

    /// <summary>
    /// A one-asset archive laid out the way EvilHop itself lays one out: assembled at the block
    /// layer, then run through a session so its offsets, padding, and package counts are this
    /// library's own output rather than whatever the hand-built blocks happened to say.
    /// </summary>
    private static byte[] CanonicalArchive(Serializer serializer)
    {
        var roots = BlockFactory.MinimalArchive();
        var dictionary = roots.OfType<EvilHop.Blocks.Dictionary>().Single();

        var header = BlockFactory.CreateAssetHeader(id: 1, name: "marker", size: (uint)MarkerBytes.Length);
        header.Type = AssetType.Marker;
        dictionary.AssetTable.Children.Add(header);

        var layer = BlockFactory.Create<LayerHeader>();
        layer.Debug = BlockFactory.Create<LayerDebug>();
        layer.AssetCount = 1;
        layer.AssetIds = [header.Id];
        dictionary.LayerTable.Children.Add(layer);

        roots.OfType<AssetStream>().Single().Data.Data = MarkerBytes;

        var archive = new Archive(serializer, roots);
        header.Offset = (uint)(Save(archive).Length - MarkerBytes.Length);

        using (archive.OpenAssets()) { }
        return Save(archive);
    }

    private static byte[] Save(Archive archive)
    {
        using var stream = new MemoryStream();
        archive.Save(stream);
        return stream.ToArray();
    }

    private static Archive Load(byte[] bytes, Serializer serializer) =>
        Archive.Load(new MemoryStream(bytes), serializer);

    [Fact]
    public void Check_ArchiveThatReproducesItsOwnBytes_ReturnsNull()
    {
        var serializer = new N100FSerializer();
        byte[] bytes = CanonicalArchive(serializer);

        Assert.Null(RoundTrip.Check(Load(bytes, serializer), bytes));
    }

    [Fact]
    public void Check_BytesTheArchiveDoesNotProduce_ReportsABlockMismatch()
    {
        var serializer = new N100FSerializer();
        byte[] bytes = CanonicalArchive(serializer);

        string? failure = RoundTrip.Check(Load(bytes, serializer), [.. bytes, 0]);

        Assert.Equal("block round-trip byte mismatch.", failure);
    }

    /// <summary>
    /// The case the block-layer diff alone cannot see: the bytes are reproduced exactly, because
    /// nothing ever asked the asset layer to read them.
    /// </summary>
    [Fact]
    public void Check_AssetThatDoesNotParse_ReportsItDespiteTheBytesMatching()
    {
        var serializer = new N100FSerializer();
        var archive = Load(CanonicalArchive(serializer), serializer);
        archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.Single().Size = 0xFFFF;
        byte[] bytes = Save(archive);

        string? failure = RoundTrip.Check(Load(bytes, serializer), bytes);

        Assert.Contains("asset diagnostic", failure);
        Assert.Contains("(Marker)", failure);
    }
}

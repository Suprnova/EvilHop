using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Assets;

public class LayerLayoutTests
{
    public static TheoryData<Platform> AllPlatforms => [Platform.GameCube, Platform.Xbox, Platform.PlayStation2];

    private const int CountersLayer = 0;
    private const int EmptyAssetLayer = 1;
    private const int EmptyLayer = 2;

    /// <summary>
    /// A TSSM archive laid out by the serializer: two 12-byte Counters in one layer with a gap of
    /// flat fill between them, a layer holding only a zero-size asset, and an empty layer.
    /// </summary>
    private static byte[] Fixture(Platform platform)
    {
        var archive = Archive.Create(Serializer.Create(GameVersion.TSSM, platform));
        using (var session = archive.OpenAssets())
        {
            var counters = session.CreateLayer(LayerType.Default);
            counters.Add(new CounterAsset { Id = new AssetId(1), Name = "first" });
            counters.Add(new CounterAsset { Id = new AssetId(2), Name = "second" });
            session.CreateLayer(LayerType.Texture).Add(new EmptyAsset(AssetType.Texture) { Id = new AssetId(3) });
            session.CreateLayer(LayerType.Model);
        }

        using var stream = new MemoryStream();
        archive.Save(stream);
        return stream.ToArray();
    }

    private static Archive Load(byte[] bytes, Platform platform) =>
        Archive.Load(new MemoryStream(bytes), GameVersion.TSSM, platform);

    private static AssetHeader Header(byte[] bytes, Platform platform, uint id) =>
        Load(bytes, platform).Roots.OfType<Dictionary>().Single().AssetTable.Headers.Single(h => h.Id == id);

    private static LayerLayout Counters(Platform platform) =>
        Load(Fixture(platform), platform).ReadAssets().GetLayout(CountersLayer);

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void Entries_AreEachAssetAndTheGapBetween(Platform platform)
    {
        byte[] bytes = Fixture(platform);
        var (first, second) = (Header(bytes, platform, 1), Header(bytes, platform, 2));
        var layout = Load(bytes, platform).ReadAssets().GetLayout(CountersLayer);
        uint gap = second.Offset - first.Offset - first.Size;

        Assert.Equal(
            [
                (typeof(AssetEntry), 0u, first.Size),
                (typeof(GapEntry), first.Size, gap),
                (typeof(AssetEntry), second.Offset - first.Offset, second.Size)
            ],
            layout.Entries.Select(entry => (entry.GetType(), entry.Offset, entry.Size)));
        Assert.Equal(second.Debug.Alignment, ((AssetEntry)layout.Entries[2]).Alignment);
        Assert.Equal((byte)0x33, ((GapEntry)layout.Entries[1]).Fill);
        Assert.Equal(second.Offset + second.Size - first.Offset, layout.Length);
    }

    [Fact]
    public void Entries_GapOfMixedBytes_HasNoFill()
    {
        byte[] bytes = Fixture(Platform.Xbox);
        var first = Header(bytes, Platform.Xbox, 1);
        bytes[first.Offset + first.Size] = 0xAB;

        var layout = Load(bytes, Platform.Xbox).ReadAssets().GetLayout(CountersLayer);

        Assert.Null(((GapEntry)layout.Entries[1]).Fill);
    }

    [Theory]
    [InlineData(EmptyAssetLayer)]
    [InlineData(EmptyLayer)]
    public void Entries_LayerWithoutBytes_IsEmpty(int layer)
    {
        var layout = Load(Fixture(Platform.Xbox), Platform.Xbox).ReadAssets().GetLayout(layer);

        Assert.Empty(layout.Entries);
        Assert.Equal(0u, layout.Length);
        Assert.Null(layout.Locate(0));
    }

    [Theory]
    [InlineData(0u, 0u, true)]
    [InlineData(3u, 3u, true)]
    [InlineData(4u, 4u, false)]
    [InlineData(11u, 11u, false)]
    public void Locate_InTheFirstAsset_GivesItAndHowFarIn(uint offset, uint offsetInEntry, bool firstWord)
    {
        var layout = Counters(Platform.Xbox);

        var location = layout.Locate(offset)!.Value;

        Assert.Equal(1u, location.Asset!.Id.Value);
        Assert.Equal(offsetInEntry, location.OffsetInEntry);
        Assert.Equal(firstWord, location.IsFirstWord);
        Assert.Null(location.Previous);
        Assert.Equal(2u, location.Next!.Asset.Id.Value);
    }

    [Fact]
    public void Locate_InAGap_GivesTheGapAndItsNeighbours()
    {
        var layout = Counters(Platform.Xbox);

        var location = layout.Locate(12)!.Value;

        Assert.IsType<GapEntry>(location.Entry);
        Assert.Null(location.Asset);
        Assert.False(location.IsFirstWord);
        Assert.Equal(1u, location.Previous!.Asset.Id.Value);
        Assert.Equal(2u, location.Next!.Asset.Id.Value);
    }

    [Fact]
    public void Locate_LastByteOfTheImage_IsInTheLastAsset()
    {
        var layout = Counters(Platform.Xbox);

        var location = layout.Locate(layout.Length - 1)!.Value;

        Assert.Equal(2u, location.Asset!.Id.Value);
        Assert.Null(location.Next);
    }

    [Fact]
    public void Locate_PastTheImage_IsNull()
    {
        var layout = Counters(Platform.Xbox);

        Assert.Null(layout.Locate(layout.Length));
    }

    [Fact]
    public void Locate_OverlappingAssets_GivesTheLaterStartingOneThatHoldsTheOffset()
    {
        var outer = new CounterAsset { Id = new AssetId(1) };
        var inner = new CounterAsset { Id = new AssetId(2) };
        var layout = new LayerLayout(0, [new AssetEntry(0, 16, outer, 16), new AssetEntry(2, 4, inner, 16)], new byte[16]);

        Assert.Equal(2, layout.Entries.Count);
        Assert.Same(inner, layout.Locate(3)!.Value.Asset);
        Assert.Same(outer, layout.Locate(8)!.Value.Asset);
    }

    [Fact]
    public void GetLayout_IsBuiltOnceAndKeepsTheLayerAsRead()
    {
        var view = Load(Fixture(Platform.Xbox), Platform.Xbox).ReadAssets();
        view.Layers[CountersLayer].Remove(view.Layers[CountersLayer].Assets[0]);

        var layout = view.GetLayout(CountersLayer);

        Assert.Same(layout, view.GetLayout(CountersLayer));
        Assert.Equal(3, layout.Entries.Count);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void GetLayout_IndexOutsideLayers_ThrowsArgumentOutOfRangeException(int layer)
    {
        var view = Load(Fixture(Platform.Xbox), Platform.Xbox).ReadAssets();

        Assert.Throws<ArgumentOutOfRangeException>(() => view.GetLayout(layer));
    }
}

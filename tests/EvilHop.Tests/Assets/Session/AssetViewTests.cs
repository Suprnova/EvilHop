using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Assets;

public class AssetViewTests
{
    public static TheoryData<Platform> AllPlatforms => [Platform.GameCube, Platform.Xbox, Platform.PlayStation2];

    private const int CountersLayer = 0;
    private const int EmptyAssetLayer = 1;
    private const int EmptyLayer = 2;
    private const byte GapMarker = 0xAB;

    /// <summary>
    /// A TSSM archive laid out by the serializer: two Counters in one layer, with the gap between
    /// them overwritten by <see cref="GapMarker"/> so it isn't flat fill, a layer holding only a
    /// zero-size asset, and an empty layer.
    /// </summary>
    private static byte[] Fixture(Platform platform)
    {
        var archive = Archive.Create(Serializer.Create(GameVersion.TSSM, platform));
        using (var session = archive.OpenAssets())
        {
            var counters = session.CreateLayer(LayerType.Default);
            counters.Add(new CounterAsset { Id = new AssetId(1), Name = "first", InitialValue = 3 });
            counters.Add(new CounterAsset { Id = new AssetId(2), Name = "second", InitialValue = 5 });
            session.CreateLayer(LayerType.Texture).Add(new EmptyAsset(AssetType.Texture) { Id = new AssetId(3) });
            session.CreateLayer(LayerType.Model);
        }

        byte[] bytes = Save(archive);
        var (first, second) = (Header(bytes, platform, 1), Header(bytes, platform, 2));
        bytes.AsSpan((int)(first.Offset + first.Size), (int)(second.Offset - first.Offset - first.Size)).Fill(GapMarker);
        return bytes;
    }

    private static Archive Load(byte[] bytes, Platform platform) =>
        Archive.Load(new MemoryStream(bytes), GameVersion.TSSM, platform);

    private static AssetHeader Header(byte[] bytes, Platform platform, uint id) =>
        Load(bytes, platform).Roots.OfType<Dictionary>().Single().AssetTable.Headers.Single(h => h.Id == id);

    private static byte[] Save(Archive archive)
    {
        using var stream = new MemoryStream();
        archive.Save(stream);
        return stream.ToArray();
    }

    private static Asset AssetOf(AssetView view, uint id) =>
        view.Layers.SelectMany(layer => layer.Assets).Single(asset => asset.Id.Value == id);

    private static object Summary(IReadOnlyList<Layer> layers, IReadOnlyList<AssetDiagnostic> diagnostics) => new
    {
        Layers = string.Join(";", layers.Select(layer =>
            $"{layer.Type}:{string.Join(",", layer.Assets.Select(a => $"{a.GetType().Name}/{a.Type}/{a.Id}/{a.Name}"))}")),
        Diagnostics = string.Join(";", diagnostics)
    };

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void ReadAssets_ParsesWhatASessionDoes(Platform platform)
    {
        byte[] bytes = Fixture(platform);
        var expected = Load(bytes, platform).OpenAssets();

        var view = Load(bytes, platform).ReadAssets();

        Assert.Equal(Summary(expected.Layers, expected.Diagnostics), Summary(view.Layers, view.Diagnostics));
    }

    [Fact]
    public void ReadAssets_AssetOutsideTheData_DegradesAndReportsAsASessionDoes()
    {
        Archive Broken()
        {
            var archive = Load(Fixture(Platform.Xbox), Platform.Xbox);
            archive.Roots.OfType<Dictionary>().Single().AssetTable.Headers.Single(h => h.Id == 2).Offset = 0xFFFFFF00;
            return archive;
        }
        var expected = Broken().OpenAssets();

        var view = Broken().ReadAssets();

        Assert.NotEmpty(view.Diagnostics);
        Assert.Equal(Summary(expected.Layers, expected.Diagnostics), Summary(view.Layers, view.Diagnostics));
        Assert.True(view.GetBytes(AssetOf(view, 2)).IsEmpty);
    }

    [Fact]
    public void ReadAssets_LeavesTheBlocksAttachedAndUnlocked()
    {
        byte[] bytes = Fixture(Platform.Xbox);
        var archive = Load(bytes, Platform.Xbox);
        var roots = archive.Roots.ToList();
        var dictionary = archive.Roots.OfType<Dictionary>().Single();
        var table = dictionary.AssetTable;
        var header = table.Headers.First();

        archive.ReadAssets();

        Assert.Equal(roots, archive.Roots);
        Assert.Same(table, dictionary.AssetTable);
        header.Id = header.Id;
        Assert.Equal(bytes, Save(archive));
    }

    [Fact]
    public void ReadAssets_SerializerStatingNoPlatform_ThrowsInvalidOperationException()
    {
        var archive = Archive.Load(new MemoryStream(Fixture(Platform.Xbox)), GameVersion.TSSM);

        Assert.Throws<InvalidOperationException>(() => archive.ReadAssets());
    }

    [Fact]
    public void ReadAssets_WithPlatform_OnSerializerStatingNone_ReadsWithThatPlatform()
    {
        var archive = Archive.Load(new MemoryStream(Fixture(Platform.Xbox)), GameVersion.TSSM);

        var view = archive.ReadAssets(Platform.Xbox);

        Assert.Equal(Platform.Xbox, view.Profile.Platform);
        Assert.Equal(3, ((CounterAsset)AssetOf(view, 1)).InitialValue);
    }

    [Fact]
    public void ReadAssets_WithPlatform_ConflictingWithTheStatedOne_ThrowsInvalidOperationException()
    {
        var archive = Load(Fixture(Platform.Xbox), Platform.Xbox);

        Assert.Throws<InvalidOperationException>(() => archive.ReadAssets(Platform.GameCube));
    }

    [Fact]
    public void ReadAssets_WhileASessionIsOpen_ThrowsInvalidOperationException()
    {
        var archive = Load(Fixture(Platform.Xbox), Platform.Xbox);

        using var session = archive.OpenAssets();

        Assert.Throws<InvalidOperationException>(() => archive.ReadAssets());
    }

    [Fact]
    public void ReadAssets_AfterTheOpenSessionCommits_Reads()
    {
        var archive = Load(Fixture(Platform.Xbox), Platform.Xbox);
        archive.OpenAssets().Commit();

        var view = archive.ReadAssets();

        Assert.Equal(3, ((CounterAsset)AssetOf(view, 1)).InitialValue);
    }

    [Fact]
    public void ReadAssets_ThenASessionChangesAnAsset_KeepsWhatWasRead()
    {
        var archive = Load(Fixture(Platform.Xbox), Platform.Xbox);
        var view = archive.ReadAssets();
        byte[] before = view.GetBytes(AssetOf(view, 1)).ToArray();

        using (var session = archive.OpenAssets())
            session.Layers[CountersLayer].Assets.OfType<CounterAsset>().First().InitialValue = 9;

        Assert.Equal(3, ((CounterAsset)AssetOf(view, 1)).InitialValue);
        Assert.Equal(before, view.GetBytes(AssetOf(view, 1)).ToArray());
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void GetBytes_IsEachAssetAsAuthored(Platform platform)
    {
        byte[] bytes = Fixture(platform);
        var view = Load(bytes, platform).ReadAssets();

        foreach (uint id in new uint[] { 1, 2 })
        {
            var header = Header(bytes, platform, id);
            Assert.Equal(bytes.AsSpan((int)header.Offset, (int)header.Size), view.GetBytes(AssetOf(view, id)).Span);
        }
    }

    [Fact]
    public void GetBytes_ZeroSizeAsset_IsEmpty()
    {
        var view = Load(Fixture(Platform.Xbox), Platform.Xbox).ReadAssets();

        Assert.True(view.GetBytes(AssetOf(view, 3)).IsEmpty);
    }

    [Fact]
    public void GetBytes_AfterTheAssetsIdChanges_StillAnswers()
    {
        var view = Load(Fixture(Platform.Xbox), Platform.Xbox).ReadAssets();
        var asset = AssetOf(view, 1);
        int expected = view.GetBytes(asset).Length;

        asset.Id = new AssetId(2);

        Assert.Equal(expected, view.GetBytes(asset).Length);
    }

    [Fact]
    public void GetBytes_AssetOfAnotherView_ThrowsArgumentException()
    {
        byte[] bytes = Fixture(Platform.Xbox);
        var view = Load(bytes, Platform.Xbox).ReadAssets();
        var other = Load(bytes, Platform.Xbox).ReadAssets();

        Assert.Throws<ArgumentException>(() => view.GetBytes(AssetOf(other, 1)));
    }

    [Fact]
    public void GetBytes_AssetNeverRead_ThrowsArgumentException()
    {
        var view = Load(Fixture(Platform.Xbox), Platform.Xbox).ReadAssets();

        Assert.Throws<ArgumentException>(() => view.GetBytes(new CounterAsset()));
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void GetLayerImage_SpansTheFirstAssetsStartToTheLastOnesEndWithTheGapBetween(Platform platform)
    {
        byte[] bytes = Fixture(platform);
        var (first, second) = (Header(bytes, platform, 1), Header(bytes, platform, 2));
        var view = Load(bytes, platform).ReadAssets();

        var image = view.GetLayerImage(CountersLayer);

        Assert.Equal(bytes.AsSpan((int)first.Offset, (int)(second.Offset + second.Size - first.Offset)), image.Span);
        Assert.Equal(GapMarker, image.Span[(int)first.Size]);
    }

    [Theory]
    [InlineData(EmptyAssetLayer)]
    [InlineData(EmptyLayer)]
    public void GetLayerImage_LayerWithoutBytes_IsEmpty(int layer)
    {
        var view = Load(Fixture(Platform.Xbox), Platform.Xbox).ReadAssets();

        Assert.True(view.GetLayerImage(layer).IsEmpty);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void GetLayerImage_IndexOutsideLayers_ThrowsArgumentOutOfRangeException(int layer)
    {
        var view = Load(Fixture(Platform.Xbox), Platform.Xbox).ReadAssets();

        Assert.Throws<ArgumentOutOfRangeException>(() => view.GetLayerImage(layer));
    }

    /// <summary>
    /// A Counter's initial value follows its 8-byte base prefix, in the platform's byte order: read
    /// in the wrong order, 3 is 768.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void OpenReader_ReadsInThePlatformsByteOrder(Platform platform)
    {
        var view = Load(Fixture(platform), platform).ReadAssets();
        using var reader = view.OpenReader(AssetOf(view, 1));

        reader.BaseStream.Position = 8;

        Assert.Equal(3, reader.ReadInt16());
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void OpenLayerReader_ReadsTheImageInThePlatformsByteOrder(Platform platform)
    {
        byte[] bytes = Fixture(platform);
        var (first, second) = (Header(bytes, platform, 1), Header(bytes, platform, 2));
        var view = Load(bytes, platform).ReadAssets();
        using var reader = view.OpenLayerReader(CountersLayer);

        reader.BaseStream.Position = second.Offset - first.Offset + 8;

        Assert.Equal(5, reader.ReadInt16());
    }
}

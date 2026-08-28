using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Assets;

public class AssetSessionTests
{
    public static TheoryData<string> Games =>
        ["n100f", "bfbb", "tssm", "incredibles", "rotu", "ratatouille"];

    private static Serializer SerializerFor(string game) => game switch
    {
        "n100f" => new N100FSerializer(),
        "bfbb" => new BFBBSerializer(),
        "tssm" => new TSSMSerializer(),
        "incredibles" => new IncrediblesSerializer(),
        "rotu" => new ROTUSerializer(),
        _ => new RatatouilleSerializer()
    };

    /// <summary>
    /// The committed archives use <c>AHDR.Offset</c> as a byte index into the file, which the
    /// block-layer fixtures leave at 0 - they were built to exercise blocks, where the field is
    /// opaque. Loading one and pointing its offsets at the real data start makes it a valid archive
    /// at the asset layer without hand-assembling a second set of fixtures.
    /// </summary>
    private static Archive LoadRepaired(string game) => LoadRepaired(game, SerializerFor(game));

    /// <summary>
    /// <paramref name="mutate"/> runs before offsets are repaired, so a change that resizes a block
    /// (e.g. a <see cref="LayerHeader"/>'s <see cref="LayerHeader.AssetIds"/>) is still reflected in
    /// the recomputed data start.
    /// </summary>
    private static Archive LoadRepaired(string game, Serializer serializer, Action<EvilHop.Blocks.Dictionary>? mutate = null)
    {
        byte[] bytes = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "TestData", game, "minimal.hip"));

        var archive = Archive.Load(new MemoryStream(bytes), serializer);
        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;
        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();
        mutate?.Invoke(dictionary);

        uint dataStart = (uint)(Save(archive).Length - streamData.Data.Length);
        foreach (var header in dictionary.AssetTable.Headers)
            header.Offset = dataStart;

        return archive;
    }

    /// <summary>
    /// An archive that has already been through a commit, so its layout is what this library
    /// produces rather than what the fixture happened to contain.
    /// </summary>
    private static byte[] Canonical(string game)
    {
        var archive = LoadRepaired(game);
        using (archive.OpenAssets()) { }
        return Save(archive);
    }

    private static byte[] Save(Archive archive)
    {
        using var stream = new MemoryStream();
        archive.Save(stream);
        return stream.ToArray();
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void OpenAssets_ThenCommit_NoEdits_ProducesIdenticalBytes(string game)
    {
        byte[] canonical = Canonical(game);
        var archive = Archive.Load(new MemoryStream(canonical), SerializerFor(game));

        using (archive.OpenAssets()) { }

        Assert.Equal(canonical, Save(archive));
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void OpenAssets_ReportsNoDiagnostics(string game)
    {
        var archive = LoadRepaired(game);

        using var session = archive.OpenAssets();

        Assert.Empty(session.Diagnostics);
    }

    [Fact]
    public void OpenAssets_DuplicateLayerListing_ReportsDiagnosticForThatAsset()
    {
        uint assetId = 0;
        var archive = LoadRepaired("n100f", SerializerFor("n100f"), dictionary =>
        {
            assetId = dictionary.AssetTable.Headers.Single().Id;
            var layerHeader = dictionary.LayerTable.Headers.Single();
            layerHeader.AssetIds = [.. layerHeader.AssetIds, assetId];
        });

        using var session = archive.OpenAssets();

        var diagnostic = Assert.Single(session.Diagnostics);
        Assert.Equal(new AssetId(assetId), diagnostic.AssetId);
    }

    [Fact]
    public void OpenAssets_ProducesOneLayerWithOneAsset()
    {
        var archive = LoadRepaired("n100f");

        using var session = archive.OpenAssets();

        var layer = Assert.Single(session.Layers);
        Assert.Single(layer.Assets);
    }

    [Fact]
    public void OpenAssets_AssetCarriesHeaderSourcedFields()
    {
        var archive = LoadRepaired("n100f");
        var header = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.Single();
        uint expectedId = header.Id;
        string expectedName = header.Debug.Name;

        using var session = archive.OpenAssets();

        var asset = session.Layers[0].Assets[0];
        Assert.Equal(expectedId, asset.Id.Value);
        Assert.Equal(expectedName, asset.Name);
    }

    [Fact]
    public void OpenAssets_DetachesAssetAndLayerTables()
    {
        var archive = LoadRepaired("n100f");
        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();

        using var session = archive.OpenAssets();

        Assert.Empty(dictionary.Children);
    }

    [Fact]
    public void OpenAssets_EmptiesStreamData()
    {
        var archive = LoadRepaired("n100f");
        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;

        using var session = archive.OpenAssets();

        Assert.Empty(streamData.Data);
    }

    [Fact]
    public void OpenAssets_LocksCapturedAssetHeaderReference()
    {
        var archive = LoadRepaired("n100f");
        var header = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.Single();

        using var session = archive.OpenAssets();

        Assert.Throws<InvalidOperationException>(() => { header.Id = 1234; });
    }

    [Fact]
    public void OpenAssets_LocksCapturedAssetDebugReference()
    {
        var archive = LoadRepaired("n100f");
        var debug = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.Single().Debug;

        using var session = archive.OpenAssets();

        Assert.Throws<InvalidOperationException>(() => { debug.Name = "renamed"; });
    }

    [Fact]
    public void OpenAssets_LocksCapturedStreamDataReference()
    {
        var archive = LoadRepaired("n100f");
        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;

        using var session = archive.OpenAssets();

        Assert.Throws<InvalidOperationException>(() => { streamData.Data = [0x01]; });
    }

    [Fact]
    public void OpenAssets_LocksDictionaryAgainstReplacingAssetTable()
    {
        var archive = LoadRepaired("n100f");
        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();
        var replacement = archive.Serializer.CreateBlock<AssetTable>();

        using var session = archive.OpenAssets();

        Assert.Throws<InvalidOperationException>(() => { dictionary.AssetTable = replacement; });
    }

    [Fact]
    public void OpenAssets_LocksAssetStreamAgainstReplacingData()
    {
        var archive = LoadRepaired("n100f");
        var stream = archive.Roots.OfType<AssetStream>().Single();
        var replacement = archive.Serializer.CreateBlock<StreamData>();

        using var session = archive.OpenAssets();

        Assert.Throws<InvalidOperationException>(() => { stream.Data = replacement; });
    }

    [Fact]
    public void OpenAssets_LeavesPackageFieldsSettable()
    {
        var archive = LoadRepaired("n100f");
        var package = archive.Roots.OfType<Package>().Single();

        using var session = archive.OpenAssets();
        package.Counts.AssetCount = 99;

        Assert.Equal(99u, package.Counts.AssetCount);
    }

    [Fact]
    public void Commit_ReattachesAssetAndLayerTables()
    {
        var archive = LoadRepaired("n100f");
        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();
        var session = archive.OpenAssets();

        session.Commit();

        Assert.Equal(["ATOC", "LTOC"], dictionary.Children.Select(child => child.Tag));
    }

    [Fact]
    public void Commit_LeavesDictionaryAndAssetStreamSettableAgain()
    {
        var archive = LoadRepaired("n100f");
        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();
        var stream = archive.Roots.OfType<AssetStream>().Single();
        var session = archive.OpenAssets();

        session.Commit();
        dictionary.LayerTable = archive.Serializer.CreateBlock<LayerTable>();
        stream.Data = archive.Serializer.CreateBlock<StreamData>();

        Assert.NotNull(dictionary.LayerTable);
        Assert.NotNull(stream.Data);
    }

    [Fact]
    public void Commit_AssignsAbsoluteOffsetPointingAtTheAssetsBytes()
    {
        byte[] canonical = Canonical("n100f");
        var archive = Archive.Load(new MemoryStream(canonical), new N100FSerializer());
        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;
        var header = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.Single();

        Assert.Equal(canonical.Length - streamData.Data.Length, (int)header.Offset);
    }

    [Fact]
    public void Commit_PadsStreamDataToA32ByteBoundary()
    {
        byte[] canonical = Canonical("n100f");
        var archive = Archive.Load(new MemoryStream(canonical), new N100FSerializer());
        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;

        Assert.Equal(0, (canonical.Length - streamData.Data.Length) % 32);
    }

    [Fact]
    public void Commit_PadsStreamDataToA2048ByteSectorOnPlayStation2()
    {
        var profile = N100FSerializer.DefaultProfile with { Platform = Platform.PlayStation2 };
        var archive = LoadRepaired("n100f", new N100FSerializer(profile));
        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;

        using (archive.OpenAssets()) { }
        byte[] saved = Save(archive);

        Assert.Equal(0, (saved.Length - streamData.Data.Length) % 2048);
    }

    [Fact]
    public void Commit_NoEdits_ReportsNothingChanged()
    {
        var archive = LoadRepaired("n100f");
        var session = archive.OpenAssets();

        session.Commit();

        Assert.Empty(session.ChangedAssets);
    }

    [Fact]
    public void Commit_EditedAsset_ReportsItChanged()
    {
        var archive = LoadRepaired("n100f");
        var session = archive.OpenAssets();
        var asset = (PayloadAsset)session.Layers[0].Assets[0];
        asset.Data = [0x01, 0x02, 0x03, 0x04];

        session.Commit();

        Assert.Equal([asset.Id], session.ChangedAssets);
    }

    [Fact]
    public void Commit_EditedAsset_SurvivesAReopen()
    {
        var archive = Archive.Load(new MemoryStream(Canonical("n100f")), new N100FSerializer());
        using (var session = archive.OpenAssets())
            ((PayloadAsset)session.Layers[0].Assets[0]).Data = [0xAA, 0xBB, 0xCC, 0xDD];

        var reopened = Archive.Load(new MemoryStream(Save(archive)), new N100FSerializer());
        using var verify = reopened.OpenAssets();

        Assert.Equal<byte>([0xAA, 0xBB, 0xCC, 0xDD], ((PayloadAsset)verify.Layers[0].Assets[0]).Data);
    }

    [Fact]
    public void Commit_ResizedAsset_UpdatesItsRecordedSize()
    {
        var archive = Archive.Load(new MemoryStream(Canonical("n100f")), new N100FSerializer());
        using (var session = archive.OpenAssets())
            ((PayloadAsset)session.Layers[0].Assets[0]).Data = new byte[64];

        var header = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.Single();

        Assert.Equal(64u, header.Size);
    }

    [Fact]
    public void Commit_CalledTwice_IsANoOp()
    {
        var archive = LoadRepaired("n100f");
        var session = archive.OpenAssets();

        session.Commit();
        byte[] first = Save(archive);
        session.Commit();

        Assert.Equal(first, Save(archive));
    }

    [Fact]
    public void Dispose_WithoutExplicitCommit_StillCommits()
    {
        var archive = LoadRepaired("n100f");
        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();

        using (archive.OpenAssets()) { }

        Assert.Equal(["ATOC", "LTOC"], dictionary.Children.Select(child => child.Tag));
    }
}

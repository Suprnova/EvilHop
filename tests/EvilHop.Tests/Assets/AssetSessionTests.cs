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

    /// <summary>
    /// The last <c>Layer</c>'s trailing padding is the archive's own: without it the file stops
    /// wherever the last asset happened to end. Every <c>DefaultProfile</c> is GameCube, so the
    /// boundary here is 32 bytes.
    /// </summary>
    [Theory]
    [MemberData(nameof(Games))]
    public void Commit_PadsTheArchivesEndToThePlatformsDataAlignment(string game)
    {
        var archive = LoadRepaired(game);

        using (archive.OpenAssets()) { }

        Assert.Equal(0, Save(archive).Length % 32);
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

    /// <summary>
    /// Real archives exist whose stored checksum disagrees with their own asset data. A session that
    /// changed nothing must write that disagreement back untouched rather than correcting it.
    /// </summary>
    [Fact]
    public void Commit_UnchangedAssetWithAWrongStoredChecksum_WritesTheStoredOneBack()
    {
        const uint wrong = 0xDEADBEEF;
        var archive = LoadRepaired("n100f", SerializerFor("n100f"), dictionary =>
            dictionary.AssetTable.Headers.Single().Debug.Checksum = wrong);

        using (archive.OpenAssets()) { }

        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();
        Assert.Equal(wrong, dictionary.AssetTable.Headers.Single().Debug.Checksum);
    }

    /// <summary>
    /// The physical surface's promise: a value written there survives commit byte-exactly, even one
    /// that disagrees with what the model would derive.
    /// </summary>
    [Fact]
    public void Commit_ChecksumWrittenToThePhysicalSurface_SerializesAsWritten()
    {
        const uint deliberate = 0x0BADC0DE;
        var archive = LoadRepaired("n100f");

        using (var session = archive.OpenAssets())
            session.Layers.Single().Assets.Single().Physical.Checksum = deliberate;

        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();
        Assert.Equal(deliberate, dictionary.AssetTable.Headers.Single().Debug.Checksum);
    }

    /// <summary>
    /// With nothing overriding it, the checksum is derived, so editing the asset moves it. The
    /// fixture ships an arbitrary checksum that disagrees with its own data, so this also covers
    /// assigning the derived value to clear an override.
    /// </summary>
    [Fact]
    public void Commit_ChangedAssetWithNoOverride_TracksItsNewData()
    {
        byte[] replacement = [1, 2, 3, 4];
        var archive = LoadRepaired("n100f");

        using (var session = archive.OpenAssets())
        {
            var asset = (PayloadAsset)session.Layers.Single().Assets.Single();
            asset.Physical.Checksum = asset.ComputedChecksum;
            asset.LoadFrom(new MemoryStream(replacement));
        }

        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();
        Assert.Equal(Crc32Mpeg2.Compute(replacement), dictionary.AssetTable.Headers.Single().Debug.Checksum);
    }

    /// <summary>
    /// An override is a deliberate disagreement, not a stale value - editing the asset it sits on
    /// doesn't quietly discard it.
    /// </summary>
    [Fact]
    public void Commit_ChangedAssetCarryingAnOverride_KeepsWritingTheOverride()
    {
        const uint wrong = 0xDEADBEEF;
        var archive = LoadRepaired("n100f", SerializerFor("n100f"), dictionary =>
            dictionary.AssetTable.Headers.Single().Debug.Checksum = wrong);

        using (var session = archive.OpenAssets())
            ((PayloadAsset)session.Layers.Single().Assets.Single()).LoadFrom(new MemoryStream([1, 2, 3, 4]));

        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();
        Assert.Equal(wrong, dictionary.AssetTable.Headers.Single().Debug.Checksum);
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

    /// <summary>
    /// Builds an empty (no assets, no layers) archive on <see cref="Platform.PlayStation2"/>, whose
    /// 2048-byte data alignment makes an empty <c>DPAK</c>'s fill run far bigger than the 4-byte
    /// <see cref="StreamData.PaddingAmount"/> field would be - the case that exposed a prior bug
    /// which wrote the field whenever the fill alone looked large enough to hold it.
    /// </summary>
    private static Archive EmptyArchive(bool hadPaddingAmountField)
    {
        var profile = N100FSerializer.DefaultProfile with { Platform = Platform.PlayStation2 };
        var archive = LoadRepaired("n100f", new N100FSerializer(profile), dictionary =>
        {
            dictionary.AssetTable.Headers = [];
            dictionary.LayerTable.Headers = [];
        });

        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;
        streamData.PaddingAmount = hadPaddingAmountField ? 0u : null;
        streamData.Padding = [];

        return archive;
    }

    /// <summary>
    /// The convention most real archives use: no assets means no <see cref="StreamData.PaddingAmount"/>
    /// field either, regardless of how much fill <c>DPAK</c> still needs.
    /// </summary>
    [Fact]
    public void Commit_NoAssets_OmitsPaddingAmountWhenTheArchiveHadNone()
    {
        var archive = EmptyArchive(hadPaddingAmountField: false);
        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;

        using (archive.OpenAssets()) { }

        Assert.Null(streamData.PaddingAmount);
        Assert.Equal(0, Save(archive).Length % 2048);
    }

    /// <summary>
    /// A minority of real archives keep <see cref="StreamData.PaddingAmount"/> even with no assets
    /// to align - a session opened against one preserves that instead of assuming the field is
    /// always dropped.
    /// </summary>
    [Fact]
    public void Commit_NoAssets_KeepsPaddingAmountWhenTheArchiveHadOne()
    {
        var archive = EmptyArchive(hadPaddingAmountField: true);
        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;

        using (archive.OpenAssets()) { }

        Assert.NotNull(streamData.PaddingAmount);
        Assert.Equal(0, Save(archive).Length % 2048);
    }

    [Fact]
    public void Commit_UpdatesPackageCounts_ForSingleAssetArchive()
    {
        var archive = LoadRepaired("n100f");
        var header = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.Single();
        uint expectedSize = header.Size;
        var counts = archive.Roots.OfType<Package>().Single().Counts;

        using (archive.OpenAssets()) { }

        Assert.Equal(1u, counts.AssetCount);
        Assert.Equal(1u, counts.LayerCount);
        Assert.Equal(expectedSize, counts.MaxAssetSize);
        Assert.Equal(expectedSize, counts.MaxLayerSize);
        Assert.Equal(0u, counts.MaxXFormAssetSize);
    }

    [Fact]
    public void Commit_MaxXFormAssetSize_ReflectsReadTransformFlag()
    {
        var archive = LoadRepaired("n100f");
        var counts = archive.Roots.OfType<Package>().Single().Counts;
        var session = archive.OpenAssets();
        var asset = session.Layers[0].Assets[0];
        asset.Physical.Flags |= AssetFlags.ReadTransform;

        session.Commit();

        Assert.Equal(counts.MaxAssetSize, counts.MaxXFormAssetSize);
    }

    /// <summary>
    /// Adds a second <c>Layer</c> holding a copy of the fixture's only <c>Asset</c>, so committing
    /// exercises padding between two <c>Layer</c>s rather than just between two <c>Asset</c>s.
    /// </summary>
    private static Archive TwoLayerArchive() => LoadRepaired("n100f", SerializerFor("n100f"), dictionary =>
    {
        var originalHeader = dictionary.AssetTable.Headers.Single();
        var originalLayer = dictionary.LayerTable.Headers.Single();

        var secondHeader = new AssetHeader
        {
            Id = originalHeader.Id + 1,
            Type = originalHeader.Type,
            Size = originalHeader.Size,
            Flags = originalHeader.Flags,
            Debug = new AssetDebug { Name = "second" }
        };
        dictionary.AssetTable.Headers = [.. dictionary.AssetTable.Headers, secondHeader];

        var secondLayer = new LayerHeader
        {
            Type = originalLayer.Type,
            AssetCount = 1,
            AssetIds = [secondHeader.Id],
            Debug = new LayerDebug()
        };
        dictionary.LayerTable.Headers = [.. dictionary.LayerTable.Headers, secondLayer];
    });

    [Fact]
    public void Commit_LastAssetInLayer_PadsTheLayerToPlatformDataAlignment()
    {
        var archive = TwoLayerArchive();

        using (archive.OpenAssets()) { }

        var headers = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.ToList();
        var firstLayerAsset = headers[0];
        var secondLayerAsset = headers[1];
        uint expectedNextOffset = firstLayerAsset.Offset + firstLayerAsset.Size;
        expectedNextOffset += (32 - expectedNextOffset % 32) % 32;

        Assert.Equal(0u, firstLayerAsset.Plus);
        Assert.Equal(expectedNextOffset, secondLayerAsset.Offset);
    }

    /// <summary>
    /// Adds a second <c>Asset</c> of <paramref name="type"/>, with no declared alignment, to the
    /// Incredibles fixture's only <c>Layer</c> on <paramref name="platform"/>, so committing exercises
    /// the gap in front of it rather than the platform data alignment <see cref="TwoLayerArchive"/>
    /// exercises. Incredibles carries every type <see cref="AssetSession.DefaultAlignmentFor"/>
    /// special-cases, so one fixture covers all of them.
    /// </summary>
    private static Archive IncrediblesArchiveWithSecondAsset(AssetType type, Platform platform) => LoadRepaired(
        "incredibles", new IncrediblesSerializer(IncrediblesSerializer.DefaultProfile with { Platform = platform }), dictionary =>
    {
        var originalHeader = dictionary.AssetTable.Headers.Single();
        var originalLayer = dictionary.LayerTable.Headers.Single();

        var secondHeader = new AssetHeader
        {
            Id = originalHeader.Id + 1,
            Type = type,
            Size = originalHeader.Size,
            Flags = originalHeader.Flags,
            Debug = new AssetDebug { Name = "second", Alignment = -1 }
        };
        dictionary.AssetTable.Headers = [.. dictionary.AssetTable.Headers, secondHeader];

        originalLayer.AssetCount = 2;
        originalLayer.AssetIds = [.. originalLayer.AssetIds, secondHeader.Id];
    });

    private static void AssertSecondAssetAligned(Archive archive, uint alignment)
    {
        using (archive.OpenAssets()) { }

        var headers = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.ToList();
        uint expectedOffset = headers[0].Offset + headers[0].Size;
        expectedOffset += (alignment - expectedOffset % alignment) % alignment;

        Assert.Equal(expectedOffset, headers[1].Offset);
    }

    /// <summary>
    /// Adds a second, zero-size <c>Asset</c> reading <c>Offset</c> <paramref name="originalOffset"/> to
    /// the Incredibles fixture's only <c>Layer</c>, leaving it there rather than repairing it to the
    /// data start the way <see cref="LoadRepaired(string, Serializer, Action{EvilHop.Blocks.Dictionary}?)"/>
    /// otherwise would - so it stands in for whatever arbitrary value a real zero-size asset's
    /// <c>Offset</c> happens to carry.
    /// </summary>
    private static Archive ArchiveWithZeroSizeSecondAsset(uint originalOffset)
    {
        byte[] bytes = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "TestData", "incredibles", "minimal.hip"));
        var serializer = new IncrediblesSerializer();

        var archive = Archive.Load(new MemoryStream(bytes), serializer);
        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;
        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();

        var originalHeader = dictionary.AssetTable.Headers.Single();
        var originalLayer = dictionary.LayerTable.Headers.Single();

        var secondHeader = new AssetHeader
        {
            Id = originalHeader.Id + 1,
            Type = originalHeader.Type,
            Size = 0,
            Offset = originalOffset,
            Flags = originalHeader.Flags,
            Debug = new AssetDebug { Name = "second", Alignment = -1 }
        };
        dictionary.AssetTable.Headers = [.. dictionary.AssetTable.Headers, secondHeader];

        originalLayer.AssetCount = 2;
        originalLayer.AssetIds = [.. originalLayer.AssetIds, secondHeader.Id];

        originalHeader.Offset = (uint)(Save(archive).Length - streamData.Data.Length);

        return archive;
    }

    /// <summary>
    /// A zero-size asset points at nothing, so real archives record all sorts of values there - not
    /// just 0 (BFBB) but a real, shared, non-cursor address (TSSM) or a value that depends on platform
    /// for otherwise-identical assets (ROTU) - see <see cref="AssetSession.BuildData"/>'s remarks. An
    /// unedited session replays whichever one it read rather than assigning a fresh position.
    /// </summary>
    [Theory]
    [InlineData(0u)]
    [InlineData(0x76A800u)]
    public void Commit_UnchangedZeroSizeAsset_KeepsItsOriginalOffset(uint originalOffset)
    {
        var archive = ArchiveWithZeroSizeSecondAsset(originalOffset);

        using (archive.OpenAssets()) { }

        var headers = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.ToList();
        Assert.Equal(originalOffset, headers[1].Offset);
    }

    /// <summary>
    /// The counterpart to <see cref="Commit_UnchangedZeroSizeAsset_KeepsItsOriginalOffset"/>: a
    /// zero-size asset with no original to replay - because it was added during the session - gets a
    /// real position like any other asset would.
    /// </summary>
    [Fact]
    public void Commit_NewZeroSizeAsset_GetsARealPosition()
    {
        var archive = LoadRepaired("incredibles", new IncrediblesSerializer());
        uint originalId = 0;

        using (var session = archive.OpenAssets())
        {
            var original = session.Layers.Single().Assets.Single();
            originalId = original.Id.Value;

            var added = new GenericPayloadAsset { Id = new AssetId(originalId + 1), Name = "second" };
            added.Physical.Type = original.Type;
            added.Physical.Flags = original.Physical.Flags;
            added.Physical.Alignment = -1;
            session.Layers.Single().Add(added);
        }

        var headers = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.ToList();
        var firstHeader = headers.Single(h => h.Id == originalId);
        var secondHeader = headers.Single(h => h.Id == originalId + 1);
        uint expectedOffset = firstHeader.Offset + firstHeader.Size;
        expectedOffset += (16 - expectedOffset % 16) % 16;

        Assert.Equal(expectedOffset, secondHeader.Offset);
    }

    /// <summary>
    /// Confirmed against every non-positive-alignment gap ahead of one of these three, on every
    /// platform, in the Incredibles corpus: 155 <c>ReactiveAnimation</c>, 52 <c>PickupTypes</c>, and
    /// 208 <c>ThrowableTable</c> gaps, none fitting the flat 16-byte default every other type gets, all
    /// fitting 2048.
    /// </summary>
    [Theory]
    [InlineData(AssetType.ReactiveAnimation)]
    [InlineData(AssetType.PickupTypes)]
    [InlineData(AssetType.ThrowableTable)]
    public void Commit_WideAlignmentType_Aligns2048BytesFromThePreviousAsset(AssetType type) =>
        AssertSecondAssetAligned(IncrediblesArchiveWithSecondAsset(type, Platform.GameCube), 2048);

    /// <summary>
    /// Confirmed against every same-platform gap ahead of one of these four in the Incredibles corpus:
    /// 480 <c>Wireframe</c>, 414 <c>BinkVideo</c>, 1 <c>CutsceneTable</c>, and 1348
    /// <c>StreamingTexture</c> gaps on GameCube all fit 32.
    /// </summary>
    [Theory]
    [InlineData(AssetType.BinkVideo)]
    [InlineData(AssetType.CutsceneTable)]
    [InlineData(AssetType.StreamingTexture)]
    [InlineData(AssetType.Wireframe)]
    public void Commit_StreamingAlignmentType_Aligns32BytesOnGameCube(AssetType type) =>
        AssertSecondAssetAligned(IncrediblesArchiveWithSecondAsset(type, Platform.GameCube), 32);

    /// <summary>
    /// The counterpart to <see cref="Commit_StreamingAlignmentType_Aligns32BytesOnGameCube"/>: off
    /// GameCube, the matching 660/301/290/2135 gaps on PS2 (269/324/240/1272 on Xbox) all reject every
    /// value up to 1024 and need 2048 instead - plausibly for optical-disc sector-aligned streaming
    /// reads, which GameCube discs don't need.
    /// </summary>
    [Theory]
    [InlineData(AssetType.BinkVideo)]
    [InlineData(AssetType.CutsceneTable)]
    [InlineData(AssetType.StreamingTexture)]
    [InlineData(AssetType.Wireframe)]
    public void Commit_StreamingAlignmentType_Aligns2048BytesOffGameCube(AssetType type) =>
        AssertSecondAssetAligned(IncrediblesArchiveWithSecondAsset(type, Platform.Xbox), 2048);

    [Fact]
    public void Commit_UpdatesPackageCounts_ForTwoLayerArchive()
    {
        var archive = TwoLayerArchive();
        var counts = archive.Roots.OfType<Package>().Single().Counts;

        using (archive.OpenAssets()) { }

        var firstLayerAsset = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable.Headers.First();

        Assert.Equal(2u, counts.AssetCount);
        Assert.Equal(2u, counts.LayerCount);
        Assert.Equal(firstLayerAsset.Size, counts.MaxLayerSize);
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

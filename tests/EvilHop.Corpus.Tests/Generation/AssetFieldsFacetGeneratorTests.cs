using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Corpus.Generation;
using EvilHop.Serialization;
using EvilHop.Validation;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Tests.Generation;

public class AssetFieldsFacetGeneratorTests
{
    private readonly AssetFieldsFacetGenerator generator = new();

    /// <summary>
    /// Loads the single-asset BFBB fixture and points its <c>AHDR.Offset</c> at where the data
    /// actually starts - the block-layer fixtures leave it at 0, which is opaque to blocks but not
    /// to a session - optionally retyping and resizing that asset so a chosen shape's codec reads it.
    /// </summary>
    private static Archive Fixture(AssetType? type = null, int dataLength = 0)
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "bfbb", "minimal.hip"));
        var archive = Archive.Load(new MemoryStream(bytes), new BFBBSerializer());

        var dictionary = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single();
        var streamData = archive.Roots.OfType<AssetStream>().Single().Data;
        var header = dictionary.AssetTable.Headers.Single();

        if (type is { } retyped) header.Type = retyped;
        if (dataLength > 0)
        {
            streamData.Data = new byte[dataLength];
            header.Size = (uint)dataLength;
        }

        var measured = new MemoryStream();
        archive.Save(measured);
        header.Offset = (uint)(measured.ToArray().Length - streamData.Data.Length);

        return archive;
    }

    /// <summary>An entity-shaped asset needs enough bytes for its fixed prefix before the tail.</summary>
    private const int EntityPrefixBytes = 128;

    [Fact]
    public void Dependencies_IncludeEveryAssetScopedObservable()
    {
        Assert.Contains("asset.physical.alignment@assetType", generator.Dependencies);
        Assert.Contains("baseAsset.physical.baseType", generator.Dependencies);
        Assert.Contains("entityAsset.physical.subtype@assetType", generator.Dependencies);
    }

    [Fact]
    public void Dependencies_IncludeTheCodecRegistry() =>
        Assert.Contains(ValidationCatalogue.AssetCodecsKey, generator.Dependencies);

    [Fact]
    public void Dependencies_ExcludeTheAssetTypeEnum() =>
        Assert.DoesNotContain(generator.Dependencies, dependency => dependency.Contains(nameof(AssetType)));

    [Fact]
    public void Stage_IsAssets_SoTheBlockFacetsMapBeforeASessionRewritesTheirBlocks() =>
        Assert.Equal(MapStage.Assets, generator.Stage);

    [Fact]
    public void Map_Asset_GroupsItsFieldsUnderTheArchiveReportedType()
    {
        var record = generator.Map(Fixture(AssetType.Trigger, EntityPrefixBytes));

        var groups = Assert.IsType<JsonObject>(record["asset.physical.alignment@assetType"]);
        Assert.Equal([((uint)AssetType.Trigger).ToString()], groups.Select(group => group.Key));
    }

    [Fact]
    public void Map_EntityShapedAsset_RecordsBothGranularitiesOfItsBaseType()
    {
        var record = generator.Map(Fixture(AssetType.Trigger, EntityPrefixBytes));

        Assert.IsType<JsonArray>(record["baseAsset.physical.baseType"]);
        Assert.IsType<JsonObject>(record["baseAsset.physical.baseType@assetType"]);
    }

    [Fact]
    public void Map_AssetWithNoBaseAssetShape_RecordsItsHeaderFieldsButNoBaseType()
    {
        // RWTX is payload-shaped: its header-sourced fields are as real as any asset's, and its base
        // type genuinely does not exist rather than being missing.
        var record = generator.Map(Fixture(AssetType.Texture));

        Assert.IsType<JsonObject>(record["asset.physical.alignment@assetType"]);
        Assert.Null(record["baseAsset.physical.baseType"]);
    }

    [Fact]
    public void Map_AlignmentStoredAsNegative_RecordsItAsStored()
    {
        var record = generator.Map(Fixture(AssetType.Texture));

        var groups = Assert.IsType<JsonObject>(record["asset.physical.alignment@assetType"]);
        var values = Assert.IsType<JsonArray>(groups[((uint)AssetType.Texture).ToString()]);
        Assert.Equal(-1L, Assert.Single(values)!.GetValue<long>());
    }

    [Fact]
    public void Map_LeavesTheBlockTreeReadableForABlockScopedFacet()
    {
        var archive = Fixture(AssetType.Texture);

        generator.Map(archive);

        // The session commits on dispose, so the blocks are reattached rather than left detached -
        // but they are rebuilt, which is why the pipeline maps the block stage first.
        Assert.NotNull(archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable);
    }

    [Fact]
    public void Reduce_ObservationKeys_AreSortedOrdinally()
    {
        var record = new MappedArchive("a.hip", generator.Map(Fixture(AssetType.Trigger, EntityPrefixBytes)));

        var keys = generator.Reduce([record]).Select(property => property.Key).ToList();

        Assert.Equal(keys.OrderBy(key => key, StringComparer.Ordinal), keys);
    }

    [Fact]
    public void Reduce_GroupedObservable_RendersItsKeyAsAFourcc()
    {
        var record = new MappedArchive("a.hip", generator.Map(Fixture(AssetType.Trigger, EntityPrefixBytes)));

        var observations = generator.Reduce([record]);
        var grouped = Assert.IsType<JsonObject>(observations["asset.physical.alignment@assetType"]);

        var group = Assert.Single(Assert.IsType<JsonArray>(grouped["groups"]))!.AsObject();
        Assert.Equal("TRIG", group["keyDisplay"]!.GetValue<string>());
    }
}

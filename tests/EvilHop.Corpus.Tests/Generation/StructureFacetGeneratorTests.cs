using EvilHop.Blocks;
using EvilHop.Corpus.Generation;
using EvilHop.Serialization;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Tests.Generation;

public class StructureFacetGeneratorTests
{
    private readonly StructureFacetGenerator generator = new();

    private static Archive ArchiveOf(params Block[] roots) => new(new N100FSerializer(), roots);

    [Fact]
    public void Map_ArchiveWithRoots_RecordsRootSequenceInOnDiskOrder()
    {
        var record = generator.Map(ArchiveOf(new HIPA(), new Package { Version = new PackageVersion() }));

        var values = Assert.IsType<JsonArray>(record[StructureFacetGenerator.RootSequenceId]);
        Assert.Equal("HIPA,PACK", Assert.Single(values)!.GetValue<string>());
    }

    [Fact]
    public void Map_ArchiveWithPackage_RecordsChildTagsSortedAlphabetically()
    {
        var package = new Package { Flags = new PackageFlags(), Version = new PackageVersion() };

        var record = generator.Map(ArchiveOf(package));

        var values = Assert.IsType<JsonArray>(record[StructureFacetGenerator.PackChildrenId]);
        Assert.Equal("PFLG,PVER", Assert.Single(values)!.GetValue<string>());
    }

    [Fact]
    public void Map_ArchiveWithNoPackage_OmitsPackChildren()
    {
        var record = generator.Map(ArchiveOf(new HIPA()));

        Assert.Null(record[StructureFacetGenerator.PackChildrenId]);
    }

    [Fact]
    public void Map_RequiredChildPresent_RecordsOne()
    {
        var record = generator.Map(ArchiveOf(new Package { Version = new PackageVersion() }));

        var values = Assert.IsType<JsonArray>(record["PACK.version"]);
        Assert.Equal(1L, Assert.Single(values)!.GetValue<long>());
    }

    [Fact]
    public void Map_RequiredChildAbsent_RecordsZero()
    {
        var record = generator.Map(ArchiveOf(new Package()));

        var values = Assert.IsType<JsonArray>(record["PACK.version"]);
        Assert.Equal(0L, Assert.Single(values)!.GetValue<long>());
    }

    [Fact]
    public void Map_NoChildrenBlock_RecordsItsOwnChildCount()
    {
        var record = generator.Map(ArchiveOf(new Package { Version = new PackageVersion() }));

        var values = Assert.IsType<JsonArray>(record["PVER.childCount"]);
        Assert.Equal(0L, Assert.Single(values)!.GetValue<long>());
    }

    [Fact]
    public void Map_FieldValueObservable_IsNotRecorded()
    {
        var version = new PackageVersion { SubVersion = 2, ClientVersion = ClientVersion.Default, CompatVersion = 1 };

        var record = generator.Map(ArchiveOf(new Package { Version = version }));

        Assert.Null(record["PVER.subVersion"]);
    }

    [Fact]
    public void Reduce_RootSequence_IsEnumeratedText()
    {
        var record = new MappedArchive("a.hip", generator.Map(ArchiveOf(new HIPA())));

        var observations = generator.Reduce([record]);
        var valueSet = Assert.IsType<JsonObject>(observations[StructureFacetGenerator.RootSequenceId]);

        Assert.Equal("enumerated", valueSet["kind"]!.GetValue<string>());
        Assert.Equal("text", valueSet["presentation"]!.GetValue<string>());
    }

    [Fact]
    public void Reduce_ObservationKeys_AreSortedOrdinally()
    {
        var record = new MappedArchive("a.hip", generator.Map(ArchiveOf(new Package { Version = new PackageVersion() })));

        var observations = generator.Reduce([record]);

        var keys = observations.Select(property => property.Key).ToList();
        Assert.Equal(keys.OrderBy(key => key, StringComparer.Ordinal), keys);
    }
}

using EvilHop.Blocks;
using EvilHop.Corpus.Generation;
using EvilHop.Serialization;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Tests.Generation;

public class BlockFieldsFacetGeneratorTests
{
    private readonly BlockFieldsFacetGenerator generator = new();

    private static Archive ArchiveOf(params Block[] roots) => new(new N100FSerializer(), roots);

    private static PackageVersion VersionBlock(uint subVersion = 2) =>
        new() { SubVersion = subVersion, ClientVersion = ClientVersion.Default, CompatVersion = 1 };

    [Fact]
    public void Dependencies_IncludesEveryBlockScopedObservable()
    {
        Assert.Contains("PVER.subVersion", generator.Dependencies);
        Assert.Contains("PFLG.flags", generator.Dependencies);
    }

    [Fact]
    public void Map_ArchiveWithDecoratedBlock_RecordsObservedValue()
    {
        var record = generator.Map(ArchiveOf(VersionBlock(subVersion: 2)));

        var values = Assert.IsType<JsonArray>(record["PVER.subVersion"]);
        Assert.Equal(2u, Assert.Single(values)!.GetValue<uint>());
    }

    [Fact]
    public void Map_ArchiveWithNoMatchingBlocks_OmitsTheObservable()
    {
        var record = generator.Map(ArchiveOf());

        Assert.Null(record["PVER.subVersion"]);
    }

    [Fact]
    public void Reduce_TwoArchivesWithSameValue_GroupsIntoOneEntryWithCombinedCount()
    {
        var first = new MappedArchive("a.hip", generator.Map(ArchiveOf(VersionBlock())));
        var second = new MappedArchive("b.hip", generator.Map(ArchiveOf(VersionBlock())));

        var observations = generator.Reduce([first, second]);

        var entry = SoleValueOf(observations, "PVER.subVersion");
        Assert.Equal(2u, entry["value"]!.GetValue<uint>());
        Assert.Equal(2, entry["count"]!.GetValue<int>());
    }

    [Fact]
    public void Reduce_DistinctValues_SortsThemAscending()
    {
        var low = new MappedArchive("a.hip", generator.Map(ArchiveOf(VersionBlock(subVersion: 1))));
        var high = new MappedArchive("b.hip", generator.Map(ArchiveOf(VersionBlock(subVersion: 5))));

        var observations = generator.Reduce([high, low]);

        var values = ValuesOf(observations, "PVER.subVersion");
        Assert.Equal([1u, 5u], values.Select(v => v!["value"]!.GetValue<uint>()));
    }

    [Fact]
    public void Reduce_MoreThanTwoWitnesses_KeepsOnlyTheLexicographicallyFirstTwo()
    {
        var records = new[] { "c.hip", "a.hip", "b.hip" }
            .Select(path => new MappedArchive(path, generator.Map(ArchiveOf(VersionBlock()))))
            .ToList();

        var observations = generator.Reduce(records);

        var entry = SoleValueOf(observations, "PVER.subVersion");
        Assert.Equal(["a.hip", "b.hip"], entry["witnesses"]!.AsArray().Select(w => w!.GetValue<string>()));
    }

    [Fact]
    public void Reduce_BitmaskObservable_RecordsUnionOfObservedBits()
    {
        var first = new MappedArchive("a.hip", generator.Map(ArchiveOf(new PackageFlags { Flags = PackFlags.Unknown2 | PackFlags.GameCube })));
        var second = new MappedArchive("b.hip", generator.Map(ArchiveOf(new PackageFlags { Flags = PackFlags.Unknown2 | PackFlags.NTSC })));

        var observations = generator.Reduce([first, second]);

        var valueSet = Assert.IsType<JsonObject>(observations["PFLG.flags"]);
        Assert.Equal("bitmask", valueSet["kind"]!.GetValue<string>());
        Assert.Equal((uint)(PackFlags.Unknown2 | PackFlags.GameCube | PackFlags.NTSC), valueSet["union"]!.GetValue<uint>());
    }

    [Fact]
    public void Reduce_HexPresentedObservable_IncludesHexDisplay()
    {
        var record = new MappedArchive("a.hip", generator.Map(ArchiveOf(VersionBlock())));

        var entry = SoleValueOf(generator.Reduce([record]), "PVER.clientVersion");

        Assert.Equal($"0x{(uint)ClientVersion.Default:X8}", entry["display"]!.GetValue<string>());
    }

    [Fact]
    public void Reduce_NumberPresentedObservable_OmitsDisplay()
    {
        var record = new MappedArchive("a.hip", generator.Map(ArchiveOf(VersionBlock())));

        var entry = SoleValueOf(generator.Reduce([record]), "PVER.subVersion");

        Assert.Null(entry["display"]);
    }

    private static JsonArray ValuesOf(JsonObject observations, string observableId) =>
        Assert.IsType<JsonArray>(Assert.IsType<JsonObject>(observations[observableId])["values"]);

    private static JsonObject SoleValueOf(JsonObject observations, string observableId) =>
        Assert.Single(ValuesOf(observations, observableId))!.AsObject();
}

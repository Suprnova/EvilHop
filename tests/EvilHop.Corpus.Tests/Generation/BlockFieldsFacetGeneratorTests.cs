using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Corpus.Generation;
using EvilHop.Serialization;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Tests.Generation;

public class BlockFieldsFacetGeneratorTests
{
    private readonly BlockFieldsFacetGenerator generator = new();
    private static readonly string[] sourceArray = ["c.hip", "a.hip", "b.hip"];

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
        Assert.Equal(2L, Assert.Single(values)!.GetValue<long>());
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
        Assert.Equal(2L, entry["value"]!.GetValue<long>());
        Assert.Equal(2, entry["count"]!.GetValue<int>());
    }

    [Fact]
    public void Reduce_DistinctValues_SortsThemAscending()
    {
        var low = new MappedArchive("a.hip", generator.Map(ArchiveOf(VersionBlock(subVersion: 1))));
        var high = new MappedArchive("b.hip", generator.Map(ArchiveOf(VersionBlock(subVersion: 5))));

        var observations = generator.Reduce([high, low]);

        var values = ValuesOf(observations, "PVER.subVersion");
        Assert.Equal([1L, 5L], values.Select(v => v!["value"]!.GetValue<long>()));
    }

    [Fact]
    public void Reduce_MoreThanTwoWitnesses_KeepsOnlyTheLexicographicallyFirstTwo()
    {
        var records = sourceArray.Select(path => new MappedArchive(path, generator.Map(ArchiveOf(VersionBlock()))))
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

    [Fact]
    public void Reduce_DisplayPresent_IsOrderedBeforeValue()
    {
        var record = new MappedArchive("a.hip", generator.Map(ArchiveOf(VersionBlock())));

        var entry = SoleValueOf(generator.Reduce([record]), "PVER.clientVersion");

        Assert.Equal(["display", "value", "count", "witnesses"], entry.Select(property => property.Key));
    }

    [Fact]
    public void Reduce_FourccPresentedObservable_IncludesAsciiDisplay()
    {
        var record = new MappedArchive("a.hip", generator.Map(ArchiveOf(new AssetHeader { Type = AssetType.Animation })));

        var observations = generator.Reduce([record]);
        var valueSet = Assert.IsType<JsonObject>(observations["AHDR.type"]);

        Assert.Equal("fourcc", valueSet["presentation"]!.GetValue<string>());
        Assert.Equal("ANIM", SoleValueOf(observations, "AHDR.type")["display"]!.GetValue<string>());
    }

    [Fact]
    public void Reduce_ObservationKeys_AreSortedOrdinally()
    {
        var record = new MappedArchive("a.hip", generator.Map(ArchiveOf(VersionBlock(), new PackageFlags { Flags = PackFlags.NTSC })));

        var observations = generator.Reduce([record]);

        var keys = observations.Select(property => property.Key).ToList();
        Assert.Equal(keys.OrderBy(key => key, StringComparer.Ordinal), keys);
    }

    private static JsonArray ValuesOf(JsonObject observations, string observableId) =>
        Assert.IsType<JsonArray>(Assert.IsType<JsonObject>(observations[observableId])["values"]);

    private static JsonObject SoleValueOf(JsonObject observations, string observableId) =>
        Assert.Single(ValuesOf(observations, observableId))!.AsObject();
}

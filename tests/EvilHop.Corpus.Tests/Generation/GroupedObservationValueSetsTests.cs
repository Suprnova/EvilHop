using EvilHop.Corpus.Generation;
using EvilHop.Validation;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Tests.Generation;

public class GroupedObservationValueSetsTests
{
    private const uint Boul = 0x424F554C;
    private const uint Trig = 0x54524947;

    private static Observable Grouped(
        ObservableCardinality cardinality = ObservableCardinality.Enumerated,
        ObservablePresentation presentation = ObservablePresentation.Number) =>
        new("x@assetType", ObservableScope.Asset, cardinality, presentation, _ => [],
            ObservableKind.FieldValue, ObservableGrouping.AssetType, ObservablePresentation.Fourcc);

    private static MappedArchive Record(string path, params (uint Key, long Value)[] occurrences)
    {
        var record = new JsonObject();
        foreach (var (key, value) in occurrences)
            ObservationValueSets.Append(record, new Observation("x@assetType", value, key));

        return new MappedArchive(path, record);
    }

    [Fact]
    public void Append_GroupedObservation_BucketsByKey()
    {
        var record = Record("a.hip", (Trig, 16), (Boul, 32), (Trig, 16)).Record;

        var groups = Assert.IsType<JsonObject>(record["x@assetType"]);
        Assert.Equal([16L, 16L], Assert.IsType<JsonArray>(groups[Trig.ToString()]).Select(v => v!.GetValue<long>()));
        Assert.Equal(32L, Assert.Single(Assert.IsType<JsonArray>(groups[Boul.ToString()]))!.GetValue<long>());
    }

    [Fact]
    public void Reduce_GroupedObservable_RecordsOneValueSetPerKey()
    {
        var reduced = ObservationValueSets.Reduce(Grouped(), [Record("a.hip", (Trig, 16), (Boul, 32))]);

        Assert.Equal("grouped", reduced["kind"]!.GetValue<string>());
        Assert.Equal("assetType", reduced["groupedBy"]!.GetValue<string>());
        Assert.Equal("fourcc", reduced["keyPresentation"]!.GetValue<string>());
        Assert.Equal("enumerated", reduced["valueKind"]!.GetValue<string>());
        Assert.Equal(2, Assert.IsType<JsonArray>(reduced["groups"]).Count);
    }

    [Fact]
    public void Reduce_GroupedObservable_SortsGroupsByKeyAscending()
    {
        var reduced = ObservationValueSets.Reduce(Grouped(), [Record("a.hip", (Trig, 16), (Boul, 32))]);

        var keys = Assert.IsType<JsonArray>(reduced["groups"]).Select(group => group!["key"]!.GetValue<uint>());
        Assert.Equal([Boul, Trig], keys);
    }

    [Fact]
    public void Reduce_GroupedObservable_RendersEachKeyThroughItsKeyPresentation()
    {
        var reduced = ObservationValueSets.Reduce(Grouped(), [Record("a.hip", (Trig, 16))]);

        Assert.Equal("TRIG", GroupOf(reduced, Trig)["keyDisplay"]!.GetValue<string>());
    }

    [Fact]
    public void Reduce_GroupedObservable_KeyIsOrderedBeforeItsDisplayAndPayload()
    {
        var reduced = ObservationValueSets.Reduce(Grouped(), [Record("a.hip", (Trig, 16))]);

        Assert.Equal(["key", "keyDisplay", "values"], GroupOf(reduced, Trig).Select(property => property.Key));
    }

    [Fact]
    public void Reduce_GroupedObservable_CountsAndWitnessesEachGroupSeparately()
    {
        var reduced = ObservationValueSets.Reduce(Grouped(),
            [Record("b.hip", (Trig, 16), (Boul, 32)), Record("a.hip", (Trig, 16))]);

        var entry = Assert.Single(Assert.IsType<JsonArray>(GroupOf(reduced, Trig)["values"]))!.AsObject();
        Assert.Equal(2, entry["count"]!.GetValue<int>());
        Assert.Equal(["a.hip", "b.hip"], entry["witnesses"]!.AsArray().Select(w => w!.GetValue<string>()));
    }

    [Fact]
    public void Reduce_GroupedBitmaskObservable_UnionsEachGroupSeparately()
    {
        var reduced = ObservationValueSets.Reduce(
            Grouped(ObservableCardinality.Bitmask, ObservablePresentation.Hex),
            [Record("a.hip", (Trig, 0b0001), (Trig, 0b0100), (Boul, 0b0010))]);

        Assert.Equal("bitmask", reduced["valueKind"]!.GetValue<string>());
        Assert.Equal(0b0101u, GroupOf(reduced, Trig)["union"]!.GetValue<uint>());
        Assert.Equal(0b0010u, GroupOf(reduced, Boul)["union"]!.GetValue<uint>());
    }

    [Fact]
    public void Reduce_GroupedObservable_ReducesEachGroupAsItsOwnValueSet()
    {
        var grouped = ObservationValueSets.Reduce(Grouped(), [Record("a.hip", (Trig, 16), (Trig, 32))]);

        var ungrouped = ObservationValueSets.Reduce(
            "x", ObservableCardinality.Enumerated, ObservablePresentation.Number,
            [new MappedArchive("a.hip", new JsonObject { ["x"] = new JsonArray { 16L, 32L } })]);

        Assert.Equal(ungrouped["values"]!.ToJsonString(), GroupOf(grouped, Trig)["values"]!.ToJsonString());
    }

    [Fact]
    public void Reduce_GroupedObservable_IsInsensitiveToArchiveOrder()
    {
        var first = Record("a.hip", (Trig, 16), (Boul, 32));
        var second = Record("b.hip", (Boul, 32), (Trig, 48));

        var forward = ObservationValueSets.Reduce(Grouped(), [first, second]);
        var backward = ObservationValueSets.Reduce(Grouped(), [second, first]);

        Assert.Equal(forward.ToJsonString(), backward.ToJsonString());
    }

    [Fact]
    public void Reduce_GroupedObservable_ReadsBackFromACachedRecordIdentically()
    {
        var fresh = Record("a.hip", (Trig, 16), (Boul, 32));
        var cached = new MappedArchive(fresh.Path, (JsonObject)JsonNode.Parse(fresh.Record.ToJsonString())!);

        Assert.Equal(
            ObservationValueSets.Reduce(Grouped(), [fresh]).ToJsonString(),
            ObservationValueSets.Reduce(Grouped(), [cached]).ToJsonString());
    }

    [Fact]
    public void Reduce_MoreGroupsThanTheCap_Throws()
    {
        var occurrences = Enumerable.Range(0, ObservationValueSets.MaxGroups + 1)
            .Select(i => ((uint)i, 1L))
            .ToArray();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ObservationValueSets.Reduce(Grouped(), [Record("a.hip", occurrences)]));

        Assert.Contains("group cap", exception.Message);
    }

    [Fact]
    public void Reduce_MoreValuesAcrossGroupsThanTheCap_ThrowsNamingTheWidestGroups()
    {
        // Every group stays under the per-group cap, so only the cap across groups catches this -
        // which is the whole reason it exists.
        int perGroup = ObservationValueSets.MaxGroupedValues / 4;
        var occurrences = Enumerable.Range(0, 5)
            .SelectMany(group => Enumerable.Range(0, perGroup).Select(value => ((uint)group, (long)value)))
            .ToArray();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ObservationValueSets.Reduce(Grouped(), [Record("a.hip", occurrences)]));

        Assert.Contains("Widest groups", exception.Message);
    }

    [Fact]
    public void Reduce_MoreValuesInOneGroupThanTheValueCap_ThrowsNamingTheGroup()
    {
        var occurrences = Enumerable.Range(0, ObservationValueSets.MaxEnumeratedValues + 1)
            .Select(value => (Trig, (long)value))
            .ToArray();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ObservationValueSets.Reduce(Grouped(), [Record("a.hip", occurrences)]));

        Assert.Contains("x@assetType[TRIG]", exception.Message);
    }

    private static JsonObject GroupOf(JsonObject reduced, uint key) =>
        Assert.IsType<JsonArray>(reduced["groups"]).Single(group => group!["key"]!.GetValue<uint>() == key)!.AsObject();
}

using EvilHop.Corpus.Generation;
using EvilHop.Validation;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Tests.Generation;

public class ObservationValueSetsTests
{
    [Fact]
    public void Reduce_ValueRoundTrippedThroughJson_GroupsWithAFreshlyMappedOccurrenceOfTheSameValue()
    {
        // A MapCache hit re-parses a previous run's JSON text, while a cache miss keeps the value
        // ToJsonValue just created in memory. Those two representations of the same number disagree
        // on their underlying CLR type unless narrowed to one canonical type at the source - this is
        // the exact mismatch that once let two differently-boxed zeros reach the same reduce pass.
        var fresh = new JsonObject { ["x"] = new JsonArray { ObservationValueSets.ToJsonValue(0) } };
        var roundTripped = (JsonObject)JsonNode.Parse(fresh.ToJsonString())!;
        var records = new List<MappedArchive> { new("a.hip", fresh), new("b.hip", roundTripped) };

        var valueSet = ObservationValueSets.Reduce("x", ObservableCardinality.Enumerated, ObservablePresentation.Number, records);

        var entry = Assert.Single(Assert.IsType<JsonArray>(valueSet["values"]))!.AsObject();
        Assert.Equal(2, entry["count"]!.GetValue<int>());
    }

    [Fact]
    public void Reduce_NegativeValue_RecordsItRatherThanFailing()
    {
        // ADBG.alignment stores -1 to mean "use this type's default", so a whole number the recorder
        // carries has to be signed - narrowing to uint would fail generation on real archives.
        var records = new List<MappedArchive>
        {
            new("a.hip", new JsonObject { ["x"] = new JsonArray { ObservationValueSets.ToJsonValue(-1) } })
        };

        var valueSet = ObservationValueSets.Reduce("x", ObservableCardinality.Enumerated, ObservablePresentation.Number, records);

        var entry = Assert.Single(Assert.IsType<JsonArray>(valueSet["values"]))!.AsObject();
        Assert.Equal(-1L, entry["value"]!.GetValue<long>());
    }

    [Fact]
    public void Reduce_TooManyDistinctValues_Throws()
    {
        var records = Enumerable.Range(0, ObservationValueSets.MaxEnumeratedValues + 1)
            .Select(i => new MappedArchive($"{i}.hip", new JsonObject { ["x"] = new JsonArray { ObservationValueSets.ToJsonValue((uint)i) } }))
            .ToList();

        Assert.Throws<InvalidOperationException>(() =>
            ObservationValueSets.Reduce("x", ObservableCardinality.Enumerated, ObservablePresentation.Number, records));
    }
}

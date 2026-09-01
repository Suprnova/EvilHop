using EvilHop.Corpus.Json;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Tests.Json;

public class DeterministicJsonTests
{
    [Fact]
    public void Serialize_ObjectWithUnsortedKeys_SortsThemOrdinally()
    {
        var node = new JsonObject { ["zebra"] = 1, ["apple"] = 2 };

        string json = DeterministicJson.Serialize(node);

        Assert.True(json.IndexOf("apple", StringComparison.Ordinal) < json.IndexOf("zebra", StringComparison.Ordinal));
    }

    [Fact]
    public void Serialize_NestedObject_SortsKeysAtEveryLevel()
    {
        var node = new JsonObject { ["outer"] = new JsonObject { ["zebra"] = 1, ["apple"] = 2 } };

        string json = DeterministicJson.Serialize(node);

        Assert.True(json.IndexOf("apple", StringComparison.Ordinal) < json.IndexOf("zebra", StringComparison.Ordinal));
    }

    [Fact]
    public void Serialize_Array_PreservesElementOrder()
    {
        var node = new JsonArray { 3, 1, 2 };

        string json = DeterministicJson.Serialize(node);

        Assert.Equal(["3", "1", "2"], JsonNode.Parse(json)!.AsArray().Select(n => n!.ToJsonString()));
    }

    [Fact]
    public void Serialize_UsesTwoSpaceIndent()
    {
        var node = new JsonObject { ["key"] = 1 };

        string json = DeterministicJson.Serialize(node);

        Assert.Contains("\n  \"key\"", json);
    }

    [Fact]
    public void Serialize_EndsWithExactlyOneTrailingNewline()
    {
        var node = new JsonObject { ["key"] = 1 };

        string json = DeterministicJson.Serialize(node);

        Assert.EndsWith("\n", json);
        Assert.False(json.EndsWith("\n\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Serialize_NeverEmitsCarriageReturns()
    {
        var node = new JsonObject { ["outer"] = new JsonObject { ["inner"] = 1 } };

        string json = DeterministicJson.Serialize(node);

        Assert.DoesNotContain('\r', json);
    }

    [Fact]
    public void Serialize_CalledTwiceOnEquivalentTrees_ProducesByteIdenticalOutput()
    {
        var first = new JsonObject { ["b"] = 2, ["a"] = 1 };
        var second = new JsonObject { ["a"] = 1, ["b"] = 2 };

        Assert.Equal(DeterministicJson.Serialize(first), DeterministicJson.Serialize(second));
    }
}

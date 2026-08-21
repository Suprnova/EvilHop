using EvilHop.Common;

namespace EvilHop.Corpus.Tests;

public class CorpusOptionsTests
{
    [Fact]
    public void Parse_InventoryWithMultipleRoots_PreservesAllRoots()
    {
        var options = CorpusOptions.Parse(["inventory", "--out", "corpus/n100f.json", "artifacts/n100f", "artifacts/bfbb"]);

        Assert.Equal(CorpusVerb.Inventory, options.Verb);
        Assert.Equal(["artifacts/n100f", "artifacts/bfbb"], options.Roots);
        Assert.Equal("corpus/n100f.json", options.OutputPath);
    }

    [Fact]
    public void Parse_WithoutSerializerFlag_DefaultsToN100F()
    {
        var options = CorpusOptions.Parse(["verify", "artifacts/n100f"]);

        Assert.Equal(GameVersion.N100F, options.Game);
    }

    [Theory]
    [InlineData("n100f", GameVersion.N100F)]
    [InlineData("N100F", GameVersion.N100F)]
    [InlineData("bfbb", GameVersion.BFBB)]
    [InlineData("BFBB", GameVersion.BFBB)]
    public void Parse_SerializerFlag_ParsesGameKeyCaseInsensitively(string key, GameVersion expected)
    {
        var options = CorpusOptions.Parse(["verify", "--serializer", key, "artifacts/bfbb"]);

        Assert.Equal(CorpusVerb.Verify, options.Verb);
        Assert.Equal(expected, options.Game);
    }

    [Fact]
    public void Parse_SerializerFlag_UnknownGameKey_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => CorpusOptions.Parse(["verify", "--serializer", "gamecube", "artifacts/n100f"]));

        Assert.Contains("gamecube", ex.Message);
    }

    [Fact]
    public void Parse_WithoutRoundTripFlag_DefaultsToFalse()
    {
        var options = CorpusOptions.Parse(["verify", "artifacts/n100f"]);

        Assert.False(options.RoundTrip);
    }

    [Fact]
    public void Parse_RoundTripFlag_SetsRoundTripTrue()
    {
        var options = CorpusOptions.Parse(["verify", "--round-trip", "artifacts/n100f"]);

        Assert.True(options.RoundTrip);
    }

    [Fact]
    public void Parse_InventoryWithDump_SetsDumpPath()
    {
        var options = CorpusOptions.Parse(["inventory", "--out", "corpus/n100f.json", "--dump", "dump/n100f.jsonl", "artifacts/n100f"]);

        Assert.Equal("dump/n100f.jsonl", options.DumpPath);
    }

    [Fact]
    public void Parse_NoArguments_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CorpusOptions.Parse([]));
    }

    [Fact]
    public void Parse_UnknownVerb_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CorpusOptions.Parse(["explode", "artifacts/n100f"]));
    }

    [Fact]
    public void Parse_InventoryWithoutOut_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CorpusOptions.Parse(["inventory", "artifacts/n100f"]));
    }

    [Fact]
    public void Parse_VerifyWithoutRoot_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CorpusOptions.Parse(["verify"]));
    }

    [Fact]
    public void Parse_FlagMissingValue_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CorpusOptions.Parse(["inventory", "--out"]));
    }
}

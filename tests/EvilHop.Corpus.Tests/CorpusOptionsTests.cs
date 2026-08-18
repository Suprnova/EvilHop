namespace EvilHop.Corpus.Tests;

public class CorpusOptionsTests
{
    [Fact]
    public void Parse_InventoryWithMultipleRoots_PreservesAllRoots()
    {
        var options = CorpusOptions.Parse(["inventory", "--out", "corpus/v1.json", "artifacts/n100f", "artifacts/bfbb"]);

        Assert.Equal(CorpusVerb.Inventory, options.Verb);
        Assert.Equal(["artifacts/n100f", "artifacts/bfbb"], options.Roots);
        Assert.Equal("corpus/v1.json", options.OutputPath);
    }

    [Fact]
    public void Parse_VerifyWithSerializerAndRoot_SetsSerializerId()
    {
        var options = CorpusOptions.Parse(["verify", "--serializer", "v2", "artifacts/bfbb"]);

        Assert.Equal(CorpusVerb.Verify, options.Verb);
        Assert.Equal("v2", options.SerializerId);
    }

    [Fact]
    public void Parse_InventoryWithDump_SetsDumpPath()
    {
        var options = CorpusOptions.Parse(["inventory", "--out", "corpus/v1.json", "--dump", "dump/v1.jsonl", "artifacts/n100f"]);

        Assert.Equal("dump/v1.jsonl", options.DumpPath);
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

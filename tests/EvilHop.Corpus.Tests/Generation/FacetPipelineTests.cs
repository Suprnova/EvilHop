using EvilHop.Corpus.Caching;
using EvilHop.Corpus.Generation;
using EvilHop.Corpus.Json;
using EvilHop.Serialization;
using System.Security.Cryptography;

namespace EvilHop.Corpus.Tests.Generation;

public class FacetPipelineTests : IDisposable
{
    private readonly string cacheDirectory = Path.Combine(Path.GetTempPath(), $"evilhop-corpus-pipeline-tests-{Guid.NewGuid()}");

    public void Dispose()
    {
        if (Directory.Exists(cacheDirectory)) Directory.Delete(cacheDirectory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static CoveredArchive LoadFixture(string game, string fileName = "minimal.hip")
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", game, fileName);
        byte[] bytes = File.ReadAllBytes(path);
        string sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var archive = Archive.Load(new MemoryStream(bytes), SerializerFor(game));
        return new CoveredArchive($"{game}/{fileName}", sha256, archive);
    }

    private static Serializer SerializerFor(string game) => game switch
    {
        "n100f" => new N100FSerializer(),
        "bfbb" => new BFBBSerializer(),
        "incredibles" => new IncrediblesSerializer(),
        "tssm" => new TSSMSerializer(),
        "rotu" => new ROTUSerializer(),
        "ratatouille" => new RatatouilleSerializer(),
        _ => throw new ArgumentOutOfRangeException(nameof(game))
    };

    [Fact]
    public void Run_CacheMissThenCacheHit_ProduceByteIdenticalOutput()
    {
        var archives = new[] { LoadFixture("n100f"), LoadFixture("bfbb") };
        var generator = new BlockFieldsFacetGenerator();

        string miss = DeterministicJson.Serialize(FacetPipeline.Run(generator, archives, new MapCache(cacheDirectory)));
        string hit = DeterministicJson.Serialize(FacetPipeline.Run(generator, archives, new MapCache(cacheDirectory)));

        Assert.Equal(miss, hit);
    }

    [Fact]
    public void Run_PopulatesTheCacheForEachCoveredArchive()
    {
        var archive = LoadFixture("n100f");
        IFacetGenerator generator = new BlockFieldsFacetGenerator();
        var cache = new MapCache(cacheDirectory);

        FacetPipeline.Run(generator, [archive], cache);

        Assert.True(cache.TryGet(generator.Id, archive.Sha256, generator.InputFingerprint(), out _));
    }

    [Fact]
    public void Run_IncludesGeneratorRevisionAndInputFingerprint()
    {
        IFacetGenerator generator = new BlockFieldsFacetGenerator();

        var facet = FacetPipeline.Run(generator, [], new MapCache(cacheDirectory));

        Assert.Equal(generator.Revision, facet["generator"]!["revision"]!.GetValue<int>());
        Assert.Equal(generator.InputFingerprint(), facet["generator"]!["inputs"]!.GetValue<string>());
    }

    [Fact]
    public void Run_RecordsCoverageArchiveCount()
    {
        var archives = new[] { LoadFixture("n100f"), LoadFixture("bfbb") };
        var generator = new BlockFieldsFacetGenerator();

        var facet = FacetPipeline.Run(generator, archives, new MapCache(cacheDirectory));

        Assert.Equal(2, facet["coverage"]!["archives"]!.GetValue<int>());
    }
}

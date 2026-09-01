using EvilHop;
using EvilHop.Common;
using EvilHop.Corpus;
using EvilHop.Corpus.Caching;
using EvilHop.Corpus.Discovery;
using EvilHop.Corpus.Generation;
using EvilHop.Corpus.Json;
using EvilHop.Serialization;
using System.Text.Json.Nodes;

if (args is not ["inventory", ..])
{
    Console.Error.WriteLine("Usage: evilhop-corpus inventory [--manifest <path>] [--root <path>] [--out <dir>] [--cache <dir>]");
    return 1;
}

string manifestPath = OptionValue(args, "--manifest") ?? "corpus/manifest.json";
string artifactRoot = OptionValue(args, "--root") ?? "artifacts";
string outDirectory = OptionValue(args, "--out") ?? "corpus";
string cacheDirectory = OptionValue(args, "--cache") ?? ".corpus-cache";

Manifest manifest = File.Exists(manifestPath) ? Manifest.Load(manifestPath) : Manifest.Empty;

if (manifest.Builds.Count == 0)
{
    Console.WriteLine($"No builds declared in '{manifestPath}'; nothing to generate.");
    return 0;
}

var cache = new MapCache(cacheDirectory);
var fileHashes = new FileHashCache(Path.Combine(cacheDirectory, "file-hashes.json"));

IReadOnlyList<IFacetGenerator> generators = [new BlockFieldsFacetGenerator(), new StructureFacetGenerator()];
var fingerprints = generators.ToDictionary(g => g.Id, g => g.InputFingerprint());

// Grouped by directory-implied game only - an override can change how an archive is read, but never
// which game's inventory it's counted toward. Each game accumulates its sha256s (for
// coverage.sourceSetHash) and its small, already-mapped per-archive records - never the loaded
// Archive itself, which is dropped as soon as every generator has mapped it. Holding every archive
// in the covered set alive at once is fine for the tool's own tests over a handful of fixtures; it
// isn't for a real corpus, where the raw archives alone run into tens of gigabytes.
var byGame = new Dictionary<GameVersion, GameAccumulator>();
int failures = 0;
int processed = 0;

foreach (var build in manifest.Builds)
{
    var directoryGame = BuildProfiles.GameFor(build.Directory);
    var platform = BuildProfiles.PlatformFor(build.Directory);

    foreach (var found in ArchiveDiscovery.Find(artifactRoot, build.Directory))
    {
        var over = manifest.Overrides.FirstOrDefault(o => string.Equals(o.Path, found.RelativePath, StringComparison.OrdinalIgnoreCase));

        try
        {
            string sha256 = fileHashes.GetOrCompute(found.FullPath);
            var readGame = over?.Game ?? directoryGame;
            var profile = BuildProfiles.ProfileFor(readGame, platform) with { Quirks = over?.Quirks ?? FormatQuirks.None };

            if (!byGame.TryGetValue(directoryGame, out var accumulator))
                byGame[directoryGame] = accumulator = new GameAccumulator();
            accumulator.Sha256s.Add(sha256);

            foreach (var (generator, record) in MapWithCache(found, readGame, profile, sha256))
                accumulator.RecordsFor(generator.Id).Add(new MappedArchive(found.RelativePath, record));

            if (++processed % 200 == 0) Console.WriteLine($"...{processed} archives mapped.");
        }
        catch (Exception ex)
        {
            failures++;
            Console.Error.WriteLine($"Skipping '{found.RelativePath}': {ex.Message}");
        }
    }
}

Directory.CreateDirectory(outDirectory);

foreach (var (game, accumulator) in byGame.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal))
{
    var facets = new JsonObject();
    foreach (var generator in generators.OrderBy(g => g.Id, StringComparer.Ordinal))
    {
        var records = accumulator.RecordsFor(generator.Id);
        facets[generator.Id] = new JsonObject
        {
            ["generator"] = new JsonObject { ["revision"] = generator.Revision, ["inputs"] = fingerprints[generator.Id] },
            ["coverage"] = new JsonObject
            {
                ["archives"] = accumulator.Sha256s.Count,
                ["sourceSetHash"] = FacetPipeline.SourceSetHash(accumulator.Sha256s)
            },
            ["observations"] = generator.Reduce(records)
        };
    }

    var inventory = new JsonObject { ["schema"] = 1, ["game"] = game.ToString(), ["facets"] = facets };

    string path = Path.Combine(outDirectory, $"{game.ToString().ToLowerInvariant()}.json");
    File.WriteAllText(path, DeterministicJson.Serialize(inventory));
    Console.WriteLine($"Wrote '{path}' ({accumulator.Sha256s.Count} archives).");
}

if (failures > 0) Console.WriteLine($"{failures} archive(s) failed to load and were excluded; see above.");

fileHashes.Save();

return 0;

// Checks every generator's cache first and only loads the archive - the expensive step - if at
// least one of them missed. A fully cached archive is never opened at all.
IEnumerable<(IFacetGenerator Generator, JsonObject Record)> MapWithCache(
    DiscoveredArchive found, GameVersion readGame, FormatProfile profile, string sha256)
{
    var cached = new Dictionary<string, JsonObject>();
    var pending = new List<IFacetGenerator>();

    foreach (var generator in generators)
    {
        if (cache.TryGet(generator.Id, sha256, fingerprints[generator.Id], out string? json))
            cached[generator.Id] = (JsonObject)JsonNode.Parse(json!)!;
        else
            pending.Add(generator);
    }

    Archive? archive = null;
    if (pending.Count > 0)
    {
        var serializer = BuildProfiles.SerializerFor(readGame, profile);
        using var stream = File.OpenRead(found.FullPath);
        archive = Archive.Load(stream, serializer);
    }

    foreach (var generator in generators)
    {
        if (cached.TryGetValue(generator.Id, out var record))
        {
            yield return (generator, record);
            continue;
        }

        record = generator.Map(archive!);
        cache.Set(generator.Id, sha256, fingerprints[generator.Id], record.ToJsonString());
        yield return (generator, record);
    }
}

static string? OptionValue(string[] args, string name)
{
    int index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

/// <summary>One game's accumulated coverage: every covered archive's hash, and each facet's mapped records.</summary>
file sealed class GameAccumulator
{
    public List<string> Sha256s { get; } = [];

    private readonly Dictionary<string, List<MappedArchive>> _recordsByFacet = [];

    public List<MappedArchive> RecordsFor(string facetId)
    {
        if (!_recordsByFacet.TryGetValue(facetId, out var records))
            _recordsByFacet[facetId] = records = [];
        return records;
    }
}

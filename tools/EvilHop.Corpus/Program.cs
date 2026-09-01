using EvilHop;
using EvilHop.Common;
using EvilHop.Corpus;
using EvilHop.Corpus.Caching;
using EvilHop.Corpus.Discovery;
using EvilHop.Corpus.Generation;
using EvilHop.Corpus.Json;
using EvilHop.Serialization;
using System.Security.Cryptography;
using System.Text;
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
IFacetGenerator generator = new BlockFieldsFacetGenerator();
string inputFingerprint = generator.InputFingerprint();

// Grouped by directory-implied game only - an override can change how an archive is read, but never
// which game's inventory it's counted toward. Each game accumulates its sha256s (for
// coverage.sourceSetHash) and its small, already-mapped per-archive records - never the loaded
// Archive itself, which is dropped as soon as it's been mapped. Holding every archive in the covered
// set alive at once is fine for the tool's own tests over a handful of fixtures; it isn't for a real
// corpus, where the raw archives alone run into tens of gigabytes.
var byGame = new Dictionary<GameVersion, (List<string> Sha256s, List<MappedArchive> Records)>();
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
            JsonObject record = MapWithCache(found, readGame, profile, sha256);

            if (!byGame.TryGetValue(directoryGame, out var accumulator))
                byGame[directoryGame] = accumulator = ([], []);

            accumulator.Sha256s.Add(sha256);
            accumulator.Records.Add(new MappedArchive(found.RelativePath, record));

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
    var inventory = new JsonObject
    {
        ["schema"] = 1,
        ["game"] = game.ToString(),
        ["facets"] = new JsonObject
        {
            [generator.Id] = new JsonObject
            {
                ["generator"] = new JsonObject { ["revision"] = generator.Revision, ["inputs"] = inputFingerprint },
                ["coverage"] = new JsonObject
                {
                    ["archives"] = accumulator.Sha256s.Count,
                    ["sourceSetHash"] = SourceSetHash(accumulator.Sha256s)
                },
                ["observations"] = generator.Reduce(accumulator.Records)
            }
        }
    };

    string path = Path.Combine(outDirectory, $"{game.ToString().ToLowerInvariant()}.json");
    File.WriteAllText(path, DeterministicJson.Serialize(inventory));
    Console.WriteLine($"Wrote '{path}' ({accumulator.Sha256s.Count} archives).");
}

if (failures > 0) Console.WriteLine($"{failures} archive(s) failed to load and were excluded; see above.");

fileHashes.Save();

return 0;

JsonObject MapWithCache(DiscoveredArchive found, GameVersion readGame, FormatProfile profile, string sha256)
{
    if (cache.TryGet(generator.Id, sha256, inputFingerprint, out string? cached))
        return (JsonObject)JsonNode.Parse(cached!)!;

    var serializer = BuildProfiles.SerializerFor(readGame, profile);
    using var stream = File.OpenRead(found.FullPath);
    var archive = Archive.Load(stream, serializer);

    JsonObject record = generator.Map(archive);
    cache.Set(generator.Id, sha256, inputFingerprint, record.ToJsonString());
    return record;
}

static string SourceSetHash(IReadOnlyList<string> sha256s)
{
    string joined = string.Join('\n', sha256s.OrderBy(sha => sha, StringComparer.Ordinal));
    return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(joined)))[..7];
}

static string? OptionValue(string[] args, string name)
{
    int index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

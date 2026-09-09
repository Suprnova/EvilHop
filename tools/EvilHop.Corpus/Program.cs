using EvilHop;
using EvilHop.Corpus;
using EvilHop.Corpus.Archives;
using EvilHop.Corpus.Invariants;
using EvilHop.Corpus.Output;
using EvilHop.Serialization;
using System.Diagnostics;

try
{
    var options = CorpusOptions.Parse(args);
    return options.Verb switch
    {
        CorpusVerb.Verify => RunVerify(options),
        CorpusVerb.Inventory => RunInventory(options),
        CorpusVerb.SniffVerify => RunSniffVerify(options),
        _ => throw new UnreachableException()
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static int RunVerify(CorpusOptions options)
{
    var defaultProfile = SerializerFactory.DefaultProfileFor(options.Game);
    var buildProfiles = BuildProfiles.LoadDefault();
    var serializers = new Dictionary<FormatProfile, Serializer>();

    int total = 0, failed = 0;
    foreach (var discovered in ArchiveWalker.Discover(options.Roots))
    {
        total++;

        try
        {
            var serializer = ResolveSerializer(discovered, defaultProfile, buildProfiles, serializers);
            byte[] originalBytes = File.ReadAllBytes(discovered.FullPath);
            var archive = Archive.Load(new MemoryStream(originalBytes), serializer);

            if (options.RoundTrip && RoundTrip.Check(archive, originalBytes) is string mismatch)
            {
                failed++;
                Console.Error.WriteLine($"FAIL {discovered.RelativePath}: {mismatch}");
            }
        }
        catch (Exception ex)
        {
            failed++;
            Console.Error.WriteLine($"FAIL {discovered.RelativePath}: {ex.Message}");
        }
    }

    Console.WriteLine($"{total - failed}/{total} archives parsed successfully.");
    return failed == 0 ? 0 : 1;
}

static int RunInventory(CorpusOptions options)
{
    var defaultProfile = SerializerFactory.DefaultProfileFor(options.Game);
    var buildProfiles = BuildProfiles.LoadDefault();
    var serializers = new Dictionary<FormatProfile, Serializer>();
    var builder = new InventoryBuilder(InvariantRegistry.CreateAll());
    using var dump = options.DumpPath is not null ? new DumpWriter(options.DumpPath) : null;

    int processed = 0;
    foreach (var discovered in ArchiveWalker.Discover(options.Roots))
    {
        var serializer = ResolveSerializer(discovered, defaultProfile, buildProfiles, serializers);
        var context = Read(serializer, discovered);
        builder.Observe(context);
        dump?.Write(context);

        processed++;
        if (processed % 100 == 0) Console.WriteLine($"Processed {processed} archives...");
    }

    Console.WriteLine($"Processed {processed} archives.");

    InventoryWriter.Write(options.OutputPath!, builder);
    Console.WriteLine($"Wrote inventory to {options.OutputPath}");
    return 0;
}

static int RunSniffVerify(CorpusOptions options)
{
    int total = 0, failed = 0;
    foreach (var discovered in ArchiveWalker.Discover(options.Roots))
    {
        total++;
        try
        {
            using var stream = File.OpenRead(discovered.FullPath);
            var sniff = Serializer.Sniff(stream);

            if (sniff.Profile is null || sniff.Profile.Game != options.Game)
            {
                failed++;
                string guess = sniff.Profile is null ? "Unrecognized" : sniff.Profile.Game.ToString();
                Console.Error.WriteLine($"FAIL {discovered.RelativePath}: sniffed as {guess}, expected {options.Game}.");
            }
        }
        catch (Exception ex)
        {
            failed++;
            Console.Error.WriteLine($"FAIL {discovered.RelativePath}: {ex.Message}");
        }
    }

    Console.WriteLine($"{total - failed}/{total} archives correctly sniffed as {options.Game}.");
    return failed == 0 ? 0 : 1;
}

/// <summary>
/// The serializer <paramref name="discovered"/> should be read with: its game's default profile,
/// then the platform the archive declares for itself, then any committed per-build override - which
/// is applied last because it is the curated record, and is what names a platform for the builds
/// whose archives don't declare one.
/// </summary>
static Serializer ResolveSerializer(
    DiscoveredArchive discovered,
    FormatProfile defaultProfile,
    BuildProfiles buildProfiles,
    Dictionary<FormatProfile, Serializer> cache)
{
    var profile = buildProfiles.Resolve(
        SerializerFactory.WithSniffedPlatform(defaultProfile, discovered.FullPath), discovered.RelativePath);

    if (!cache.TryGetValue(profile, out var serializer))
        cache[profile] = serializer = SerializerFactory.Create(profile);

    return serializer;
}

static ArchiveContext Read(Serializer serializer, DiscoveredArchive discovered)
{
    var fileInfo = new FileInfo(discovered.FullPath);
    using var stream = File.OpenRead(discovered.FullPath);

    try
    {
        return new ArchiveContext
        {
            BuildKey = discovered.BuildKey,
            RelativePath = discovered.RelativePath,
            Roots = serializer.Read(stream),
            ArchiveLength = fileInfo.Length
        };
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            $"Failed to parse '{discovered.RelativePath}'. Run 'verify' first to find and exclude unparseable archives.", ex);
    }
}

using EvilHop.Corpus;
using EvilHop.Corpus.Invariants;
using EvilHop.Corpus.Output;
using System.Diagnostics;

try
{
    var options = CorpusOptions.Parse(args);
    return options.Verb switch
    {
        CorpusVerb.Verify => RunVerify(options),
        CorpusVerb.Inventory => RunInventory(options),
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
    var serializer = SerializerFactory.Create(options.SerializerId);

    int total = 0, failed = 0;
    foreach (var discovered in ArchiveWalker.Discover(options.Roots))
    {
        total++;
        try
        {
            using var stream = File.OpenRead(discovered.FullPath);
            serializer.Read(stream);
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
    var serializer = SerializerFactory.Create(options.SerializerId);
    var builder = new InventoryBuilder(InvariantRegistry.CreateAll());
    using var dump = options.DumpPath is not null ? new DumpWriter(options.DumpPath) : null;

    int processed = 0;
    foreach (var discovered in ArchiveWalker.Discover(options.Roots))
    {
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

static ArchiveContext Read(EvilHop.Serialization.SerializerV1 serializer, DiscoveredArchive discovered)
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

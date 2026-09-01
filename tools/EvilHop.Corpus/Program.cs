using EvilHop.Corpus;

if (args is not ["inventory", ..])
{
    Console.Error.WriteLine("Usage: evilhop-corpus inventory [--manifest <path>] [--cache <dir>]");
    return 1;
}

string manifestPath = OptionValue(args, "--manifest") ?? "corpus/manifest.json";
Manifest manifest = File.Exists(manifestPath) ? Manifest.Load(manifestPath) : Manifest.Empty;

if (manifest.Builds.Count == 0)
{
    Console.WriteLine($"No builds declared in '{manifestPath}'; nothing to generate.");
    return 0;
}

// Archive discovery, per-game serializer selection, and facet writing land as later build-order
// steps land their own facets - this wires the manifest up to that point and stops.
Console.WriteLine($"Loaded {manifest.Builds.Count} build(s) from '{manifestPath}'.");
return 0;

static string? OptionValue(string[] args, string name)
{
    int index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

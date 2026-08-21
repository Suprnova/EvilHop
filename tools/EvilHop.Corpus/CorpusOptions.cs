using EvilHop.Common;

namespace EvilHop.Corpus;

/// <summary>
/// The verb requested on the command line.
/// </summary>
internal enum CorpusVerb
{
    /// <summary>Parse every archive and emit a committed inventory.</summary>
    Inventory,

    /// <summary>Parse every archive and report failures, without emitting an inventory.</summary>
    Verify
}

/// <summary>
/// Parsed command-line arguments for <c>EvilHop.Corpus</c>.
/// </summary>
/// <remarks>
/// <code>
/// EvilHop.Corpus inventory --out corpus/n100f.json [--serializer &lt;game&gt;] [--dump &lt;path&gt;] &lt;root&gt;...
/// EvilHop.Corpus verify [--serializer &lt;game&gt;] &lt;root&gt;...
/// </code>
/// </remarks>
internal sealed class CorpusOptions
{
    /// <summary>The requested verb.</summary>
    public required CorpusVerb Verb { get; init; }

    /// <summary>One or more corpus roots to walk.</summary>
    public required IReadOnlyList<string> Roots { get; init; }

    /// <summary>The inventory output path. Required for <see cref="CorpusVerb.Inventory"/>.</summary>
    public string? OutputPath { get; init; }

    /// <summary>Which game to read archives with. Defaults to <see cref="GameVersion.N100F"/>, the only game with a serializer today.</summary>
    public GameVersion Game { get; init; } = GameVersion.N100F;

    /// <summary>The optional JSONL full-fidelity dump path.</summary>
    public string? DumpPath { get; init; }

    /// <summary>
    /// Parses <paramref name="args"/> into a <see cref="CorpusOptions"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the arguments are malformed or incomplete.</exception>
    public static CorpusOptions Parse(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Expected a verb: 'inventory' or 'verify'.");

        var verb = args[0] switch
        {
            "inventory" => CorpusVerb.Inventory,
            "verify" => CorpusVerb.Verify,
            var other => throw new ArgumentException($"Unknown verb '{other}'. Expected 'inventory' or 'verify'.")
        };

        string? output = null;
        var game = GameVersion.N100F;
        string? dump = null;
        List<string> roots = [];

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out":
                    output = RequireValue(args, ref i, "--out");
                    break;
                case "--serializer":
                    string key = RequireValue(args, ref i, "--serializer");
                    if (!Enum.TryParse(key, ignoreCase: true, out game))
                        throw new ArgumentException(
                            $"Unknown game '{key}'. Expected one of: {string.Join(", ", Enum.GetNames<GameVersion>())}.");
                    break;
                case "--dump":
                    dump = RequireValue(args, ref i, "--dump");
                    break;
                default:
                    roots.Add(args[i]);
                    break;
            }
        }

        if (roots.Count == 0)
            throw new ArgumentException("At least one <root> is required.");

        if (verb == CorpusVerb.Inventory && output == null)
            throw new ArgumentException("'inventory' requires --out.");

        return new CorpusOptions
        {
            Verb = verb,
            Roots = roots,
            OutputPath = output,
            Game = game,
            DumpPath = dump
        };
    }

    private static string RequireValue(string[] args, ref int index, string flag)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"'{flag}' requires a value.");
        return args[++index];
    }
}

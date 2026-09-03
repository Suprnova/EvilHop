#pragma warning disable
// corpusq.cs — query a committed EvilHop corpus inventory. See SKILL.md for usage and examples.
//
// Every subcommand prints a compact, line-oriented digest instead of raw JSON, so answering a
// question costs a few lines of context rather than the whole inventory. This script only reads
// the committed JSON — it never touches artifacts/, and it knows nothing about the HIP format.
//
// HUMAN WARNING: SLOP AHEAD! this was made solely for convenience and may not be the same quality
// as other files in this project. do not trust this script by default.
using System.Text.Json;
using System.Text.RegularExpressions;

const string Usage = """
corpusq — query a committed EvilHop corpus inventory without printing the whole file.

Usage:
  dotnet run --file corpusq.cs -- [options] <command> [args]

Commands:
  summary                 Builds, field counts, and invariant health. Start here.
  builds                  Build keys and their archive counts.
  fields [pattern]        One line per field, optionally filtered by a regex.
  field <key>             Full JSON for one field, e.g. AssetHeader.Type.
  values <key>            Observed values of one set-kind field, with counts.
  constants [pattern]     Fields with exactly one observed value corpus-wide.
  invariants [pattern]    One line of health per invariant.
  invariant <name>        Full JSON for one invariant, including violation samples.
  exemplar <key> <value>  An archive path containing that value, plus its spread.
  grep <pattern>          Search field names, recorded values, and invariant names.

Options:
  -i, --inventory <path>  Inventory to read. Default: corpus/n100f.json
  -n, --limit <n>         Cap printed lines; 0 for unlimited. Default: 60
""";

var arity = new Dictionary<string, (int Min, int Max)>
{
    ["summary"] = (0, 0),
    ["builds"] = (0, 0),
    ["fields"] = (0, 1),
    ["field"] = (1, 1),
    ["values"] = (1, 1),
    ["constants"] = (0, 1),
    ["invariants"] = (0, 1),
    ["invariant"] = (1, 1),
    ["exemplar"] = (2, 2),
    ["grep"] = (1, 1),
};

string inventoryPath = "corpus/n100f.json";
int limit = 60;
List<string> positional = [];

try
{
    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-h" or "--help":
                Console.WriteLine(Usage);
                return 0;
            case "-i" or "--inventory":
                inventoryPath = NextArg(args, ref i, args[i]);
                break;
            case "-n" or "--limit":
                string raw = NextArg(args, ref i, args[i]);
                if (!int.TryParse(raw, out limit) || limit < 0)
                    throw new UsageError($"--limit needs a non-negative integer, got '{raw}'.");
                break;
            default:
                positional.Add(args[i]);
                break;
        }
    }

    if (positional.Count == 0)
    {
        Console.WriteLine(Usage);
        return 0;
    }

    string command = positional[0];
    string[] rest = [.. positional.Skip(1)];

    if (!arity.TryGetValue(command, out var bounds))
        throw new UsageError($"unknown command '{command}'. Commands: {string.Join(' ', arity.Keys)}");
    if (rest.Length < bounds.Min || rest.Length > bounds.Max)
        throw new UsageError($"'{command}' takes {Describe(bounds)}, got {rest.Length}.");

    if (!File.Exists(inventoryPath))
        throw new UsageError($"inventory not found: '{inventoryPath}'. Generate one with the generating-corpus-inventory skill.");

    using var document = JsonDocument.Parse(File.ReadAllText(inventoryPath));
    var root = document.RootElement;
    foreach (var required in new[] { "builds", "fields", "invariants" })
        if (!root.TryGetProperty(required, out _))
            throw new UsageError($"'{inventoryPath}' has no '{required}' key — is it really a corpus inventory?");

    var inventory = new Inventory(
        root.GetProperty("builds"),
        root.GetProperty("fields"),
        root.GetProperty("invariants"),
        limit);

    switch (command)
    {
        case "summary": Summary(inventory); break;
        case "builds": Builds(inventory); break;
        case "fields": Fields(inventory, rest.FirstOrDefault()); break;
        case "field": Detail(inventory.Fields, rest[0], "field"); break;
        case "values": Values(inventory, rest[0]); break;
        case "constants": Constants(inventory, rest.FirstOrDefault()); break;
        case "invariants": Invariants(inventory, rest.FirstOrDefault()); break;
        case "invariant": Detail(inventory.InvariantsNode, rest[0], "invariant"); break;
        case "exemplar": Exemplar(inventory, rest[0], rest[1]); break;
        case "grep": Grep(inventory, rest[0]); break;
    }

    return 0;
}
catch (UsageError error)
{
    Console.Error.WriteLine($"error: {error.Message}");
    return 2;
}
catch (JsonException error)
{
    Console.Error.WriteLine($"error: inventory is not valid JSON: {error.Message}");
    return 1;
}

static string NextArg(string[] args, ref int i, string flag) =>
    ++i < args.Length ? args[i] : throw new UsageError($"'{flag}' requires a value.");

static string Describe((int Min, int Max) bounds) =>
    bounds.Min == bounds.Max ? $"{bounds.Min} argument(s)" : $"{bounds.Min}-{bounds.Max} arguments";

// ---------------------------------------------------------------- commands

void Summary(Inventory inventory)
{
    int archives = inventory.Builds.EnumerateArray().Sum(b => b.GetProperty("archives").GetInt32());
    Console.WriteLine($"builds      {inventory.Builds.GetArrayLength()}  ({archives} archives)");
    foreach (var build in inventory.Builds.EnumerateArray())
        Console.WriteLine($"  {build.GetProperty("key").GetString()}  {build.GetProperty("archives").GetInt32()}");

    var fields = inventory.Fields.EnumerateObject().ToList();
    int sets = fields.Count(f => f.Value.GetProperty("kind").GetString() == "set");
    Console.WriteLine($"fields      {fields.Count}  ({sets} set, {fields.Count - sets} summary)");

    var invariants = inventory.InvariantsNode.EnumerateObject().ToList();
    Console.WriteLine($"invariants  {invariants.Count}");

    var flagged = invariants.Where(i => Flag(i.Value) is not null)
        .Select(i => $"{i.Name} ({Flag(i.Value)})").ToList();
    Console.WriteLine(flagged.Count > 0
        ? $"  needs attention: {string.Join(", ", flagged)}"
        : "  no violations, no unexplained cases");
}

void Builds(Inventory inventory) => Emit(
    [.. inventory.Builds.EnumerateArray().Select(b => $"{b.GetProperty("key").GetString()}  {b.GetProperty("archives").GetInt32()}")],
    inventory.Limit);

void Fields(Inventory inventory, string? pattern) => Emit(
    [.. inventory.Fields.EnumerateObject().Where(f => Matches(f.Name, pattern)).Select(f => FieldLine(f.Name, f.Value))],
    inventory.Limit, "no field matched");

void Values(Inventory inventory, string key)
{
    if (!inventory.Fields.TryGetProperty(key, out var field))
        throw new UsageError(NotFound(inventory.Fields, key, "field"));

    if (field.GetProperty("kind").GetString() != "set")
        throw new UsageError($"'{key}' is a summary field — it blew the cardinality cap, so no value list was kept. "
            + $"Its range: {FieldLine(key, field)}");

    Emit([.. field.GetProperty("values").EnumerateObject()
        .Select(v => $"{Escape(v.Name)}\t{v.Value.GetProperty("count").GetInt64()}")], inventory.Limit);
}

void Constants(Inventory inventory, string? pattern) => Emit(
    [.. inventory.Fields.EnumerateObject()
        .Where(f => Matches(f.Name, pattern)
            && f.Value.GetProperty("kind").GetString() == "set"
            && f.Value.GetProperty("values").EnumerateObject().Count() == 1)
        .Select(f => $"{f.Name} = {f.Value.GetProperty("values").EnumerateObject().First().Name}")],
    inventory.Limit, "no single-valued fields");

void Invariants(Inventory inventory, string? pattern) => Emit(
    [.. inventory.InvariantsNode.EnumerateObject().Where(i => Matches(i.Name, pattern)).Select(i => InvariantLine(i.Name, i.Value))],
    inventory.Limit, "no invariant matched");

void Exemplar(Inventory inventory, string key, string value)
{
    if (!inventory.Fields.TryGetProperty(key, out var field) || field.GetProperty("kind").GetString() != "set")
        throw new UsageError($"'{key}' is not a set-kind field, so it records no exemplars.");
    if (!field.GetProperty("values").TryGetProperty(value, out var occurrence))
        throw new UsageError($"'{value}' was not observed for {key}. Known: "
            + string.Join(' ', field.GetProperty("values").EnumerateObject().Take(10).Select(v => v.Name)));

    Console.WriteLine(occurrence.GetProperty("exemplar").GetString());
    Console.WriteLine($"count={occurrence.GetProperty("count").GetInt64()}  builds="
        + string.Join(", ", occurrence.GetProperty("builds").EnumerateArray().Select(b => b.GetString())));
}

void Grep(Inventory inventory, string pattern)
{
    List<string> lines = [];
    foreach (var field in inventory.Fields.EnumerateObject())
    {
        if (Matches(field.Name, pattern))
        {
            lines.Add($"field      {FieldLine(field.Name, field.Value)}");
        }
        else if (field.Value.GetProperty("kind").GetString() == "set")
        {
            var hits = field.Value.GetProperty("values").EnumerateObject()
                .Where(v => Matches(v.Name, pattern)).Take(8).Select(v => v.Name).ToList();
            if (hits.Count > 0) lines.Add($"value      {field.Name}  ->  {string.Join(' ', hits)}");
        }
    }
    foreach (var invariant in inventory.InvariantsNode.EnumerateObject().Where(i => Matches(i.Name, pattern)))
        lines.Add($"invariant  {InvariantLine(invariant.Name, invariant.Value)}");

    Emit(lines, inventory.Limit, "no match");
}

void Detail(JsonElement container, string key, string label)
{
    if (!container.TryGetProperty(key, out var node))
        throw new UsageError(NotFound(container, key, label));

    // Utf8JsonWriter rather than JsonSerializer.Serialize: this runs as a trim/AOT-analyzed
    // file-based app, where the reflection-based serializer is a build error.
    using var buffer = new MemoryStream();
    using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        node.WriteTo(writer);
    Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer.ToArray()));
}

// ---------------------------------------------------------------- rendering

static string FieldLine(string key, JsonElement field)
{
    if (field.GetProperty("kind").GetString() == "set")
    {
        var values = field.GetProperty("values").EnumerateObject().Select(v => Escape(v.Name)).ToList();
        string preview = string.Join(' ', values.Take(8));
        string more = values.Count > 8 ? $" +{values.Count - 8} more" : "";
        return $"{key}  set[{values.Count}]  {preview}{more}";
    }

    List<string> parts = [];
    if (field.TryGetProperty("distinct", out var distinct)) parts.Add($"distinct={distinct.GetInt64()}");
    if (field.TryGetProperty("min", out var min)) parts.Add($"{Raw(min)}..{Raw(field.GetProperty("max"))}");
    if (field.TryGetProperty("minLength", out var minLength)) parts.Add($"len {minLength.GetInt32()}..{field.GetProperty("maxLength").GetInt32()}");
    return $"{key}  summary  {string.Join("  ", parts)}";
}

// Invariants come in three shapes: the usual checked/outcomes pair, the structural table keyed by
// block type, and a bare value summary for observations that are recorded but never pass or fail.
static string InvariantLine(string name, JsonElement node)
{
    if (name == "structural")
    {
        long checkedTotal = 0;
        List<string> violations = [];
        foreach (var block in node.EnumerateObject())
        {
            checkedTotal += block.Value.GetProperty("checked").GetInt64();
            long violated = block.Value.GetProperty("outcomes").GetProperty("violated").GetInt64();
            if (violated > 0) violations.Add($"{block.Name}={violated}");
        }
        return $"structural  checked={checkedTotal}  {(violations.Count > 0 ? string.Join("  ", violations) : "all satisfied")}";
    }

    if (!node.TryGetProperty("outcomes", out var outcomes))
        return $"{name}  (observation, not pass/fail)  {FieldLine("", node).Trim()}";

    string rendered = string.Join("  ", outcomes.EnumerateObject()
        .Where(o => o.Value.GetInt64() > 0).Select(o => $"{o.Name}={o.Value.GetInt64()}"));
    string flag = Flag(node) is { } reason ? $"  <-- {reason.ToUpperInvariant()}" : "";

    // Some invariants carry an extra value summary alongside their pass/fail counts (paddingIsHomogeneous
    // records which fill byte it saw). That observation is the actual finding, so surface it inline.
    string extra = "";
    foreach (var property in node.EnumerateObject())
        if (property.Name is not ("checked" or "outcomes" or "violated" or "unexplained")
            && property.Value.ValueKind == JsonValueKind.Object
            && property.Value.TryGetProperty("kind", out _))
            extra += $"  {property.Name}={FieldLine("", property.Value).Trim()}";

    return $"{name}  checked={node.GetProperty("checked").GetInt64()}  {rendered}{extra}{flag}";
}

// Why an invariant deserves a second look, or null when it is clean.
static string? Flag(JsonElement node)
{
    if (!node.TryGetProperty("outcomes", out var outcomes))
    {
        // Either the structural table, whose values are themselves invariant results, or a bare
        // value summary, which records an observation and can never fail.
        foreach (var child in node.EnumerateObject())
            if (child.Value.ValueKind == JsonValueKind.Object
                && child.Value.TryGetProperty("outcomes", out var childOutcomes)
                && childOutcomes.TryGetProperty("violated", out var childViolated)
                && childViolated.GetInt64() > 0)
                return "violated";
        return null;
    }

    if (outcomes.TryGetProperty("violated", out var violated) && violated.GetInt64() > 0) return "violated";
    if (outcomes.TryGetProperty("unexplained", out var unexplained) && unexplained.GetInt64() > 0) return "unexplained";
    return null;
}

static string Raw(JsonElement value) => value.ValueKind == JsonValueKind.String ? Escape(value.GetString()!) : value.GetRawText();

// Observed values can be or contain control characters — PCRT's trailing terminator is a bare "\n",
// which would silently split a line-oriented digest in two and render the value invisible.
static string Escape(string value) => value
    .Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");

static bool Matches(string name, string? pattern) =>
    pattern is null || Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase);

static string NotFound(JsonElement container, string key, string label)
{
    var near = container.EnumerateObject()
        .Where(p => p.Name.Contains(key, StringComparison.OrdinalIgnoreCase)).Take(5).Select(p => p.Name).ToList();
    return $"no {label} '{key}'." + (near.Count > 0 ? $" Did you mean: {string.Join(", ", near)}?" : $" List them with the '{label}s' command.");
}

static void Emit(List<string> lines, int limit, string emptyMessage = "nothing to show")
{
    if (lines.Count == 0)
    {
        Console.WriteLine($"({emptyMessage})");
        return;
    }

    foreach (var line in limit > 0 ? lines.Take(limit) : lines)
        Console.WriteLine(line);

    if (limit > 0 && lines.Count > limit)
        Console.WriteLine($"  … truncated ({lines.Count - limit} more), raise with --limit");
}

sealed class UsageError(string message) : Exception(message);

sealed record Inventory(JsonElement Builds, JsonElement Fields, JsonElement InvariantsNode, int Limit);

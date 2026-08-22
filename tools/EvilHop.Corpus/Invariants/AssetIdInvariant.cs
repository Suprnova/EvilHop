using EvilHop.Blocks;
using EvilHop.Common;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary>
/// <c>AHDR.Id</c> derives from <see cref="AssetDebug.Name"/> via <see cref="BKDRHash"/>, but not
/// always directly - some names were transformed before hashing, and some were truncated before
/// being stored. The outcome is classified rather than boolean, because a boolean check would
/// report thousands of entirely expected non-matches as violations.
/// </summary>
internal sealed class AssetIdMatchesNameHashInvariant : IInvariant
{
    private const int UnexplainedSampleCap = 50;

    /// <inheritdoc/>
    public string Name => "assetIdMatchesNameHash";

    private long _checked;
    private readonly Dictionary<string, long> _outcomes = [];
    private readonly List<JsonObject> _unexplainedSamples = [];

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var header in archive.AllBlocks.OfType<AssetHeader>())
        {
            var debug = header.GetChild<AssetDebug>();
            if (debug is null) continue;

            _checked++;
            string name = debug.Name;
            uint id = header.Id;

            string? rule = FindMatchingRule(name, header.Type, id);
            if (rule is not null)
            {
                RecordOutcome(rule);
                continue;
            }

            if (name.Length == 31)
            {
                RecordOutcome("truncated");
                continue;
            }

            RecordOutcome("unexplained");
            RecordUnexplained(archive.RelativePath, name, id, header.Type);
        }
    }

    /// <summary>
    /// Builds each candidate name for <paramref name="type"/>, in the order tried, and returns the
    /// rule name of the first one whose hash matches <paramref name="id"/>, or <see langword="null"/>.
    /// </summary>
    private static string? FindMatchingRule(string name, AssetType type, uint id)
    {
        if (BKDRHash.Calculate(name) == id) return "direct";

        if (type == AssetType.Animation)
        {
            if (BKDRHash.Calculate(Path.ChangeExtension(name, ".anm")) == id) return "anim-suffix";
        }

        if (type == AssetType.DestructibleAsset)
        {
            if (BKDRHash.Calculate(name + ".dff_destruct") == id) return "dff-destruct";
        }

        if (type == AssetType.MorphTarget)
        {
            if (BKDRHash.Calculate(Path.ChangeExtension(name, ".mph")) == id) return "mpht-replace";
            if (BKDRHash.Calculate(name + ".mph") == id) return "mpht-append";
        }

        return null;
    }

    private void RecordOutcome(string rule) => _outcomes[rule] = _outcomes.GetValueOrDefault(rule) + 1;

    private void RecordUnexplained(string path, string name, uint id, AssetType type)
    {
        if (_unexplainedSamples.Count < UnexplainedSampleCap)
        {
            _unexplainedSamples.Add(new JsonObject
            {
                ["path"] = path,
                ["name"] = name,
                ["expected"] = FormatHex(id),
                ["actual"] = FormatHex(BKDRHash.Calculate(name)),
                ["type"] = type.ToString()
            });
        }
    }

    private static string FormatHex(uint value) => $"0x{value:X8}";

    /// <inheritdoc/>
    public JsonObject ToJson()
    {
        var outcomes = new JsonObject();
        foreach (var (key, count) in _outcomes.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            outcomes[key] = count;

        return new JsonObject
        {
            ["checked"] = _checked,
            ["outcomes"] = outcomes,
            ["unexplained"] = new JsonArray([.. _unexplainedSamples])
        };
    }
}

/// <summary>No duplicate <see cref="AssetHeader.Id"/> exists within a single archive.</summary>
internal sealed class AssetIdsUniqueInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "assetIdsUnique";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        var seen = new HashSet<uint>();
        foreach (var header in archive.AllBlocks.OfType<AssetHeader>())
        {
            bool isUnique = seen.Add(header.Id);
            _result.Record(isUnique, () => new JsonObject { ["path"] = archive.RelativePath, ["id"] = $"0x{header.Id:X8}" });
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

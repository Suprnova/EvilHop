using EvilHop.Blocks;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary>How many instances of a declared child type a parent block is expected to have.</summary>
internal enum ChildMultiplicity { Exactly, Optional, Many }

/// <summary>One declared child type and its expected multiplicity within a parent block.</summary>
internal sealed record ChildRule(Type ChildType, ChildMultiplicity Multiplicity)
{
    public static ChildRule Exactly<T>() where T : Block => new(typeof(T), ChildMultiplicity.Exactly);
    public static ChildRule Optional<T>() where T : Block => new(typeof(T), ChildMultiplicity.Optional);
    public static ChildRule Many<T>() where T : Block => new(typeof(T), ChildMultiplicity.Many);

    public bool IsSatisfiedBy(int count) => Multiplicity switch
    {
        ChildMultiplicity.Exactly => count == 1,
        ChildMultiplicity.Optional => count is 0 or 1,
        ChildMultiplicity.Many => true,
        _ => throw new UnreachableException()
    };
}

/// <summary>
/// Every "No children" / "Required X child" rule from the audit is the same shape - a generic
/// checker over one declarative table covers all of them.
/// </summary>
internal sealed class StructuralInvariant : IInvariant
{
    /// <summary>
    /// Declares, per parent block type, every child type it may contain and that child's expected
    /// multiplicity. A block type with an empty array declares "no children". A block type absent
    /// from the table is not structurally checked.
    /// </summary>
    private static readonly Dictionary<Type, ChildRule[]> Declarations = new()
    {
        [typeof(HIPA)] = [],

        [typeof(Package)] =
        [
            ChildRule.Exactly<PackageVersion>(), ChildRule.Exactly<PackageFlags>(), ChildRule.Exactly<PackageCount>(),
            ChildRule.Exactly<PackageCreated>(), ChildRule.Exactly<PackageModified>(), ChildRule.Optional<PackagePlatform>()
        ],
        [typeof(PackageVersion)] = [],
        [typeof(PackageFlags)] = [],
        [typeof(PackageCount)] = [],
        [typeof(PackageCreated)] = [],
        [typeof(PackageModified)] = [],
        [typeof(PackagePlatform)] = [],

        [typeof(Dictionary)] = [ChildRule.Exactly<AssetTable>(), ChildRule.Exactly<LayerTable>()],
        [typeof(AssetTable)] = [ChildRule.Exactly<AssetInf>(), ChildRule.Many<AssetHeader>()],
        [typeof(AssetInf)] = [],
        [typeof(AssetHeader)] = [ChildRule.Exactly<AssetDebug>()],
        [typeof(AssetDebug)] = [],
        [typeof(LayerTable)] = [ChildRule.Exactly<LayerInf>(), ChildRule.Many<LayerHeader>()],
        [typeof(LayerInf)] = [],
        [typeof(LayerHeader)] = [ChildRule.Exactly<LayerDebug>()],
        [typeof(LayerDebug)] = [],

        [typeof(AssetStream)] = [ChildRule.Exactly<StreamHeader>(), ChildRule.Exactly<StreamData>()],
        [typeof(StreamHeader)] = [],
        [typeof(StreamData)] = []
    };

    /// <inheritdoc/>
    public string Name => "structural";

    private readonly Dictionary<string, InvariantResult> _results = [];

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var block in archive.AllBlocks)
        {
            if (Declarations.TryGetValue(block.GetType(), out var rules))
                CheckBlock(block, rules, archive);
        }
    }

    private void CheckBlock(Block block, ChildRule[] rules, ArchiveContext archive)
    {
        string blockName = block.GetType().Name;
        if (!_results.TryGetValue(blockName, out var result))
            _results[blockName] = result = new InvariantResult();

        var counts = rules.ToDictionary(rule => rule.ChildType, rule => block.Children.Count(c => c.GetType() == rule.ChildType));
        bool satisfied = counts.Values.Sum() == block.Children.Count && rules.All(rule => rule.IsSatisfiedBy(counts[rule.ChildType]));

        result.Record(satisfied, () => new JsonObject
        {
            ["path"] = archive.RelativePath,
            ["children"] = new JsonObject(counts
                .OrderBy(kv => kv.Key.Name, StringComparer.Ordinal)
                .Select(kv => KeyValuePair.Create(kv.Key.Name, (JsonNode?)kv.Value)))
        });
    }

    /// <inheritdoc/>
    public JsonObject ToJson()
    {
        var result = new JsonObject();
        foreach (var (blockName, blockResult) in _results.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            result[blockName] = blockResult.ToJson();
        return result;
    }
}

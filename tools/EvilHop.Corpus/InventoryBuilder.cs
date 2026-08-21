using EvilHop.Blocks;
using EvilHop.Corpus.Extraction;
using EvilHop.Corpus.Invariants;

namespace EvilHop.Corpus;

/// <summary>
/// Owns every accumulator that persists across the corpus: per-build archive counts, per-field
/// value accumulators, and invariant results. Archives are folded in one at a time and discarded.
/// </summary>
internal sealed class InventoryBuilder(IReadOnlyList<IInvariant> invariants)
{
    private readonly Dictionary<string, long> _builds = [];
    private readonly Dictionary<string, FieldAccumulator> _fields = [];

    /// <summary>Per-build archive counts.</summary>
    public IReadOnlyDictionary<string, long> Builds => _builds;

    /// <summary>Per-field value accumulators, keyed by <c>"{BlockType}.{Property}"</c>.</summary>
    public IReadOnlyDictionary<string, FieldAccumulator> Fields => _fields;

    /// <summary>The invariants checked over every archive.</summary>
    public IReadOnlyList<IInvariant> Invariants => invariants;

    /// <summary>
    /// Folds one archive into every accumulator and invariant.
    /// </summary>
    public void Observe(ArchiveContext archive)
    {
        _builds[archive.BuildKey] = _builds.GetValueOrDefault(archive.BuildKey) + 1;

        foreach (var block in archive.AllBlocks)
            ObserveFields(block, archive);

        foreach (var invariant in invariants)
            invariant.Check(archive);
    }

    private void ObserveFields(Block block, ArchiveContext archive)
    {
        var blockType = block.GetType();
        foreach (var property in FieldExtractor.GetFields(blockType))
        {
            if (!FieldExtractor.TryGetValue(property, block, out var value)) continue;

            string fieldKey = $"{blockType.Name}.{property.Name}";
            if (!_fields.TryGetValue(fieldKey, out var accumulator))
                _fields[fieldKey] = accumulator = new FieldAccumulator(FieldKindClassifier.Classify(property.PropertyType));

            accumulator.Record(value, archive.BuildKey, archive.RelativePath);
        }
    }
}

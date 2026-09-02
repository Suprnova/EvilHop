using EvilHop.Blocks;
using EvilHop.Validation;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Generation;

/// <summary>
/// Maps every archive- and block-scoped field-value <see cref="Observable"/> to the values observed
/// in one archive, and reduces every covered archive's contribution into the <c>blockFields</c>
/// facet's <c>ValueSet</c>s.
/// </summary>
public sealed class BlockFieldsFacetGenerator : IFacetGenerator
{
    /// <inheritdoc/>
    public string Id => "blockFields";

    /// <inheritdoc/>
    public int Revision => 3;

    /// <inheritdoc/>
    public IEnumerable<string> Dependencies => FieldValueObservables.Select(o => o.Id);

    private static IEnumerable<Observable> FieldValueObservables =>
        ValidationCatalogue.Instance.Observables.Where(o => o.Scope == ObservableScope.Block && o.Kind == ObservableKind.FieldValue);

    /// <inheritdoc/>
    public JsonObject Map(Archive archive)
    {
        var record = new JsonObject();
        var fieldValueIds = FieldValueObservables.Select(o => o.Id).ToHashSet();

        foreach (var block in Descendants(archive))
            foreach (var observation in ValidationCatalogue.Instance.Observe(block))
                if (fieldValueIds.Contains(observation.ObservableId))
                    ObservationValueSets.Append(record, observation);

        return record;
    }

    /// <inheritdoc/>
    public JsonObject Reduce(IReadOnlyList<MappedArchive> records)
    {
        // Ordered explicitly rather than relying on the catalogue's own (reflection-driven, not
        // otherwise stable) enumeration order - this is what keeps regenerating an unchanged facet
        // byte-identical.
        var observations = new JsonObject();
        foreach (var observable in FieldValueObservables.OrderBy(o => o.Id, StringComparer.Ordinal))
            observations[observable.Id] = ObservationValueSets.Reduce(observable, records);
        return observations;
    }

    private static IEnumerable<Block> Descendants(Archive archive) => archive.Roots.SelectMany(Descendants);

    private static IEnumerable<Block> Descendants(Block block) => new[] { block }.Concat(block.Children.SelectMany(Descendants));
}

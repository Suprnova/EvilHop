using EvilHop.Validation;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Generation;

/// <summary>
/// Maps every asset-scoped field-value <see cref="Observable"/> to the values observed in one
/// archive's assets, and reduces every covered archive's contribution into the <c>assetFields</c>
/// facet - a <c>ValueSet</c> per ungrouped observable, and a container of one <c>ValueSet</c> per
/// asset type for a grouped one.
/// </summary>
/// <remarks>
/// Unlike the block-scoped facets, this one opens an <see cref="EvilHop.Assets.AssetSession"/>,
/// which is both the expensive step and a mutating one - committing rebuilds the blocks the other
/// facets read - so the pipeline maps this facet after them, against the same loaded archive.
/// </remarks>
public sealed class AssetFieldsFacetGenerator : IFacetGenerator
{
    /// <inheritdoc/>
    public string Id => "assetFields";

    /// <inheritdoc/>
    public int Revision => 1;

    /// <inheritdoc/>
    public MapStage Stage => MapStage.Assets;

    /// <inheritdoc/>
    /// <remarks>
    /// Depends on the asset codec registry as well as on its observables: which groups exist, and
    /// what their values are, follow from what an asset parses into, so registering a codec or
    /// moving a type's shape has to invalidate this facet even though no observable's declaration
    /// changed. It deliberately does <em>not</em> depend on <c>AssetType</c> - group keys are raw
    /// uints, so naming a new member changes nothing that was recorded.
    /// </remarks>
    public IEnumerable<string> Dependencies =>
        [.. AssetObservables.Select(o => o.Id), ValidationCatalogue.AssetCodecsKey];

    private static IEnumerable<Observable> AssetObservables =>
        ValidationCatalogue.Instance.Observables.Where(o => o.Scope == ObservableScope.Asset && o.Kind == ObservableKind.FieldValue);

    /// <inheritdoc/>
    public JsonObject Map(Archive archive)
    {
        var record = new JsonObject();
        var assetFieldIds = AssetObservables.Select(o => o.Id).ToHashSet();

        using var session = archive.OpenAssets();
        foreach (var asset in session.Layers.SelectMany(layer => layer.Assets))
            foreach (var observation in ValidationCatalogue.Instance.Observe(asset))
                if (assetFieldIds.Contains(observation.ObservableId))
                    ObservationValueSets.Append(record, observation);

        return record;
    }

    /// <inheritdoc/>
    public JsonObject Reduce(IReadOnlyList<MappedArchive> records)
    {
        // Ordered explicitly, same reasoning as blockFields: keeps an unchanged facet's regeneration
        // byte-identical regardless of the catalogue's own reflection-driven enumeration order.
        var observations = new JsonObject();
        foreach (var observable in AssetObservables.OrderBy(o => o.Id, StringComparer.Ordinal))
            observations[observable.Id] = ObservationValueSets.Reduce(observable, records);
        return observations;
    }
}

using EvilHop.Blocks;
using EvilHop.Validation;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Generation;

/// <summary>
/// Maps every archive's block-tree shape - its root tag sequence, each <see cref="Package"/>'s child
/// tag set, and every structural <see cref="Observable"/> (required-child multiplicity,
/// leaf-childlessness) - and reduces every covered archive's contribution into the <c>structure</c>
/// facet's <c>ValueSet</c>s.
/// </summary>
public sealed class StructureFacetGenerator : IFacetGenerator
{
    /// <summary>The root tag sequence's recorded ID, archive-scoped rather than block-scoped.</summary>
    public const string RootSequenceId = "archive.rootSequence";

    /// <summary>Each <see cref="Package"/>'s child tag set's recorded ID.</summary>
    public const string PackChildrenId = "PACK.children";

    /// <inheritdoc/>
    public string Id => "structure";

    /// <inheritdoc/>
    public int Revision => 1;

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="RootSequenceId"/> and <see cref="PackChildrenId"/> aren't catalogue-declared - no
    /// rule reads either - so they can't be fingerprinted through <see cref="Dependencies"/>; a
    /// change to how they're computed only shows up by hand-bumping <see cref="Revision"/>.
    /// </remarks>
    public IEnumerable<string> Dependencies => StructuralObservables.Select(o => o.Id);

    private static IEnumerable<Observable> StructuralObservables =>
        ValidationCatalogue.Instance.Observables.Where(o => o.Kind == ObservableKind.Structural);

    /// <inheritdoc/>
    public JsonObject Map(Archive archive)
    {
        var record = new JsonObject
        {
            [RootSequenceId] = new JsonArray { ObservationValueSets.ToJsonValue(RootSequence(archive)) }
        };

        var packChildren = new JsonArray();
        foreach (string children in PackChildSets(archive))
            packChildren.Add(ObservationValueSets.ToJsonValue(children));
        if (packChildren.Count > 0) record[PackChildrenId] = packChildren;

        var structuralIds = StructuralObservables.Select(o => o.Id).ToHashSet();
        foreach (var block in Descendants(archive))
            foreach (var observation in ValidationCatalogue.Instance.Observe(block))
                if (structuralIds.Contains(observation.ObservableId))
                    ObservationValueSets.Append(record, observation);

        return record;
    }

    /// <inheritdoc/>
    public JsonObject Reduce(IReadOnlyList<MappedArchive> records)
    {
        var shapes = new List<(string Id, ObservableCardinality Cardinality, ObservablePresentation Presentation)>
        {
            (RootSequenceId, ObservableCardinality.Enumerated, ObservablePresentation.Text),
            (PackChildrenId, ObservableCardinality.Enumerated, ObservablePresentation.Text)
        };
        shapes.AddRange(StructuralObservables.Select(o => (o.Id, o.Cardinality, o.Presentation)));

        // Ordered explicitly, same reasoning as blockFields: keeps an unchanged facet's regeneration
        // byte-identical regardless of the catalogue's own reflection-driven enumeration order.
        var observations = new JsonObject();
        foreach (var shape in shapes.OrderBy(s => s.Id, StringComparer.Ordinal))
            observations[shape.Id] = ObservationValueSets.Reduce(shape.Id, shape.Cardinality, shape.Presentation, records);
        return observations;
    }

    private static string RootSequence(Archive archive) => string.Join(",", archive.Roots.Select(root => root.Tag));

    private static IEnumerable<string> PackChildSets(Archive archive) =>
        Descendants(archive).OfType<Package>()
            .Select(package => string.Join(",", package.Children.Select(child => child.Tag).OrderBy(t => t, StringComparer.Ordinal)));

    private static IEnumerable<Block> Descendants(Archive archive) => archive.Roots.SelectMany(Descendants);

    private static IEnumerable<Block> Descendants(Block block) => new[] { block }.Concat(block.Children.SelectMany(Descendants));
}

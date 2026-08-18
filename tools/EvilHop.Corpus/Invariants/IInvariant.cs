using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary>
/// A hand-written check encoding domain knowledge reflection cannot infer - a relationship between
/// fields, or across blocks, checked over every archive in the corpus.
/// </summary>
internal interface IInvariant
{
    /// <summary>The key this invariant's result is recorded under in <c>inventory.invariants</c>.</summary>
    string Name { get; }

    /// <summary>Checks <paramref name="archive"/>, folding the outcome into this invariant's running state.</summary>
    void Check(ArchiveContext archive);

    /// <summary>Produces the final JSON for this invariant. Call once, after every archive has been checked.</summary>
    JsonObject ToJson();
}

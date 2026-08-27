using EvilHop.Blocks;

namespace EvilHop.Corpus.Archives;

/// <summary>
/// Recursive traversal helpers over the public <see cref="Block.Children"/> API. Every extractor
/// and invariant walks the tree through these, rather than each writing its own recursion.
/// </summary>
internal static class BlockTreeExtensions
{
    /// <summary>
    /// Yields <paramref name="block"/> followed by every descendant, depth-first.
    /// </summary>
    public static IEnumerable<Block> SelfAndDescendants(this Block block)
    {
        yield return block;
        foreach (var child in block.Children)
            foreach (var descendant in child.SelfAndDescendants())
                yield return descendant;
    }

    /// <summary>
    /// Yields every block reachable from <paramref name="roots"/>, including the roots themselves.
    /// </summary>
    public static IEnumerable<Block> AllBlocks(this IEnumerable<Block> roots) =>
        roots.SelectMany(root => root.SelfAndDescendants());
}

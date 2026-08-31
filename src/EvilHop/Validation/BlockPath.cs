using EvilHop.Blocks;

namespace EvilHop.Validation;

/// <summary>
/// A sequence of <c>(tag, ordinal)</c> pairs locating a <see cref="Block"/> within an archive's
/// block tree.
/// </summary>
/// <param name="Segments">
/// The path's segments, from the root block down to the located block. Each segment's ordinal is
/// its index among same-tagged siblings; the first of a tag is omitted from
/// <see cref="ToString"/>, later ones are suffixed with <c>[ordinal]</c>.
/// </param>
public readonly record struct BlockPath(IReadOnlyList<(string Tag, int Ordinal)> Segments)
{
    /// <summary>
    /// Builds the <see cref="BlockPath"/> locating <paramref name="block"/>, from its outermost
    /// ancestor down to itself.
    /// </summary>
    /// <param name="block">The <see cref="Block"/> to locate.</param>
    /// <returns>The <see cref="BlockPath"/> for <paramref name="block"/>.</returns>
    public static BlockPath For(Block block)
    {
        var segments = new List<(string Tag, int Ordinal)>();

        for (var current = block; current != null; current = current.Parent)
        {
            IReadOnlyList<Block> siblings = current.Parent != null ? current.Parent.Children : [current];
            int ordinal = siblings.Where(sibling => sibling.Tag == current.Tag).ToList().IndexOf(current);
            segments.Insert(0, (current.Tag, ordinal));
        }

        return new BlockPath(segments);
    }

    /// <inheritdoc/>
    public bool Equals(BlockPath other) => Segments.SequenceEqual(other.Segments);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var segment in Segments) hash.Add(segment);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() =>
        string.Join('/', Segments.Select(s => s.Ordinal > 0 ? $"{s.Tag}[{s.Ordinal}]" : s.Tag));
}

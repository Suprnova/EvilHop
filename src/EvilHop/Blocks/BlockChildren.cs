using System.Collections;

namespace EvilHop.Blocks;

/// <summary>
/// Represents a collection of child <see cref="Block"/> objects.
/// </summary>
/// <remarks>
/// This class is used to enforce structural invariants with regards to the
/// parent-child relationships between <see cref="Block"/> objects.
/// It is primarily intended to enforce the following rules:
/// <list type="bullet">
/// <item>Each child <see cref="Block"/> may only have one parent <see cref="Block"/>.</item>
/// <item>Cycles in the parent-child graph are not allowed.</item>
/// </list>
/// </remarks>
public class BlockChildren(Block parent) : IReadOnlyList<Block>
{
    private readonly Block parent = parent;
    private readonly List<Block> children = [];

    /// <inheritdoc/>
    public Block this[int index] => children[index];

    /// <inheritdoc/>
    public int Count => children.Count;

    /// <inheritdoc/>
    public IEnumerator<Block> GetEnumerator() => children.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Adds a child <see cref="Block"/> at the end of the collection.
    /// </summary>
    /// <param name="child">The <see cref="Block"/> to add.</param>
    public void Add(Block child)
    {
        ArgumentNullException.ThrowIfNull(child);
        ThrowIfAncestorOf(child, parent);
        child.Parent = parent;
        children.Add(child);
    }

    /// <summary>
    /// Inserts a child <see cref="Block"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the child <see cref="Block"/>.</param>
    /// <param name="child">The <see cref="Block"/> to insert.</param>
    public void Insert(int index, Block child)
    {
        ArgumentNullException.ThrowIfNull(child);
        ThrowIfAncestorOf(child, parent);
        child.Parent = parent;
        children.Insert(index, child);
    }

    /// <summary>
    /// Removes the first occurrence of a specific child <see cref="Block"/>.
    /// </summary>
    /// <param name="child">The <see cref="Block"/> to remove.</param>
    /// <returns><see langword="true"/> if <paramref name="child"/> was found and removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(Block child)
    {
        ArgumentNullException.ThrowIfNull(child);
        bool removed = children.Remove(child);
        if (removed) child.Parent = null;
        return removed;
    }

    /// <summary>
    /// Removes the child <see cref="Block"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the child <see cref="Block"/> to remove.</param>
    public void RemoveAt(int index)
    {
        var child = children[index];
        children.RemoveAt(index);
        child.Parent = null;
    }

    /// <summary>
    /// Determines whether a child <see cref="Block"/> is in the collection.
    /// </summary>
    /// <param name="child">The <see cref="Block"/> to find.</param>
    /// <returns><see langword="true"/> if <paramref name="child"/> is found; otherwise <see langword="false"/>.</returns>
    public bool Contains(Block child) => children.Contains(child);

    /// <summary>
    /// Removes all child <see cref="Block"/> objects from the collection.
    /// </summary>
    public void Clear() => children.ToList().ForEach(child => Remove(child));

    /// <summary>
    /// Searches for the specified child <see cref="Block"/> and returns the zero-based index of
    /// the first occurrence within the collection.
    /// </summary>
    /// <param name="child">The <see cref="Block"/> to find.</param>
    /// <returns>The zero-based index of the first occurrence of <paramref name="child"/>, if found; otherwise -1.</returns>
    public int IndexOf(Block child) => children.IndexOf(child);

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the specified <paramref name="potentialAncestor"/>
    /// is an ancestor of the <paramref name="potentialDescendant"/>.
    /// </summary>
    /// <param name="potentialAncestor">The <see cref="Block"/> to check as an ancestor.</param>
    /// <param name="potentialDescendant">The <see cref="Block"/> to check as a descendant.</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="potentialAncestor"/> is an ancestor of <paramref name="potentialDescendant"/>.</exception>
    private static void ThrowIfAncestorOf(Block potentialAncestor, Block potentialDescendant)
    {
        if (IsAncestorOf(potentialAncestor, potentialDescendant))
        {
            throw new InvalidOperationException("Adding a child that is an ancestor of this block would create a cycle.");
        }
    }

    /// <summary>
    /// Determines whether the specified <paramref name="potentialAncestor"/> is an ancestor of the
    /// <paramref name="potentialDescendant"/> in the parent-child hierarchy.
    /// </summary>
    /// <param name="potentialAncestor">The <see cref="Block"/> to check as an ancestor.</param>
    /// <param name="potentialDescendant">The <see cref="Block"/> to check as a descendant.</param>
    /// <returns><see langword="true"/> if <paramref name="potentialAncestor"/> is an ancestor of <paramref name="potentialDescendant"/>; otherwise <see langword="false"/>.</returns>
    private static bool IsAncestorOf(Block potentialAncestor, Block potentialDescendant)
    {
        if (potentialAncestor == potentialDescendant) return true;

        var current = potentialDescendant.Parent;
        while (current != null)
        {
            if (current == potentialAncestor)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }
}

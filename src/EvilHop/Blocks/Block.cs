using System.Text;
using EvilHop.Extensions;

namespace EvilHop.Blocks;

public abstract class Block
{
    /// <summary>
    /// A 4-character <see cref="string"/> that identifies the type of the block.
    /// </summary>
    protected abstract string Id { get; }

    /// <summary>
    /// The length of this block's body, including its data and its children blocks.
    /// </summary>
    public virtual uint Length => DataLength + ChildrenLength;

    /// <summary>
    /// The length of this block, including its header, data, and children blocks.
    /// </summary>
    protected virtual uint StoredLength => HeaderLength + DataLength + ChildrenLength;

    /// <summary>
    /// The length of this block's header, containing its ID and its length.
    /// </summary>
    protected virtual uint HeaderLength => 8;

    /// <summary>
    /// The length of this block's data.
    /// </summary>
    protected abstract uint DataLength { get; }

    /// <summary>
    /// The length of all of this block's children.
    /// </summary>
    protected uint ChildrenLength => (uint)Children.Sum(c => c.StoredLength);

    /// <summary>
    /// The list containing all child blocks to this block.
    /// </summary>
    public List<Block> Children { get; } = [];

    /// <summary>
    /// Searches for a <see cref="Block"/> of the specified type and returns the first occurrence from this <see cref="Block"/>'s children.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Block"/> to be found.</typeparam>
    /// <returns>The first occurrence of a <see cref="Block"/> of type <typeparamref name="T"/>, if found; otherwise, <see langword="null"/>.</returns>
    protected T? GetChild<T>() where T : Block
    {
        return Children.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Searches for a <see cref="Block"/> of the specified type and returns the first occurrence from this <see cref="Block"/>'s children.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Block"/> to be found.</typeparam>
    /// <returns>The first occurrence of a <see cref="Block"/> of type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown when a block of type <typeparamref name="T"/> is not present in <see cref="Block.Children"/>.</exception>
    protected T GetRequiredChild<T>() where T : Block
    {
        var childBlock = GetChild<T>();
        return childBlock ??
            throw new InvalidDataException($"Child block of type {typeof(T).Name} is not present in block {this.GetType().Name}.");
    }

    /// <summary>
    /// Replaces the <see cref="Block"/> of the specified type within this <see cref="Block"/>'s children with the provided <paramref name="value"/>.
    /// If <paramref name="value"/> is <see langword="null"/>, removes the <see cref="Block"/> instead.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Block"/> to replace.</typeparam>
    /// <param name="value">The new <see cref="Block"/>.</param>
    protected void SetChild<T>(T? value) where T : Block
    {
        // todo: consider adding a child if it doesn't exist?
        var index = Children.IndexOf(GetRequiredChild<T>());

        if (value == null) Children.RemoveAt(index);
        else Children[index] = value;
    }
}

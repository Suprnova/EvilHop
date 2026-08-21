namespace EvilHop.Blocks;

/// <summary>
/// The abstract base class from which all block types derive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#Structure">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class Block
{
    /// <summary>
    /// The 4-byte ASCII identifier for this block type.
    /// </summary>
    protected internal abstract string Tag { get; }

    /// <summary>
    /// The parent block of this block, if any. This property is null for top-level blocks.
    /// </summary>
    public Block? Parent { get; internal set; }

    /// <summary>
    /// The collection of <see cref="Block"/> children to this parent.
    /// </summary>
    public BlockChildren Children { get; }

    internal Block()
    {
        Children = new BlockChildren(this);
    }

    /// <summary>
    /// Searches this <see cref="Block"/>'s immediate children for a child of the specified type
    /// <typeparamref name="T"/>. If found, returns the first instance of the child. If not found,
    /// throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Block"/> to be found.</typeparam>
    /// <returns>The first occurrence of a <see cref="Block"/> of type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a <see cref="Block"/> of type <typeparamref name="T"/> is not found.</exception>
    public T GetRequiredChild<T>() where T : Block =>
        GetChild<T>() ?? throw new InvalidOperationException($"Required child of type {typeof(T).Name} not found.");

    /// <summary>
    /// Searches this <see cref="Block"/>'s immediate children for a child of the specified type
    /// <typeparamref name="T"/>. If found, returns the first instance of the child.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Block"/> to be found.</typeparam>
    /// <returns>The first occurrence of a <see cref="Block"/> of type <typeparamref name="T"/>.</returns>
    public T? GetChild<T>() where T : Block => Children.OfType<T>().FirstOrDefault();

    /// <summary>
    /// Searches this <see cref="Block"/>'s immediate children for all children of the specified type
    /// <typeparamref name="T"/>. Returns an enumerable collection of all found children.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Block"/> to be found.</typeparam>
    /// <returns>An enumerable collection of all found children <see cref="Block"/>s.</returns>
    public IEnumerable<T> GetChildren<T>() where T : Block => Children.OfType<T>();

    /// <summary>
    /// Adds or replaces, if present, the first instance of a <see cref="Block"/> of the specified
    /// type <typeparamref name="T"/> with the provided <paramref name="value"/>. If
    /// <see langword="null"/>, removes the child <see cref="Block"/> instead.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Block"/> to replace.</typeparam>
    /// <param name="value">The new <see cref="Block"/>.</param>
    /// <returns>The replaced <see cref="Block"/> if present, otherwise <see langword="null"/>.</returns>
    public T? SetChild<T>(T? value) where T : Block
    {
        // TODO: see if this should throw when AreBlockFieldsLocked is true
        var candidate = GetChild<T>();
        if (candidate != null) Children.Remove(candidate);

        if (value != null) Children.Add(value);
        return candidate;
    }

    /// <summary>
    /// Indicates whether the fields of this <see cref="Block"/> are currently locked.
    /// <see langword="false"/> by default, toggled by <see cref="Archive"/> for managed
    /// blocks (AHDR, ADBG, LHDR, DPAK) when set to <c>Asset Mode</c>. Has no effect on
    /// unmanaged blocks or unmanaged fields for managed blocks.
    /// </summary>
    internal bool AreBlockFieldsLocked { get; set; } = false;

    /// <summary>
    /// Returns the value of the specified field.
    /// </summary>
    /// <remarks>
    /// This method is currently a pass-through, as we have no need to prevent fields from being
    /// read. However, it is provided for symmetry with <see cref="SetManagedBlockField{T}(ref T, T)"/>
    /// and allows for future extensibility should our requirements change.
    /// </remarks>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">The field to get the value of.</param>
    /// <returns>The value of the field.</returns>
#pragma warning disable CA1822 // Mark members as static
    protected T GetManagedBlockField<T>(ref T field) => field;
#pragma warning restore CA1822 // Mark members as static

    /// <summary>
    /// Sets the value of the specified field. If <see cref="AreBlockFieldsLocked"/> is
    /// <see langword="true"/>, this method will throw an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">The field to set the value of.</param>
    /// <param name="value">The value to set the field to.</param>
    /// <exception cref="InvalidOperationException">Thrown when the block fields are locked.</exception>
    protected void SetManagedBlockField<T>(ref T field, T value)
    {
        // todo: custom exception?
        if (AreBlockFieldsLocked)
            throw new InvalidOperationException(
                $"Cannot modify field of type {typeof(T).Name} on block of type {GetType().Name}" +
                $"because block fields are locked. Release the lock by exiting Asset Mode from" +
                $"{nameof(Archive)} before attempting to modify block fields."
                );

        field = value;
    }
}

namespace EvilHop.Blocks;

/// <summary>
/// A no-data child <see cref="Block"/> of <see cref="Dictionary"/> that stores information
/// about the archive's <c>Layer</c>'s.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#LTOC">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: Required LINF child.
public class LayerTable : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "LTOC";

    /// <summary>
    /// The child <see cref="LayerInf"/> of the <see cref="LayerTable"/>.
    /// </summary>
    public LayerInf Inf
    {
        get => GetRequiredChild<LayerInf>();
        set => SetChild(value);
    }

    /// <summary>
    /// The <see cref="LayerHeader"/> children of the <see cref="AssetTable"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this block's fields are locked.</exception>
    public IEnumerable<LayerHeader> Headers
    {
        get => GetChildren<LayerHeader>();
        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            EnsureFieldsUnlocked();

            foreach (var header in GetChildren<LayerHeader>().ToList()) Children.Remove(header);
            foreach (var header in value) Children.Add(header);
        }
    }

    internal LayerTable() { }
}

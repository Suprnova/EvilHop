namespace EvilHop.Blocks;

/// <summary>
/// A no-data child <see cref="Block"/> of <see cref="Dictionary"/> that stores information
/// about the archive's <c>Assets</c>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#ATOC">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: Required AINF child.
public class AssetTable : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "ATOC";

    /// <summary>
    /// The child <see cref="AssetInf"/> of the <see cref="AssetTable"/>.
    /// </summary>
    public AssetInf Inf
    {
        get => GetRequiredChild<AssetInf>();
        set => SetChild(value);
    }

    /// <summary>
    /// The <see cref="AssetHeader"/> children of the <see cref="AssetTable"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this block's fields are locked.</exception>
    public IEnumerable<AssetHeader> Headers
    {
        get => GetChildren<AssetHeader>();
        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            EnsureFieldsUnlocked();

            foreach (var header in GetChildren<AssetHeader>().ToList()) Children.Remove(header);
            foreach (var header in value) Children.Add(header);
        }
    }

    internal AssetTable() { }
}

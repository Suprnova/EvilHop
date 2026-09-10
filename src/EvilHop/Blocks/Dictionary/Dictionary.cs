namespace EvilHop.Blocks;

/// <summary>
/// A no-data <see cref="Block"/> that serves as the root parent for the
/// <c>Asset</c> and <c>Layer</c> tables.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#DICT">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: Required ATOC and LTOC children.
public class Dictionary : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "DICT";

    /// <summary>
    /// The child <see cref="AssetTable"/> of the <see cref="Dictionary"/>.
    /// </summary>
    public AssetTable AssetTable
    {
        get => GetRequiredChild<AssetTable>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="LayerTable"/> of the <see cref="Dictionary"/>.
    /// </summary>
    public LayerTable LayerTable
    {
        get => GetRequiredChild<LayerTable>();
        set => SetChild(value);
    }

    internal Dictionary() { }
}

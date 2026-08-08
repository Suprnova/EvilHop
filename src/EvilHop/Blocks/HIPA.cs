namespace EvilHop.Blocks;

/// <summary>
/// A no-data, no-children <see cref="Block"/> at the start of every HIP archive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#HIPA">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class HIPA : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "HIPA";
}

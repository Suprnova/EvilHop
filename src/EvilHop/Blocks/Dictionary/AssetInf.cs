namespace EvilHop.Blocks;

/// <summary>
/// A child <see cref="Block"/> of <see cref="AssetTable"/> with unknown use.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#AINF">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class AssetInf : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "AINF";

    /// <summary>
    /// Unknown. Always 0.
    /// </summary>
    /// Validation TODO: Always 0.
    public uint Value { get; set; }

    internal AssetInf() { }
}

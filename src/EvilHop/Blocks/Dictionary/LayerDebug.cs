namespace EvilHop.Blocks;

/// <summary>
/// A child <see cref="Block"/> of <see cref="LayerHeader"/> with unknown use.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#LDBG">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class LayerDebug : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "LDBG";

    /// <summary>
    /// Unknown. Always 0xFFFFFFFF besides in N100F Prototype.
    /// </summary>
    /// Validation TODO: Always 0xFFFFFFFF in non-N100F Proto.
    public uint Value { get; set; }

    internal LayerDebug() { }
}

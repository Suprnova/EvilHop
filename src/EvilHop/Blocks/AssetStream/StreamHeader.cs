namespace EvilHop.Blocks;

/// <summary>
/// A child <see cref="Block"/> of <see cref="AssetStream"/> with unknown use.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#DHDR">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class StreamHeader : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "DHDR";

    /// <summary>
    /// Unknown. Always 0xFFFFFFFF.
    /// </summary>
    /// Validation TODO: Always 0xFFFFFFFF.
    public uint Value { get; set; }

    internal StreamHeader() { }
}

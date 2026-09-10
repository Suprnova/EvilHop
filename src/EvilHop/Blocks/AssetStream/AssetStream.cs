namespace EvilHop.Blocks;

/// <summary>
/// A no-data <see cref="Block"/> that serves as the root parent for all <c>Asset</c> data
/// related blocks.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#STRM">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: Required DHDR and DPAK block.
public class AssetStream : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "STRM";

    /// <summary>
    /// The child <see cref="StreamHeader"/> of the <see cref="AssetStream"/>.
    /// </summary>
    public StreamHeader Header
    {
        get => GetRequiredChild<StreamHeader>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="StreamData"/> of the <see cref="AssetStream"/>.
    /// </summary>
    public StreamData Data
    {
        get => GetRequiredChild<StreamData>();
        set => SetChild(value);
    }

    internal AssetStream() { }
}

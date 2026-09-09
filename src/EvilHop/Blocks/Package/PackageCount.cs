namespace EvilHop.Blocks;

/// <summary>
/// A child <see cref="Block"/> of <see cref="Package"/> that contains information
/// about the counts of particular things within the archive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#PCNT">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class PackageCount : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PCNT";

    /// <summary>
    /// The number of <c>assets</c> present in the archive.
    /// </summary>
    /// Validation TODO: Equal to number of AHDR blocks.
    public uint AssetCount { get; set; }

    /// <summary>
    /// The number of <c>layers</c> present in the archive.
    /// </summary>
    /// Validation TODO: Equal to number of LHDR blocks.
    public uint LayerCount { get; set; }

    /// <summary>
    /// The size of the largest <c>Asset</c> in the archive.
    /// </summary>
    /// Validation TODO: Equal to max .size of AHDRs.
    public uint MaxAssetSize { get; set; }

    /// <summary>
    /// The size of the largest <c>Layer</c> in the archive.
    /// </summary>
    /// Validation TODO: Equal to the largest sum of Size+Plus across a LayerHeader's AssetIds,
    /// counting each listing rather than each distinct asset.
    public uint MaxLayerSize { get; set; }

    /// <summary>
    /// The size of the largest <c>Asset</c> with <c>READ_TRANSFORM</c> in the archive.
    /// </summary>
    /// Validation TODO: Equal to max .size of AHDRs with READ_TRANSFORM as a flag.
    public uint MaxXFormAssetSize { get; set; }

    internal PackageCount() { }
}

using EvilHop.Common;

namespace EvilHop.Blocks;

/// <summary>
/// A child <see cref="Block"/> of <see cref="AssetHeader"/> which defines an entry
/// for an <c>Asset</c>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#ADBG">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class AssetDebug : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "ADBG";

    /// <summary>
    /// The multiple of bytes this <c>Asset</c>'s own data <see cref="AssetHeader.Offset"/> aligns
    /// to.
    /// </summary>
    /// <remarks>
    /// Any non-positive value, including 0, uses a default alignment for the particular
    /// <see cref="AssetType"/>. Most types default to 16, at least four (<c>BinkVideo</c>,
    /// <c>CutsceneTable</c>, <c>StreamingTexture</c>, <c>Wireframe</c>) use 32, and a few
    /// (<c>PickupTypes</c>, <c>ReactiveAnimation</c>, <c>ThrowableTable</c>) to at least 128.
    /// EvilHop does not model this table; it exposes the raw stored value.
    /// </remarks>
    /// Valid calculation of the *previous* Asset's AHDR.plus using this.
    public int Alignment
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    /// <summary>
    /// The name of the <c>Asset</c>.
    /// </summary>
    /// <remarks>
    /// In official archive files, this field is trimmed to 31 characters. This may create
    /// disconnects between the <c>Asset</c>'s name and its <see cref="AssetHeader.Id"/>.
    /// </remarks>
    public string Name
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    } = "";

    /// <summary>
    /// The filename of the file that the <c>Asset</c> was sourced from.
    /// </summary>
    /// <remarks>
    /// Only populated when the <see cref="AssetFlags.SourceFile"/> flag is set
    /// in <see cref="AssetHeader.Flags"/>.
    /// </remarks>
    /// Validation TODO: Set when SourceFile, unset otherwise.
    public string FileName { get; set; } = "";

    /// <summary>
    /// The CRC-32/MPEG-2 checksum of the <c>Asset</c>'s data.
    /// </summary>
    /// Validation TODO: Calculate using asset's data and validate.
    public uint Checksum { get; set; }

    internal AssetDebug() { }
}

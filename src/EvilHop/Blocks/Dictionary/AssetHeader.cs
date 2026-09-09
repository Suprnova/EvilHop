using EvilHop.Common;

namespace EvilHop.Blocks;

/// <summary>
/// A child <see cref="Block"/> of <see cref="AssetTable"/> which defines an entry
/// for an <c>Asset</c>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#AHDR">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: Required ADBG child.
public class AssetHeader : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "AHDR";

    /// <summary>
    /// The child <see cref="AssetDebug"/> of the <see cref="AssetHeader"/>.
    /// </summary>
    public AssetDebug Debug
    {
        get => GetRequiredChild<AssetDebug>();
        set => SetChild(value);
    }

    /// <summary>
    /// The <c>Asset</c>'s ID, calculated from <see cref="AssetDebug.Name"/> with a modified
    /// BKDR hash algorithm.
    /// </summary>
    /// Validation TODO: Equal to ID calculated using ADBG.name.
    /// No uniqueness violations.
    public uint Id
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    /// <summary>
    /// The <c>Asset</c>'s type.
    /// </summary>
    /// Validation TODO: Maps to closed enum value.
    public AssetType Type
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    /// <summary>
    /// The absolute offset of the <c>Asset</c>'s data within the archive.
    /// </summary>
    /// Validation TODO: Does not exceed total archive size.
    public uint Offset
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    /// <summary>
    /// The length of the <c>Asset</c>'s data in bytes.
    /// </summary>
    /// Validation TODO: When added to Offset, does not exceed total archive size.
    public uint Size
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    /// <summary>
    /// The length of the padding between the end of this <c>Asset</c>'s data and the start
    /// of the next's.
    /// </summary>
    /// <remarks>
    /// This pads up to the *next* <c>Asset</c>'s own <see cref="AssetDebug.Alignment"/>
    /// requirement, not this <c>Asset</c>'s. For the last <c>Asset</c> in a <c>Layer</c>,
    /// this value is 0.
    /// </remarks>
    /// Validation TODO: When last asset in layer, equals 0.
    /// When added to Offset and Size, does not exceed total archive size.
    /// Valid calculation using the next Asset's ADBG.alignment.
    public uint Plus
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    /// <summary>
    /// Information about the <c>Asset</c>'s data and how it should be handled in game.
    /// </summary>
    /// Validation TODO: SourceFile and SourceVirtual are not set simultaneously.
    public AssetFlags Flags
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    internal AssetHeader() { }
}

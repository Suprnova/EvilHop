using EvilHop.Common;
using EvilHop.Validation;

namespace EvilHop.Blocks;

/// <summary>
/// A no-data <see cref="Block"/> that serves as the root parent for the
/// <c>Asset</c> and <c>Layer</c> tables.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#DICT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public class Dictionary : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "DICT";

    /// <summary>
    /// The child <see cref="AssetTable"/> of the <see cref="Dictionary"/>.
    /// </summary>
    [RequiredChild]
    public AssetTable AssetTable
    {
        get => GetRequiredChild<AssetTable>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="LayerTable"/> of the <see cref="Dictionary"/>.
    /// </summary>
    [RequiredChild]
    public LayerTable LayerTable
    {
        get => GetRequiredChild<LayerTable>();
        set => SetChild(value);
    }

    internal Dictionary() { }
}

/// <summary>
/// A no-data child <see cref="Block"/> of <see cref="Dictionary"/> that stores information
/// about the archive's <c>Assets</c>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#ATOC">Heavy Iron Modding documentation</seealso>
/// </remarks>
public class AssetTable : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "ATOC";

    /// <summary>
    /// The child <see cref="AssetInf"/> of the <see cref="AssetTable"/>.
    /// </summary>
    [RequiredChild]
    public AssetInf Inf
    {
        get => GetRequiredChild<AssetInf>();
        set => SetChild(value);
    }

    /// <summary>
    /// The <see cref="AssetHeader"/> children of the <see cref="AssetTable"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this block's fields are locked.</exception>
    [RepeatableChild]
    public IEnumerable<AssetHeader> Headers
    {
        get => GetChildren<AssetHeader>();
        set
        {
            EnsureFieldsUnlocked();

            foreach (var header in GetChildren<AssetHeader>().ToList()) Children.Remove(header);
            foreach (var header in value) Children.Add(header);
        }
    }

    internal AssetTable() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="AssetTable"/> with unknown use.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#AINF">Heavy Iron Modding documentation</seealso>
/// </remarks>
[NoChildren]
public class AssetInf : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "AINF";

    /// <summary>
    /// Unknown. Always 0.
    /// </summary>
    [ConstantValue(0u)]
    public uint Value { get; set; }

    internal AssetInf() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="AssetTable"/> which defines an entry
/// for an <c>Asset</c>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#AHDR">Heavy Iron Modding documentation</seealso>
/// </remarks>
public class AssetHeader : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "AHDR";

    /// <summary>
    /// The child <see cref="AssetDebug"/> of the <see cref="AssetHeader"/>.
    /// </summary>
    [RequiredChild]
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
    [ClosedEnum(Severity = Severity.Warning)]
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
    [DefinedBits]
    public AssetFlags Flags
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    internal AssetHeader() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="AssetHeader"/> which defines an entry
/// for an <c>Asset</c>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#ADBG">Heavy Iron Modding documentation</seealso>
/// </remarks>
[NoChildren]
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

/// <summary>
/// A no-data child <see cref="Block"/> of <see cref="Dictionary"/> that stores information
/// about the archive's <c>Layer</c>'s.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#LTOC">Heavy Iron Modding documentation</seealso>
/// </remarks>
public class LayerTable : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "LTOC";

    /// <summary>
    /// The child <see cref="LayerInf"/> of the <see cref="LayerTable"/>.
    /// </summary>
    [RequiredChild]
    public LayerInf Inf
    {
        get => GetRequiredChild<LayerInf>();
        set => SetChild(value);
    }

    /// <summary>
    /// The <see cref="LayerHeader"/> children of the <see cref="AssetTable"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this block's fields are locked.</exception>
    [RepeatableChild]
    public IEnumerable<LayerHeader> Headers
    {
        get => GetChildren<LayerHeader>();
        set
        {
            EnsureFieldsUnlocked();

            foreach (var header in GetChildren<LayerHeader>().ToList()) Children.Remove(header);
            foreach (var header in value) Children.Add(header);
        }
    }

    internal LayerTable() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="LayerTable"/> with unknown use.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#LINF">Heavy Iron Modding documentation</seealso>
/// </remarks>
[NoChildren]
public class LayerInf : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "LINF";

    /// <summary>
    /// Unknown. Always 0.
    /// </summary>
    [ConstantValue(0u)]
    public uint Value { get; set; }

    internal LayerInf() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="LayerTable"/> which defines an entry
/// for a <c>Layer</c>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#LHDR">Heavy Iron Modding documentation</seealso>
/// </remarks>
public class LayerHeader : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "LHDR";

    /// <summary>
    /// The child <see cref="LayerDebug"/> of the <see cref="LayerHeader"/>.
    /// </summary>
    [RequiredChild]
    public LayerDebug Debug
    {
        get => GetRequiredChild<LayerDebug>();
        set => SetChild(value);
    }

    /// <summary>
    /// The <c>Layer</c>'s type.
    /// </summary>
    [ClosedEnum(Severity = Severity.Warning)]
    public LayerType Type
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    /// <summary>
    /// The number of <c>assets</c> present in this <c>Layer</c>.
    /// </summary>
    /// Validation TODO: When summed across all LHDRs, does not exceed count of AHDRs.
    /// Equal to size of AssetIds.
    public uint AssetCount
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    /// <summary>
    /// A list of the IDs for the <c>assets</c> present in this <c>Layer</c>.
    /// </summary>
    /// Validation TODO: For each ID, an AHDR of that ID exists.
    public IEnumerable<uint> AssetIds
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    } = [];

    internal LayerHeader() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="LayerHeader"/> with unknown use.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#LDBG">Heavy Iron Modding documentation</seealso>
/// </remarks>
[NoChildren]
public class LayerDebug : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "LDBG";

    /// <summary>
    /// Unknown. Always 0xFFFFFFFF besides in N100F Prototype.
    /// </summary>
    [ConstantValue(0xFFFFFFFFu, From = GameVersion.BFBB)]
    public uint Value { get; set; }

    internal LayerDebug() { }
}

#pragma warning disable CS1591 // Missing XML comment

/// <summary>
/// Represents all known values for <see cref="AssetHeader.Flags"/>.
/// Communicates information about an <c>Asset</c>'s data and how it should be handled by the game.
/// </summary>
[Flags]
public enum AssetFlags : uint
{
    None = 0U,
    /// <summary>
    /// The <c>Asset</c>'s data was sourced from an external file.
    /// </summary>
    /// <remarks>
    /// When set, <see cref="AssetDebug.FileName"/> should be populated with the file's source.
    /// This should not be set simultaneously with <see cref="SourceVirtual"/>.
    /// </remarks>
    SourceFile = 1U << 0,
    /// <summary>
    /// The <c>Asset</c>'s data was created by Heavy Iron's internal level editor.
    /// </summary>
    /// <remarks>
    /// When set, <see cref="AssetDebug.FileName"/> should be empty.
    /// This should not be set simultaneously with <see cref="SourceFile"/>.
    /// </remarks>
    SourceVirtual = 1U << 1,
    /// <summary>
    /// The <c>Asset</c>'s data is stored in a special format and must be converted into another
    /// at runtime.
    /// </summary>
    ReadTransform = 1U << 2,
    /// <summary>
    /// The <c>Asset</c>'s data must be transformed from a runtime-specific format into a special
    /// binary format.
    /// </summary>
    WriteTransform = 1U << 3,
    UnknownScooby = 1U << 31
}

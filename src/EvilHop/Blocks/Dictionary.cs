using EvilHop.Common;

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
}

/// <summary>
/// A no-data child <see cref="Block"/> of <see cref="Dictionary"/> that stores information
/// about the archive's <c>Assets</c>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#ATOC">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: Required AINF child.
public class AssetTable : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "ATOC";
}

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
    /// The <c>Asset</c>'s ID, calculated from <see cref="AssetDebug.Name"/> with a modified
    /// BKDR hash algorithm.
    /// </summary>
    /// Validation TODO: Equal to ID calculated using ADBG.name.
    /// No uniqueness violations.
    public uint Id { get; set; }

    /// <summary>
    /// The <c>Asset</c>'s type.
    /// </summary>
    /// Validation TODO: Maps to closed enum value.
    public AssetType Type { get; set; }

    /// <summary>
    /// The absolute offset of the <c>Asset</c>'s data within the archive.
    /// </summary>
    /// Validation TODO: Does not exceed total archive size.
    public uint Offset { get; set; }

    /// <summary>
    /// The length of the <c>Asset</c>'s data in bytes.
    /// </summary>
    /// Validation TODO: When added to Offset, does not exceed total archive size.
    public uint Size { get; set; }

    /// <summary>
    /// The length of the padding between the end of this <c>Asset</c>'s data and the start
    /// of the next's.
    /// </summary>
    /// <remarks>
    /// This value is calculated using <see cref="AssetDebug.Alignment"/>. For the last
    /// <c>Asset</c> in a <c>Layer</c>, this value is 0.
    /// </remarks>
    /// Validation TODO: When last asset in layer, equals 0.
    /// When added to Offset and Size, does not exceed total archive size.
    /// Valid calculation using ADBG.alignment.
    public uint Plus { get; set; }

    /// <summary>
    /// Information about the <c>Asset</c>'s data and how it should be handled in game.
    /// </summary>
    /// Validation TODO: SourceFile and SourceVirtual are not set simultaneously.
    public AssetFlags Flags { get; set; }
}

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

    // TODO: validate this is actually an int and not uint
    /// <summary>
    /// The multiple of bytes to align the <c>Asset</c>'s data to.
    /// </summary>
    /// <remarks>
    /// A value of -1 uses the default alignment value for the particular <see cref="AssetType"/>.
    /// </remarks>
    /// Validation TODO: Ensure -1 alignments actually exist.
    /// Valid calculation of AHDR.plus using this.
    public int Alignment { get; set; }

    /// <summary>
    /// The name of the <c>Asset</c>.
    /// </summary>
    /// <remarks>
    /// In official archive files, this field is trimmed to 31 characters. This may create
    /// disconnects between the <c>Asset</c>'s name and its <see cref="AssetHeader.Id"/>.
    /// </remarks>
    /// Validation TODO: When hashed, equals AHDR.id.
    public string Name { get; set; } = "";

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

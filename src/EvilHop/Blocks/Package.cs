using System.Globalization;

namespace EvilHop.Blocks;

/// <summary>
/// A no-data <see cref="Block"/> that serves as the root parent for all archive metadata blocks.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#PACK">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: 5 children pre-Battle, otherwise 6
/// Required children: PVER, PFLG, PCNT, PCRT, and PMOD pre-Battle, plus PLAT post-Battle
/// Group ExpectedChildCount and RequiredChild into one attribute, since Required always means exactly 1 instance
public class Package : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PACK";

    /// <summary>
    /// The child <see cref="PackageVersion"/> of the <see cref="Package"/>.
    /// </summary>
    public PackageVersion Version
    {
        get => GetRequiredChild<PackageVersion>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="PackageFlags"/> of the <see cref="Package"/>.
    /// </summary>
    public PackageFlags Flags
    {
        get => GetRequiredChild<PackageFlags>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="PackageCount"/> of the <see cref="Package"/>.
    /// </summary>
    public PackageCount Counts
    {
        get => GetRequiredChild<PackageCount>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="PackageCreated"/> of the <see cref="Package"/>.
    /// </summary>
    public PackageCreated Created
    {
        get => GetRequiredChild<PackageCreated>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="PackageModified"/> of the <see cref="Package"/>.
    /// </summary>
    public PackageModified Modified
    {
        get => GetRequiredChild<PackageModified>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="PackagePlatform"/> of the <see cref="Package"/>.
    /// </summary>
    /// <remarks>
    /// Only present from BFBB onwards.
    /// </remarks>
    public PackagePlatform? Platform
    {
        get => GetChild<PackagePlatform>();
        set => SetChild(value);
    }

    internal Package() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="Package"/> that contains information
/// about the version of the archive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#PVER">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class PackageVersion : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PVER";

    /// <summary>
    /// Unknown. Always 2.
    /// </summary>
    /// Validation TODO: Always 2.
    public uint SubVersion { get; set; }

    /// <summary>
    /// Indicate the version of the client consuming the archive.
    /// </summary>
    /// Validation TODO: Always 0x00000001 in N100F proto, 0x00040006 in N100F,
    /// and 0X000A000F in all others.
    public ClientVersion ClientVersion { get; set; }

    /// <summary>
    /// Unknown. Always 1.
    /// </summary>
    /// Validation TODO: Always 1.
    public uint CompatVersion { get; set; }

    internal PackageVersion() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="Package"/> that contains information
/// about the archive's flags.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#PFLG">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class PackageFlags : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PFLG";

    /// <summary>
    /// Unknown.
    /// </summary>
    /// Validation TODO: Ensure a valid combination of flags.
    public PackFlags Flags { get; set; }

    internal PackageFlags() { }
}

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
    /// Validation TODO: Equal to size of largest layer in DPAK, excluding padding bytes.
    public uint MaxLayerSize { get; set; }

    /// <summary>
    /// The size of the largest <c>Asset</c> with <c>READ_TRANSFORM</c> in the archive.
    /// </summary>
    /// Validation TODO: Equal to max .size of AHDRs with READ_TRANSFORM as a flag.
    public uint MaxXFormAssetSize { get; set; }

    internal PackageCount() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="Package"/> that contains information
/// about the creation date of the archive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#PCRT">Heavy Iron Modding documentation </seealso>
/// </remarks>
/// Validation TODO: No children.
/// CreatedDate and CreatedDateString represent the same time.
public class PackageCreated : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PCRT";

    private readonly string _dateTimeFormat = "ddd MMM dd HH:mm:ss yyyy";

    /// <summary>
    /// The timestamp at which the archive was created.
    /// </summary>
    /// <remarks>
    /// Within the archive file, this field is stored in Unix time with an offset of
    /// UTC-7:00 (Pacific Time).
    /// </remarks>
    /// Validation TODO: Can convert to a valid Unix time.
    public DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    /// The string representation of the timestamp at which the archive was created.
    /// </summary>
    /// <remarks>
    /// Expects strings in the following <see cref="DateTimeOffset.ToString()"/>
    /// (<c>en-US</c>) formatting:
    /// <code>
    /// ddd MMM dd HH:mm:ss yyyy
    /// </code>
    /// </remarks>
    /// Validation TODO: Matches expected formatting.
    /// Appended by '\n' in N100F.
    public string CreatedDateString { get; set; }

    internal PackageCreated() : this(DateTimeOffset.Now) { }

    internal PackageCreated(DateTimeOffset createdDate)
    {
        CreatedDate = createdDate;
        CreatedDateString = CreatedDate.ToString(_dateTimeFormat, new CultureInfo("en-US"));
    }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="Package"/> that contains information
/// about the last modified date of the archive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#PMOD">Heavy Iron Modding documentation </seealso>
/// </remarks>
/// Validation TODO: No children.
public class PackageModified : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PMOD";

    /// <summary>
    /// The timestamp at which the archive was last modified.
    /// </summary>
    /// <remarks>
    /// Within the archive file, this field is stored in Unix time with an offset of
    /// UTC-7:00 (Pacific Time).
    /// </remarks>
    /// Validation TODO: Can convert to a valid Unix time.
    public DateTimeOffset ModifiedDate { get; set; }

    internal PackageModified() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="Package"/> that contains information
/// about the platform, region, and language the archive was built for.
/// </summary>
/// <remarks>
/// Introduced in Battle. Not present in N100F.
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#PLAT">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class PackagePlatform : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PLAT";

    /// <summary>
    /// The platform the archive was built for.
    /// </summary>
    /// Validation TODO: Maps to one of the expected values.
    public string PlatformId { get; set; } = "";

    /// <summary>
    /// The human-readable name of <see cref="PlatformId"/>.
    /// </summary>
    /// <remarks>
    /// Only present in Battle. Dropped from all subsequent games.
    /// </remarks>
    public string? PlatformName { get; set; }

    /// <summary>
    /// The archive's target region.
    /// </summary>
    /// Validation TODO: "NTSC" or "PAL".
    public string Region { get; set; } = "";

    /// <summary>
    /// The archive's target language.
    /// </summary>
    /// Validation TODO: Maps to a language observed in the documentation.
    public string Language { get; set; } = "";

    /// <summary>
    /// The archive's target game.
    /// </summary>
    public string GameName { get; set; } = "";

    internal PackagePlatform() { }
}

#pragma warning disable CS1591 // Missing XML comment

/// <summary>
/// Represents all known values for <see cref="PackageVersion.ClientVersion"/>.
/// </summary>
public enum ClientVersion : uint
{
    N100FPrototype = 0x00000001,
    N100FRelease = 0x00040006,
    Default = 0x000A000F
}

/// <summary>
/// Represents all known values for <see cref="PackageFlags.Flags"/>.
/// </summary>
[Flags]
public enum PackFlags : uint
{
    Unknown2 = 1U << 1,
    Unknown3 = 1U << 2,
    Unknown4 = 1U << 3,
    Unknown6 = 1U << 5,
    Unknown17 = 1U << 16,
    Unknown18 = 1U << 17,
    Unknown19 = 1U << 18,
    Unknown20 = 1U << 19,
    Unknown21 = 1U << 20,
    Unknown22 = 1U << 21,
    Unknown23 = 1U << 22,
    Unknown25 = 1U << 24,
    Unknown26 = 1U << 25,
    Default = Unknown2 | Unknown3 | Unknown4 | Unknown6,
    DE_PS2_BFBB = Unknown21 | Unknown19,
    US_GC_BFBB = Unknown22 | Unknown20 | Unknown17,
    US_XBOX_BFBB = Unknown22 | Unknown20 | Unknown18,
    US_PS2_BFBB = Unknown22 | Unknown20 | Unknown19,
    GC_MNPAL_BFBB = Unknown23 | Unknown21 | Unknown17,
    US_BFBB = Unknown26,
    DE_PS2_BFBB_2 = Unknown26 | Unknown25
}

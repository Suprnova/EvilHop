using EvilHop.Common;
using EvilHop.Serialization;
using EvilHop.Validation;
using System.Globalization;

namespace EvilHop.Blocks;

/// <summary>
/// A no-data <see cref="Block"/> that serves as the root parent for all archive metadata blocks.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#PACK">Heavy Iron Modding documentation</seealso>
/// </remarks>
public class Package : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PACK";

    /// <summary>
    /// The child <see cref="PackageVersion"/> of the <see cref="Package"/>.
    /// </summary>
    [RequiredChild]
    public PackageVersion Version
    {
        get => GetRequiredChild<PackageVersion>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="PackageFlags"/> of the <see cref="Package"/>.
    /// </summary>
    [RequiredChild]
    public PackageFlags Flags
    {
        get => GetRequiredChild<PackageFlags>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="PackageCount"/> of the <see cref="Package"/>.
    /// </summary>
    [RequiredChild]
    public PackageCount Counts
    {
        get => GetRequiredChild<PackageCount>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="PackageCreated"/> of the <see cref="Package"/>.
    /// </summary>
    [RequiredChild]
    public PackageCreated Created
    {
        get => GetRequiredChild<PackageCreated>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="PackageModified"/> of the <see cref="Package"/>.
    /// </summary>
    [RequiredChild]
    public PackageModified Modified
    {
        get => GetRequiredChild<PackageModified>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="PackagePlatform"/> of the <see cref="Package"/>.
    /// </summary>
    /// <remarks>
    /// Only present from BFBB onwards, except a build carrying <see cref="FormatQuirks.OmitsPlatformBlock"/>
    /// - real evidence being BFBB's <c>font2.HIP</c>, which otherwise reads as an ordinary BFBB archive.
    /// </remarks>
    [RequiredChild(From = GameVersion.BFBB, ExceptQuirks = FormatQuirks.OmitsPlatformBlock)]
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
[NoChildren]
public class PackageVersion : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PVER";

    /// <summary>
    /// Unknown. Always 2.
    /// </summary>
    [ConstantValue(2u)]
    public uint SubVersion { get; set; }

    /// <summary>
    /// Indicate the version of the client consuming the archive.
    /// </summary>
    /// <remarks>
    /// N100F alone shipped both a prototype (<see cref="Blocks.ClientVersion.N100FPrototype"/>) and a
    /// release (<see cref="Blocks.ClientVersion.N100FRelease"/>) build; nothing currently
    /// distinguishes those two builds from each other, so the rule stays widened to that closed pair
    /// for N100F rather than picking one.
    /// </remarks>
    [AllowedValues(ClientVersion.N100FPrototype, ClientVersion.N100FRelease, Games = [GameVersion.N100F])]
    [ConstantValue(ClientVersion.Default, From = GameVersion.BFBB)]
    public ClientVersion ClientVersion { get; set; }

    /// <summary>
    /// Unknown. Always 1.
    /// </summary>
    [ConstantValue(1u)]
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
[NoChildren]
public class PackageFlags : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PFLG";

    /// <summary>
    /// Flag settings for the <see cref="Archive"/>. 
    /// </summary>
    [RequiredBits(PackFlags.Default)]
    [DefinedBits]
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
[NoChildren]
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

/// <summary>
/// A child <see cref="Block"/> of <see cref="Package"/> that contains information
/// about the creation date of the archive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#PCRT">Heavy Iron Modding documentation </seealso>
/// </remarks>
/// CreatedDate and CreatedDateString represent the same time.
[NoChildren]
public class PackageCreated : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PCRT";

    private readonly string _dateTimeFormat = "ddd MMM dd HH:mm:ss yyyy";

    /// <summary>
    /// The timestamp at which the archive was created.
    /// </summary>
    /// <remarks>
    /// Within the archive file, this field is stored as a UTC Unix timestamp.
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
    /// Within the archive file, this field is calculated in whatever local time zone
    /// the build machine's clock was set to.
    /// </remarks>
    /// Validation TODO: Matches expected formatting.
    /// Appended by '\n' in N100F.
    /// Not necessary to validate it matches against CreatedDate, since we don't know the local
    /// timezone for the build machine of unofficial archives.
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
[NoChildren]
public class PackageModified : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PMOD";

    /// <summary>
    /// The timestamp at which the archive was last modified.
    /// </summary>
    /// <remarks>
    /// Within the archive file, this field is stored as a UTC Unix timestamp.
    /// </remarks>
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
[NoChildren]
public class PackagePlatform : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PLAT";

    /// <summary>
    /// The platform the archive was built for.
    /// </summary>
    [AllowedValues("GC", "P2", "XB", Games = [GameVersion.BFBB])]
    [AllowedValues("BX", "GC", "PS2", From = GameVersion.Incredibles)]
    public string PlatformId { get; set; } = "";

    /// <summary>
    /// The human-readable name of <see cref="PlatformId"/>.
    /// </summary>
    /// <remarks>
    /// Only present in Battle. Dropped from all subsequent games.
    /// </remarks>
    [AllowedValues("GameCube", "PlayStation 2", "Xbox", Games = [GameVersion.BFBB])]
    public string? PlatformName { get; set; }

    /// <summary>
    /// The archive's target region.
    /// </summary>
    [AllowedValues("NTSC", "PAL")]
    public string Region { get; set; } = "";

    /// <summary>
    /// The archive's target language.
    /// </summary>
    [AllowedValues("French", "German", "US Common", "United Kingdom", Games = [GameVersion.BFBB])]
    [AllowedValues("DE", "DK", "ES", "FI", "FR", "IT", "JP", "KR", "NL", "NO", "PT", "RU", "SE", "UK", "US", From = GameVersion.Incredibles)]
    public string Language { get; set; } = "";

    /// <summary>
    /// The archive's target game.
    /// </summary>
    [ConstantValue("Sponge Bob", Games = [GameVersion.BFBB])]
    [ConstantValue("Incredibles", From = GameVersion.Incredibles)]
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

    GameCube = 1U << 16,
    Xbox = 1U << 17,
    PlayStation2 = 1U << 18,

    NTSC = 1U << 19,
    PAL = 1U << 20,

    LanguageUSCommon = 1U << 21,
    LanguageUnitedKingdom = 1U << 22,
    LanguageFrench = 1U << 23,
    LanguageGerman = 1U << 24,

    Platform = 1U << 25,

    PlatformMask = GameCube | Xbox | PlayStation2,
    RegionMask = NTSC | PAL,
    LanguageMask = LanguageUSCommon | LanguageUnitedKingdom | LanguageFrench | LanguageGerman,

    Default = Unknown2 | Unknown3 | Unknown4 | Unknown6
}

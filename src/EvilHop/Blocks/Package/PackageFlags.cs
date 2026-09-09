namespace EvilHop.Blocks;

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

#pragma warning disable CS1591 // Missing XML comment

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

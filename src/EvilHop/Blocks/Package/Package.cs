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

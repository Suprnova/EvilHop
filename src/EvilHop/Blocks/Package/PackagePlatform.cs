namespace EvilHop.Blocks;

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

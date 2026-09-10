namespace EvilHop.Blocks;

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
    /// Within the archive file, this field is stored as a UTC Unix timestamp.
    /// </remarks>
    /// Validation TODO: Can convert to a valid Unix time.
    public DateTimeOffset ModifiedDate { get; set; }

    internal PackageModified() { }
}

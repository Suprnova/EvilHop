using System.Globalization;

namespace EvilHop.Blocks;

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

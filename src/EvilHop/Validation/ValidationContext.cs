using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Validation;

/// <summary>
/// Whether an archive is known to have shipped from Heavy Iron Studios itself.
/// </summary>
public enum ArchiveOrigin
{
    /// <summary>The archive's origin is not known.</summary>
    Unknown,

    /// <summary>The archive shipped in a retail or otherwise official build of the game.</summary>
    Official
}

/// <summary>
/// The part an archive plays within a game's set of archives.
/// </summary>
public enum ArchiveRole
{
    /// <summary>The archive's role is not known.</summary>
    Unknown,

    /// <summary>The archive holds a single level's assets.</summary>
    Level,

    /// <summary>The archive is one half of a pair, sharing its role with a sibling archive.</summary>
    Paired,

    /// <summary>The archive holds assets specific to one localization.</summary>
    Localized,

    /// <summary>The archive holds assets shared across every level.</summary>
    Global
}

/// <summary>
/// The information a <see cref="ValidationRule"/> needs about the archive being validated, beyond
/// the subject it is directly checking.
/// </summary>
/// <param name="Profile">The format quirks and game identity the archive was read or written with.</param>
/// <param name="Origin">Whether the archive is known to have shipped from Heavy Iron itself.</param>
/// <param name="Role">The part the archive plays within its game's set of archives.</param>
/// <param name="BuildId">An identifier for the specific build the archive came from, if known.</param>
public sealed record ValidationContext(
    FormatProfile Profile,
    ArchiveOrigin Origin = ArchiveOrigin.Unknown,
    ArchiveRole Role = ArchiveRole.Unknown,
    string? BuildId = null)
{
    /// <summary>
    /// The game the archive was built for, from <see cref="Profile"/>.
    /// </summary>
    public GameVersion Game => Profile.Game;

    /// <summary>
    /// The console the archive was built for, from <see cref="Profile"/>.
    /// </summary>
    public Platform Platform => Profile.Platform;
}

/// <summary>
/// Implemented by every type that can validate itself against the archive format's invariants.
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// Checks this instance against the archive format's invariants.
    /// </summary>
    /// <param name="context">The <see cref="ValidationContext"/> to validate against.</param>
    /// <returns>The <see cref="ValidationIssue"/>s found.</returns>
    IEnumerable<ValidationIssue> Validate(ValidationContext context);
}

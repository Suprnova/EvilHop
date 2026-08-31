namespace EvilHop.Validation;

/// <summary>
/// A single finding produced by validating an archive.
/// </summary>
/// <param name="RuleId">The identifier of the <see cref="ValidationRule"/> that produced this issue.</param>
/// <param name="Severity">How consequential this issue is to the game.</param>
/// <param name="Site">Exactly where the violation is.</param>
/// <param name="Message">A human-readable description of the violation.</param>
/// <param name="Classification">
/// The known violation this issue matches, or <see langword="null"/> if it doesn't match one.
/// </param>
/// <param name="Related">
/// The other sites participating in the violation, for issues arising from a relationship between
/// sites. Defaults to empty.
/// </param>
public readonly record struct ValidationIssue(
    string RuleId,
    Severity Severity,
    IssueSite Site,
    string Message,
    string? Classification = null,
    IReadOnlyList<IssueSite>? Related = null)
{
    /// <summary>
    /// The other sites participating in the violation, for issues arising from a relationship
    /// between sites. Empty by default, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<IssueSite> Related { get; init; } = Related ?? [];
}

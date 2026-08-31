namespace EvilHop.Validation;

/// <summary>
/// Where a <see cref="ValidationRule"/>'s verdict can be independently re-run, from an archive
/// corpus down to the rule's own declaration.
/// </summary>
public enum EvidenceKind
{
    /// <summary>
    /// The rule's input space is small and closed - a type code, a flag word, a version constant.
    /// The distinct observed values can be recorded and the rule re-run against them offline, with
    /// no archive required.
    /// </summary>
    Replayable,

    /// <summary>
    /// The rule ranges over per-asset or per-byte data too large to record in full, but its
    /// outcome collapses to a small ledger plus a handful of self-contained anchors that pin the
    /// algorithm without holding an archive.
    /// </summary>
    Reducible,

    /// <summary>
    /// Nothing smaller than the whole archive will do - round-trip fidelity, or gap-byte
    /// uniformity across a whole stream. Re-running the rule requires the archive itself.
    /// </summary>
    NonReplayable
}

/// <summary>
/// The non-generic base of every validation rule, carrying the identity and scoping every rule
/// shares regardless of what it checks.
/// </summary>
public abstract class ValidationRule
{
    /// <summary>
    /// This rule's stable identifier.
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// How consequential a violation of this rule is to the game.
    /// </summary>
    public abstract Severity Severity { get; }

    /// <summary>
    /// A human-readable description of what this rule checks.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Where this rule's verdict can be independently re-run.
    /// </summary>
    public abstract EvidenceKind Evidence { get; }

    /// <summary>
    /// This rule's revision, hand-bumped whenever a change to its checking logic isn't otherwise
    /// visible from its declaration.
    /// </summary>
    public virtual int RuleRevision => 1;

    /// <summary>
    /// Determines whether this rule applies given the provided <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="ValidationContext"/> to test against.</param>
    /// <returns><see langword="true"/> if this rule applies; otherwise <see langword="false"/>.</returns>
    public virtual bool AppliesTo(ValidationContext context) => true;
}

/// <summary>
/// A <see cref="ValidationRule"/> that checks a subject of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of subject this rule checks.</typeparam>
public abstract class ValidationRule<T> : ValidationRule
{
    /// <summary>
    /// Checks <paramref name="subject"/> against this rule, yielding an issue for every violation
    /// found.
    /// </summary>
    /// <param name="subject">The subject to check.</param>
    /// <param name="context">The <see cref="ValidationContext"/> to check against.</param>
    /// <returns>The <see cref="ValidationIssue"/>s found.</returns>
    public abstract IEnumerable<ValidationIssue> Check(T subject, ValidationContext context);

    /// <summary>
    /// Classifies <paramref name="subject"/> against this rule's known violations.
    /// </summary>
    /// <param name="subject">The subject to classify.</param>
    /// <param name="context">The <see cref="ValidationContext"/> to classify against.</param>
    /// <returns>
    /// The matching known violation's classification tag, or <see langword="null"/> if none match.
    /// </returns>
    public virtual string? Classify(T subject, ValidationContext context) => null;
}

using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Validation;

/// <summary>
/// The base of every declarative validation attribute, carrying the scoping every concrete attribute
/// shares regardless of what it checks.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Field,
                AllowMultiple = true)]
public abstract class ValidationAttribute : Attribute
{
    /// <summary>The games this attribute applies to. Empty means every game.</summary>
    public GameVersion[] Games { get; init; } = [];

    /// <summary>The first game, inclusive, this attribute applies to.</summary>
    public GameVersion From { get; init; } = GameVersion.N100F;

    /// <summary>The last game, inclusive, this attribute applies to.</summary>
    public GameVersion To { get; init; } = GameVersion.Ratatouille;

    /// <summary>
    /// The <see cref="FormatQuirks"/> a profile must carry for this attribute to apply.
    /// <see cref="FormatQuirks.None"/> means "don't care" rather than "no quirks."
    /// </summary>
    public FormatQuirks Quirks { get; init; } = FormatQuirks.None;

    /// <summary>The platforms this attribute applies to. Empty means every platform.</summary>
    public Platform[] Platforms { get; init; } = [];

    /// <summary>How consequential a violation of this attribute's rule is to the game.</summary>
    public Severity Severity { get; init; } = Severity.Error;

    /// <summary>
    /// Determines whether this attribute applies given the provided <paramref name="context"/>,
    /// against every scoping axis at once.
    /// </summary>
    /// <param name="context">The <see cref="ValidationContext"/> to test against.</param>
    /// <returns><see langword="true"/> if every axis matches; otherwise <see langword="false"/>.</returns>
    internal bool Matches(ValidationContext context) =>
        MatchesGames(context) && MatchesGameRange(context) && MatchesQuirks(context) && MatchesPlatforms(context);

    private bool MatchesGames(ValidationContext context) =>
        Games.Length == 0 || Games.Contains(context.Game);

    private bool MatchesGameRange(ValidationContext context) =>
        context.Game >= From && context.Game <= To;

    private bool MatchesQuirks(ValidationContext context) =>
        Quirks == FormatQuirks.None || (context.Profile.Quirks & Quirks) == Quirks;

    private bool MatchesPlatforms(ValidationContext context) =>
        Platforms.Length == 0 || Platforms.Contains(context.Platform);
}

/// <summary>
/// Declares that a member is always exactly <see cref="Value"/> within scope.
/// </summary>
/// <param name="value">The member's expected value.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ConstantValueAttribute(object value) : ValidationAttribute
{
    /// <summary>The member's expected value.</summary>
    public object Value { get; } = value;
}

/// <summary>
/// Declares that a member is always one of <see cref="Values"/> within scope.
/// </summary>
/// <param name="values">The member's closed set of expected values.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class AllowedValuesAttribute(params object[] values) : ValidationAttribute
{
    /// <summary>The member's closed set of expected values.</summary>
    public IReadOnlyList<object> Values { get; } = values;
}

/// <summary>
/// Declares that a member's raw value always maps to a defined member of its own enum type.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClosedEnumAttribute : ValidationAttribute;

/// <summary>
/// Declares that a <c>[Flags]</c>-enum-typed member never sets a bit outside its enum type's declared
/// members.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class DefinedBitsAttribute : ValidationAttribute;

/// <summary>
/// Declares that a <c>[Flags]</c>-enum-typed member always has <see cref="RequiredBits"/> set.
/// </summary>
/// <param name="requiredBits">The bits that must always be set.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class RequiredBitsAttribute(object requiredBits) : ValidationAttribute
{
    /// <summary>The bits that must always be set.</summary>
    public object RequiredBits { get; } = requiredBits;
}

/// <summary>
/// Declares that a property holds exactly one child of its own <see cref="Blocks.Block"/> type while
/// this attribute's scope applies, and none while it doesn't.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class RequiredChildAttribute : ValidationAttribute
{
    /// <summary>
    /// A quirk that drops the requirement to none, for a build that otherwise matches this
    /// attribute's scope but is real evidence the child isn't always there. Defaults to
    /// <see cref="FormatQuirks.None"/>, which never excuses it.
    /// </summary>
    public FormatQuirks ExceptQuirks { get; init; } = FormatQuirks.None;
}

/// <summary>
/// Declares that a block never has any children.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class NoChildrenAttribute : ValidationAttribute;

/// <summary>
/// Declares that a property enumerates zero or more children of a single type.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class RepeatableChildAttribute : ValidationAttribute;

/// <summary>
/// Declares that a member should be recorded in the corpus inventory. Carries no rule of its own.
/// </summary>
/// <remarks>
/// Every rule attribute is also an observable declaration, so a member with one never needs this as
/// well.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ObservedAttribute : ValidationAttribute;

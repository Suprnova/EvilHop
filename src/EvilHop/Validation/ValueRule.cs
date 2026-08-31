namespace EvilHop.Validation;

/// <summary>
/// A <see cref="ValidationRule"/> materialized from a declarative attribute: a predicate over one
/// observable's value, replayable from a recorded value set with no archive required.
/// </summary>
public abstract class ValueRule : ValidationRule
{
    /// <inheritdoc/>
    public sealed override EvidenceKind Evidence => EvidenceKind.Replayable;

    /// <summary>The identifier of the observable this rule checks.</summary>
    public abstract string ObservableId { get; }

    /// <summary>
    /// Determines whether <paramref name="value"/> satisfies this rule.
    /// </summary>
    /// <param name="value">The observed value to check.</param>
    /// <param name="context">The <see cref="ValidationContext"/> to check against.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="value"/> satisfies this rule; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public abstract bool Holds(object value, ValidationContext context);
}

/// <summary>
/// The shared shape every <see cref="ValueRule"/> materialized by <see cref="ValidationCatalogue"/>
/// is built from: identity and scoping supplied once at construction time, gated by the declaring
/// attribute's scope by default.
/// </summary>
internal abstract class AttributeValueRule(
    string id, Severity severity, string description, string observableId, ValidationAttribute source)
    : ValueRule
{
    private readonly ValidationAttribute _source = source;

    public override string Id { get; } = id;
    public override Severity Severity { get; } = severity;
    public override string Description { get; } = description;
    public override string ObservableId { get; } = observableId;
    public override bool AppliesTo(ValidationContext context) => _source.Matches(context);
}

/// <summary>Materialized from <see cref="ConstantValueAttribute"/>.</summary>
internal sealed class ConstantValueRule(
    string id, Severity severity, string description, string observableId, ValidationAttribute source,
    object expected)
    : AttributeValueRule(id, severity, description, observableId, source)
{
    public override bool Holds(object value, ValidationContext context) => Equals(expected, value);
}

/// <summary>Materialized from <see cref="AllowedValuesAttribute"/>.</summary>
internal sealed class AllowedValuesRule(
    string id, Severity severity, string description, string observableId, ValidationAttribute source,
    IReadOnlyList<object> values)
    : AttributeValueRule(id, severity, description, observableId, source)
{
    public override bool Holds(object value, ValidationContext context) => values.Contains(value);
}

/// <summary>Materialized from <see cref="ClosedEnumAttribute"/>.</summary>
internal sealed class ClosedEnumRule(
    string id, Severity severity, string description, string observableId, ValidationAttribute source,
    Type enumType)
    : AttributeValueRule(id, severity, description, observableId, source)
{
    public override bool Holds(object value, ValidationContext context) => Enum.IsDefined(enumType, value);
}

/// <summary>Materialized from <see cref="DefinedBitsAttribute"/>.</summary>
internal sealed class DefinedBitsRule(
    string id, Severity severity, string description, string observableId, ValidationAttribute source,
    ulong knownBits)
    : AttributeValueRule(id, severity, description, observableId, source)
{
    public override bool Holds(object value, ValidationContext context) =>
        (Convert.ToUInt64(value) & ~knownBits) == 0;
}

/// <summary>Materialized from <see cref="RequiredBitsAttribute"/>.</summary>
internal sealed class RequiredBitsRule(
    string id, Severity severity, string description, string observableId, ValidationAttribute source,
    object requiredBits)
    : AttributeValueRule(id, severity, description, observableId, source)
{
    private readonly ulong _required = Convert.ToUInt64(requiredBits);

    public override bool Holds(object value, ValidationContext context) =>
        (Convert.ToUInt64(value) & _required) == _required;
}

/// <summary>
/// Materialized from <see cref="RequiredChildAttribute"/>. Unlike every other <see cref="ValueRule"/>,
/// this one always applies - the declaring attribute's scope changes the expected child count instead
/// of gating whether the rule runs, so a required child reported present outside its scope is caught
/// too, not just one missing within it.
/// </summary>
internal sealed class RequiredChildRule(
    string id, Severity severity, string description, string observableId, ValidationAttribute source)
    : AttributeValueRule(id, severity, description, observableId, source)
{
    private readonly ValidationAttribute _source = source;

    public override bool AppliesTo(ValidationContext context) => true;

    public override bool Holds(object value, ValidationContext context) =>
        (int)value == (_source.Matches(context) ? 1 : 0);
}

/// <summary>Materialized from <see cref="NoChildrenAttribute"/>.</summary>
internal sealed class NoChildrenRule(
    string id, Severity severity, string description, string observableId, ValidationAttribute source)
    : AttributeValueRule(id, severity, description, observableId, source)
{
    public override bool Holds(object value, ValidationContext context) => (int)value == 0;
}

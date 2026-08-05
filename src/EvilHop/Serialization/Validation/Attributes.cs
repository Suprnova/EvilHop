using EvilHop.Serialization;
using System.Collections.Concurrent;
using System.Reflection;

namespace EvilHop.Serialization.Validation;

/// <summary>
/// Base class for validation attributes that can be applied to specific versions of the file format.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
public abstract class VersionedValidationAttribute : Attribute
{
    /// <summary>
    /// Specific versions this rule applies to. If null, applies to all versions.
    /// </summary>
    public FileFormatVersion[]? Versions { get; init; }

    /// <summary>
    /// Minimum version (inclusive) this rule applies to. If set, rule applies to all versions >= this.
    /// </summary>
    public FileFormatVersion MinVersion { get; init; } = (FileFormatVersion)(-1);

    /// <summary>
    /// Maximum version (inclusive) this rule applies to. If set, rule applies to all versions <= this.
    /// </summary>
    public FileFormatVersion MaxVersion { get; init; } = (FileFormatVersion)(-1);

    /// <summary>
    /// Severity of the validation issue.
    /// </summary>
    public abstract ValidationSeverity Severity { get; init; }

    protected VersionedValidationAttribute() { }

    protected VersionedValidationAttribute(params FileFormatVersion[] versions)
    {
        Versions = versions;
    }

    internal bool AppliesTo(FileFormatVersion version)
    {
        if (Versions != null) return Versions.Contains(version);

        // Check if MinVersion was explicitly set (not default)
        if ((int)MinVersion >= 0 && version < MinVersion) return false;

        // Check if MaxVersion was explicitly set (not default)
        if ((int)MaxVersion >= 0 && version > MaxVersion) return false;

        return true;
    }

    internal bool AppliesToAny(IEnumerable<FileFormatVersion> versions) => versions.Any(AppliesTo);
}

/// <summary>
/// Specifies the version range for which the decorated member is valid.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class VersionRangeAttribute : VersionedValidationAttribute
{
    public VersionRangeAttribute(FileFormatVersion minVersion, FileFormatVersion maxVersion = FileFormatVersion.Rat)
    {
        MinVersion = minVersion;
        MaxVersion = maxVersion;
    }

    public override ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

/// <summary>
/// Specifies the expected value for a property or field.
/// </summary>
/// <param name="value">The expected value.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class ExpectedValueAttribute(object value) : VersionedValidationAttribute()
{
    public object Value { get; } = value;
    public override ValidationSeverity Severity { get; init; } = ValidationSeverity.Warning;
}

/// <summary>
/// Specifies the allowed values for a property or field.
/// </summary>
/// <param name="allowed">The allowed values.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class AllowedValuesAttribute(params object[] allowed) : VersionedValidationAttribute()
{
    public object[] Allowed { get; } = allowed;
    public override ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

/// <summary>
/// Specifies the expected number of child blocks for a block class.
/// </summary>
/// <param name="count">The expected number of child blocks.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ExpectedChildCountAttribute(int count) : VersionedValidationAttribute()
{
    public int Count { get; } = count;
    public override ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

/// <summary>
/// Specifies that the block declaring the decorated member must have a child of the member's type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class RequiredChildAttribute : VersionedValidationAttribute
{
    /// <summary>
    /// The type of child block that is required, derived from the decorated member's type.
    /// </summary>
    public Type ChildType { get; internal set; } = typeof(object);

    public override ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

internal static class ValidationAttributesCache
{
    private static readonly ConcurrentDictionary<Type, TypeValidationAttributes> Cache = new();

    public static TypeValidationAttributes GetAttributes(Type type) =>
        Cache.GetOrAdd(type, LoadAttributes);

    private static TypeValidationAttributes LoadAttributes(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.CanRead)
            .ToList();

        return new TypeValidationAttributes
        {
            ChildCounts = [.. type.GetCustomAttributes<ExpectedChildCountAttribute>(inherit: false)],
            RequiredChildren = [.. properties
                .Select(prop => BindChildType(prop.GetCustomAttribute<RequiredChildAttribute>(inherit: false), prop))
                .OfType<RequiredChildAttribute>()],
            Fields = properties
                .Select(prop => (prop, rules: new FieldValidationAttributes
                {
                    ExpectedValues = [.. prop.GetCustomAttributes<ExpectedValueAttribute>(inherit: false)],
                    AllowedValues = [.. prop.GetCustomAttributes<AllowedValuesAttribute>(inherit: false)],
                    VersionRanges = [.. prop.GetCustomAttributes<VersionRangeAttribute>(inherit: false)]
                }))
                .Where(kvp => kvp.rules.ExpectedValues.Count > 0
                            || kvp.rules.AllowedValues.Count > 0
                            || kvp.rules.VersionRanges.Count > 0)
                .ToDictionary(kvp => kvp.prop, kvp => kvp.rules)
        };
    }

    /// <summary>
    /// Derives the required child type from the decorated property, unwrapping nullable value types.
    /// </summary>
    private static RequiredChildAttribute? BindChildType(RequiredChildAttribute? attribute, PropertyInfo property)
    {
        if (attribute is null) return null;

        attribute.ChildType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return attribute;
    }
}

internal sealed class TypeValidationAttributes
{
    public IReadOnlyList<ExpectedChildCountAttribute> ChildCounts { get; init; } = [];
    public IReadOnlyList<RequiredChildAttribute> RequiredChildren { get; init; } = [];
    public IReadOnlyDictionary<PropertyInfo, FieldValidationAttributes> Fields { get; init; } = new Dictionary<PropertyInfo, FieldValidationAttributes>();
}

public sealed class FieldValidationAttributes
{
    public IReadOnlyList<ExpectedValueAttribute> ExpectedValues { get; init; } = [];
    public IReadOnlyList<AllowedValuesAttribute> AllowedValues { get; init; } = [];
    public IReadOnlyList<VersionRangeAttribute> VersionRanges { get; init; } = [];
}

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
    public FileFormatVersion? MinVersion { get; init; }

    /// <summary>
    /// Maximum version (inclusive) this rule applies to. If set, rule applies to all versions <= this.
    /// </summary>
    public FileFormatVersion? MaxVersion { get; init; }

    /// <summary>
    /// Severity of the validation issue.
    /// </summary>
    public abstract ValidationSeverity Severity { get; init; }

    protected VersionedValidationAttribute() { }

    protected VersionedValidationAttribute(params FileFormatVersion[] versions)
    {
        Versions = versions;
    }

    protected VersionedValidationAttribute(FileFormatVersion minVersion, FileFormatVersion? maxVersion = null)
    {
        MinVersion = minVersion;
        MaxVersion = maxVersion;
    }

    internal bool AppliesTo(FileFormatVersion version)
    {
        if (Versions != null) return Versions.Contains(version);
        if (MinVersion.HasValue && version < MinVersion.Value) return false;
        if (MaxVersion.HasValue && version > MaxVersion.Value) return false;

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
    public override ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Specifies the version range (inclusive) for which the decorated member is valid.
    /// </summary>
    /// <param name="minVersion">The minimum version (inclusive).</param>
    /// <param name="maxVersion">The maximum version (inclusive).</param>
    public VersionRangeAttribute(FileFormatVersion minVersion, FileFormatVersion? maxVersion = null)
        : base(minVersion, maxVersion) { }

    /// <summary>
    /// Specifies the specific versions for which the decorated member is valid.
    /// </summary>
    /// <param name="versions">The specific versions.</param>
    public VersionRangeAttribute(params FileFormatVersion[] versions) : base(versions) { }
}

/// <summary>
/// Specifies the expected value for a property or field.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class ExpectedValueAttribute : VersionedValidationAttribute
{
    public object Value { get; }
    public override ValidationSeverity Severity { get; init; } = ValidationSeverity.Warning;

    /// <summary>
    /// Specifies the expected value for a property or field.
    /// </summary>
    /// <param name="value">The expected value.</param>
    public ExpectedValueAttribute(object value) : base() { Value = value; }

    /// <summary>
    /// Specifies the version range (inclusive) for which the expected value applies.
    /// </summary>
    /// <param name="value">The expected value.</param>
    /// <param name="minVersion">The minimum version (inclusive).</param>
    /// <param name="maxVersion">The maximum version (inclusive).</param>
    public ExpectedValueAttribute(object value, FileFormatVersion minVersion, FileFormatVersion? maxVersion = null)
        : base(minVersion, maxVersion) { Value = value; }

    /// <summary>
    /// Specifies the specific versions for which the expected value applies.
    /// </summary>
    /// <param name="value">The expected value.</param>
    /// <param name="versions">The specific versions.</param>
    public ExpectedValueAttribute(object value, params FileFormatVersion[] versions)
        : base(versions) { Value = value; }
}

/// <summary>
/// Specifies the allowed values for a property or field.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class AllowedValuesAttribute : VersionedValidationAttribute
{
    public object[] Allowed { get; }
    public override ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Specifies the allowed values for a property or field.
    /// </summary>
    /// <param name="allowed">The allowed values.</param>
    public AllowedValuesAttribute(params object[] allowed) : base()
    {
        Allowed = allowed;
    }

    /// <summary>
    /// Specifies the minimum version (inclusive) for which the allowed values apply.
    /// </summary>
    /// <param name="minVersion">The minimum version (inclusive).</param>
    /// <param name="allowed">The allowed values.</param>
    public AllowedValuesAttribute(FileFormatVersion minVersion, params object[] allowed)
        : base(minVersion)
    {
        Allowed = allowed;
    }

    /// <summary>
    /// Specifies the version range (inclusive) for which the allowed values apply.
    /// </summary>
    /// <param name="minVersion">The minimum version (inclusive).</param>
    /// <param name="maxVersion">The maximum version (inclusive).</param>
    /// <param name="allowed">The allowed values.</param>
    public AllowedValuesAttribute(FileFormatVersion minVersion, FileFormatVersion maxVersion, params object[] allowed)
        : base(minVersion, maxVersion)
    {
        Allowed = allowed;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ExpectedChildCountAttribute : VersionedValidationAttribute
{
    public int Count { get; }
    public override ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Specifies the expected number of child blocks for a block class.
    /// </summary>
    /// <param name="count">The expected number of child blocks.</param>
    public ExpectedChildCountAttribute(int count) : base()
    {
        Count = count;
    }

    /// <summary>
    /// Specifies the version range (inclusive) for which the number of
    /// child blocks are expected for a block class.
    /// </summary>
    /// <param name="count">The expected number of child blocks.</param>
    /// <param name="minVersion">The minimum version (inclusive).</param>
    /// <param name="maxVersion">The maximum version (inclusive).</param>
    public ExpectedChildCountAttribute(int count, FileFormatVersion minVersion, FileFormatVersion? maxVersion = null)
        : base(minVersion, maxVersion)
    {
        Count = count;
    }
}

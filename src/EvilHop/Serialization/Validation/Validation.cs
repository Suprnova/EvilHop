using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public enum ValidationSeverity { None, Warning, Error }

public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public required string Message { get; set; }
    // todo: is this required? are there validation issues without context?
    public required Block Context { get; set; }
}

public enum ValidationMode { None, Warn, Strict };

public class SerializerOptions
{
    public ValidationMode Mode { get; set; } = ValidationMode.None;
    public Action<ValidationIssue>? OnValidationIssue { get; set; }
}

public static class ValidationExtensions
{
    public static bool IsValid(this Block block, IFormatSerializer serializer, out IEnumerable<ValidationIssue> issues)
    {
        issues = [.. serializer.Validate(block)];
        return !issues.Any(i => i.Severity == ValidationSeverity.Error);
    }

    public static bool IsValid(this HipFile hip, IFormatSerializer serializer, out IEnumerable<ValidationIssue> issues)
    {
        issues = [.. serializer.ValidateArchive(hip)];
        return !issues.Any(i => i.Severity == ValidationSeverity.Error);
    }
}
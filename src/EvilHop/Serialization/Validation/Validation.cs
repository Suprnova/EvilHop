using EvilHop.Blocks;
using EvilHop.Serialization.Serializers;

namespace EvilHop.Serialization.Validation;

// todo: maybe something between None and Warning, where we know it's not right but also know it won't affect the game
public enum ValidationSeverity { None, Warning, Error }

public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public required string Message { get; set; }
    // todo: is this required? are there validation issues without context?
    public required Block Context { get; set; }
}

public enum ValidationMode { None, Warn, Strict };

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

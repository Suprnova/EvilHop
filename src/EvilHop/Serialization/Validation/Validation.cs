using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

// todo: maybe something between None and Warning, where we know it's not right but also know it won't affect the game
public enum ValidationSeverity { None, Warning, Error }

public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public required string Message { get; set; }
    public required Block? Context { get; set; }

    public static ValidationIssue MissingChild<TParent, TChild>(TParent context) where TParent : Block where TChild : Block
    {
        return new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Message = $"Block type {typeof(TChild).Name} is missing from {typeof(TParent).Name}.",
            Context = context
        };
    }

    public static ValidationIssue UnknownValue<TBlock>(String field, Object value, TBlock context) where TBlock : Block
    {
        return new ValidationIssue
        {
            Severity = ValidationSeverity.Warning,
            Message = $"{field} in block type {typeof(TBlock).Name} has unknown value {value}.",
            Context = context
        };
    }
}

public enum ValidationMode { None, Warn, Strict };

public static class ValidationExtensions
{
    public static bool IsValid(this Block block, IFormatSerializer serializer, out IEnumerable<ValidationIssue> issues)
    {
        issues = [.. serializer.ValidateBlock(block)];
        return !issues.Any(i => i.Severity >= ValidationSeverity.Warning);
    }

    public static bool IsValid(this HipFile hip, IFormatSerializer serializer, out IEnumerable<ValidationIssue> issues)
    {
        issues = [.. serializer.ValidateHip(hip)];
        return !issues.Any(i => i.Severity >= ValidationSeverity.Warning);
    }
}

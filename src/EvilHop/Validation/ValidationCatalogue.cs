using EvilHop.Blocks;
using System.Linq.Expressions;
using System.Reflection;

namespace EvilHop.Validation;

/// <summary>
/// Reflects once over every declarative validation attribute in the assembly and materializes them
/// into runnable <see cref="ValueRule"/>s, so that validating a <see cref="Block"/> never itself
/// touches reflection.
/// </summary>
public sealed class ValidationCatalogue
{
    /// <summary>The catalogue, built and cached on first use.</summary>
    public static ValidationCatalogue Instance => Lazy.Value;

    private static readonly Lazy<ValidationCatalogue> Lazy = new(Build);

    private readonly IReadOnlyDictionary<Type, IReadOnlyList<Entry>> _entriesByType;

    private ValidationCatalogue(IReadOnlyDictionary<Type, IReadOnlyList<Entry>> entriesByType) =>
        _entriesByType = entriesByType;

    /// <summary>
    /// One materialized rule for one <see cref="Block"/> type: how to read the value it checks, and
    /// where to site an issue if it doesn't hold. <see cref="Member"/> is <see langword="null"/> for a
    /// class-level rule, which sites at the block itself rather than one of its fields.
    /// </summary>
    private readonly record struct Entry(ValueRule Rule, string? Member, Func<Block, object?> Accessor);

    /// <summary>
    /// Checks <paramref name="subject"/> against every rule declared for its runtime type.
    /// </summary>
    /// <param name="subject">The block to check.</param>
    /// <param name="context">The <see cref="ValidationContext"/> to check against.</param>
    /// <returns>The <see cref="ValidationIssue"/>s found.</returns>
    public IEnumerable<ValidationIssue> Validate(Block subject, ValidationContext context)
    {
        if (!_entriesByType.TryGetValue(subject.GetType(), out var entries)) yield break;

        BlockPath? path = null;
        foreach (var entry in entries)
        {
            if (!entry.Rule.AppliesTo(context)) continue;

            object? value = entry.Accessor(subject);
            if (value is null || entry.Rule.Holds(value, context)) continue;

            path ??= BlockPath.For(subject);
            IssueSite site = entry.Member is null
                ? new BlockSite(path.Value)
                : new BlockFieldSite(path.Value, entry.Member);

            yield return new ValidationIssue(entry.Rule.Id, entry.Rule.Severity, site, entry.Rule.Description);
        }
    }

    private static ValidationCatalogue Build()
    {
        var entriesByType = new Dictionary<Type, IReadOnlyList<Entry>>();

        var blockTypes = typeof(Block).Assembly.GetTypes()
            .Where(type => typeof(Block).IsAssignableFrom(type) && !type.IsAbstract);

        foreach (var type in blockTypes)
        {
            string tag = ReadTag(type);
            var entries = new List<Entry>();

            foreach (var attribute in type.GetCustomAttributes<NoChildrenAttribute>())
                entries.Add(BuildNoChildrenEntry(tag, attribute));

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                foreach (var attribute in property.GetCustomAttributes<ValidationAttribute>())
                {
                    var entry = BuildPropertyEntry(tag, property, attribute);
                    if (entry is { } value) entries.Add(value);
                }

            if (entries.Count > 0) entriesByType[type] = entries;
        }

        return new ValidationCatalogue(entriesByType);
    }

    private static string ReadTag(Type blockType) =>
        ((Block)Activator.CreateInstance(blockType, nonPublic: true)!).Tag;

    private static Entry BuildNoChildrenEntry(string tag, NoChildrenAttribute attribute)
    {
        string id = $"{tag.ToLowerInvariant()}-no-children";
        string description = $"{tag} has no children.";
        var rule = new NoChildrenRule(id, attribute.Severity, description, tag, attribute);

        return new Entry(rule, Member: null, Accessor: block => block.Children.Count);
    }

    private static Entry? BuildPropertyEntry(string tag, PropertyInfo property, ValidationAttribute attribute)
    {
        string member = property.Name;
        string observableId = $"{tag}.{char.ToLowerInvariant(member[0])}{member[1..]}";
        string idBase = $"{tag.ToLowerInvariant()}.{member.ToLowerInvariant()}";

        switch (attribute)
        {
            case ConstantValueAttribute constant:
                return new Entry(
                    new ConstantValueRule(
                        $"{idBase}-constant", attribute.Severity,
                        $"{observableId} is always {constant.Value}.", observableId, attribute, constant.Value),
                    member, CompileAccessor(property));

            case AllowedValuesAttribute allowed:
                return new Entry(
                    new AllowedValuesRule(
                        $"{idBase}-allowed-values", attribute.Severity,
                        $"{observableId} is one of {string.Join(", ", allowed.Values)}.", observableId, attribute,
                        allowed.Values),
                    member, CompileAccessor(property));

            case ClosedEnumAttribute:
                return new Entry(
                    new ClosedEnumRule(
                        $"{idBase}-closed-enum", attribute.Severity,
                        $"{observableId} maps to a defined {property.PropertyType.Name} member.", observableId,
                        attribute, property.PropertyType),
                    member, CompileAccessor(property));

            case DefinedBitsAttribute:
                return new Entry(
                    new DefinedBitsRule(
                        $"{idBase}-defined-bits", attribute.Severity,
                        $"{observableId} sets no bit outside {property.PropertyType.Name}.", observableId, attribute,
                        KnownBits(property.PropertyType)),
                    member, CompileAccessor(property));

            case RequiredBitsAttribute required:
                return new Entry(
                    new RequiredBitsRule(
                        $"{idBase}-required-bits", attribute.Severity,
                        $"{observableId} always has {required.RequiredBits} set.", observableId, attribute,
                        required.RequiredBits),
                    member, CompileAccessor(property));

            case RequiredChildAttribute:
                Type childType = property.PropertyType;
                return new Entry(
                    new RequiredChildRule(
                        $"{idBase}-required", attribute.Severity,
                        $"{observableId} must have exactly one {childType.Name} child while in scope, and none outside it.",
                        observableId, attribute),
                    member, block => block.Children.Count(childType.IsInstanceOfType));

            default:
                // RepeatableChildAttribute and ObservedAttribute are recognized but carry no rule.
                return null;
        }
    }

    private static Func<Block, object?> CompileAccessor(PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(Block), "block");
        var typed = Expression.Convert(parameter, property.DeclaringType!);
        var access = Expression.Property(typed, property);
        var boxed = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<Block, object?>>(boxed, parameter).Compile();
    }

    private static ulong KnownBits(Type enumType) =>
        Enum.GetValues(enumType).Cast<object>().Aggregate(0UL, (bits, member) => bits | Convert.ToUInt64(member));
}

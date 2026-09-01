using EvilHop.Blocks;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace EvilHop.Validation;

/// <summary>
/// Reflects once over every declarative validation attribute in the assembly and materializes them
/// into runnable <see cref="ValueRule"/>s and <see cref="Observable"/>s, so that neither validating
/// nor observing a <see cref="Block"/> ever itself touches reflection.
/// </summary>
public sealed class ValidationCatalogue
{
    /// <summary>The catalogue, built and cached on first use.</summary>
    public static ValidationCatalogue Instance => Lazy.Value;

    private static readonly Lazy<ValidationCatalogue> Lazy = new(Build);

    private readonly IReadOnlyDictionary<Type, IReadOnlyList<Entry>> _entriesByType;
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<Observable>> _observablesByType;
    private readonly IReadOnlyDictionary<string, Observable> _observablesById;

    /// <summary>Every <see cref="Observable"/> declared in the assembly, the union every consumer reads.</summary>
    public IReadOnlyList<Observable> Observables { get; }

    /// <summary>Every <see cref="ValueRule"/> materialized from a declarative attribute in the assembly.</summary>
    public IReadOnlyList<ValueRule> Rules { get; }

    private ValidationCatalogue(
        IReadOnlyDictionary<Type, IReadOnlyList<Entry>> entriesByType,
        IReadOnlyDictionary<Type, IReadOnlyList<Observable>> observablesByType)
    {
        _entriesByType = entriesByType;
        _observablesByType = observablesByType;
        _observablesById = observablesByType.Values.SelectMany(o => o).ToDictionary(o => o.Id);
        Observables = [.. _observablesById.Values];
        Rules = [.. entriesByType.Values.SelectMany(entries => entries).Select(entry => entry.Rule)];
    }

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

    /// <summary>
    /// Projects <paramref name="subject"/> through every <see cref="Observable"/> declared for its
    /// runtime type.
    /// </summary>
    /// <param name="subject">The block to observe.</param>
    /// <returns>Each matching observable's ID paired with a value it yielded.</returns>
    public IEnumerable<(string ObservableId, object Value)> Observe(Block subject)
    {
        if (!_observablesByType.TryGetValue(subject.GetType(), out var observables)) yield break;

        var source = new BlockObservationSource(subject);
        foreach (var observable in observables)
            foreach (var value in observable.Select(source))
                yield return (observable.Id, value);
    }

    /// <summary>
    /// Produces a digest of <paramref name="observableId"/>'s declaration, so a fingerprint built
    /// from it changes exactly when the declaration does.
    /// </summary>
    /// <param name="observableId">The identifier of the observable to digest.</param>
    /// <returns>The digest, as a lowercase hex string.</returns>
    public string DigestOf(string observableId)
    {
        if (!_observablesById.TryGetValue(observableId, out var observable))
            throw new ArgumentException($"'{observableId}' is not a known observable.", nameof(observableId));

        string material = $"{observable.Id}|{observable.Scope}|{observable.Cardinality}|{observable.Presentation}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private static ValidationCatalogue Build()
    {
        var entriesByType = new Dictionary<Type, IReadOnlyList<Entry>>();
        var observablesByType = new Dictionary<Type, IReadOnlyList<Observable>>();

        var blockTypes = typeof(Block).Assembly.GetTypes()
            .Where(type => typeof(Block).IsAssignableFrom(type) && !type.IsAbstract);

        foreach (var type in blockTypes)
        {
            string tag = ReadTag(type);
            var entries = new List<Entry>();
            var observables = new List<Observable>();

            foreach (var attribute in type.GetCustomAttributes<NoChildrenAttribute>())
                entries.Add(BuildNoChildrenEntry(tag, attribute));

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attributes = property.GetCustomAttributes<ValidationAttribute>().ToList();

                foreach (var attribute in attributes)
                {
                    var entry = BuildPropertyEntry(tag, property, attribute);
                    if (entry is { } value) entries.Add(value);
                }

                if (BuildObservable(tag, property, attributes) is { } observable) observables.Add(observable);
            }

            if (entries.Count > 0) entriesByType[type] = entries;
            if (observables.Count > 0) observablesByType[type] = observables;
        }

        return new ValidationCatalogue(entriesByType, observablesByType);
    }

    private static Observable? BuildObservable(string tag, PropertyInfo property, IReadOnlyList<ValidationAttribute> attributes)
    {
        bool isValue = attributes.Any(a =>
            a is ObservedAttribute or ConstantValueAttribute or AllowedValuesAttribute or ClosedEnumAttribute
                or DefinedBitsAttribute or RequiredBitsAttribute);
        if (!isValue) return null;

        var (cardinality, presentation) = InferObservableShape(property.PropertyType);
        return new Observable(ObservableId(tag, property), ObservableScope.Block, cardinality, presentation, BuildSelector(property));
    }

    private static Func<ObservationSource, IEnumerable<object>> BuildSelector(PropertyInfo property)
    {
        var accessor = CompileAccessor(property);
        Type declaringType = property.DeclaringType!;

        return source =>
        {
            if (source is not BlockObservationSource(var block) || !declaringType.IsInstanceOfType(block))
                return [];

            object? value = ToPrimitive(accessor(block));
            return value is null ? [] : [value];
        };
    }

    private static object? ToPrimitive(object? value) =>
        value is Enum e ? Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType())) : value;

    private static (ObservableCardinality Cardinality, ObservablePresentation Presentation) InferObservableShape(Type propertyType)
    {
        Type effectiveType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (effectiveType.IsEnum)
        {
            if (effectiveType.IsDefined(typeof(FlagsAttribute), inherit: false))
                return (ObservableCardinality.Bitmask, ObservablePresentation.Hex);

            bool isFourcc = effectiveType.IsDefined(typeof(FourccAttribute), inherit: false);
            return (ObservableCardinality.Enumerated, isFourcc ? ObservablePresentation.Fourcc : ObservablePresentation.Hex);
        }

        if (effectiveType == typeof(string)) return (ObservableCardinality.Enumerated, ObservablePresentation.Text);

        return (ObservableCardinality.Enumerated, ObservablePresentation.Number);
    }

    private static string ObservableId(string tag, PropertyInfo property) =>
        $"{tag}.{char.ToLowerInvariant(property.Name[0])}{property.Name[1..]}";

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
        string observableId = ObservableId(tag, property);
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

            case RequiredChildAttribute requiredChild:
                Type childType = property.PropertyType;
                return new Entry(
                    new RequiredChildRule(
                        $"{idBase}-required", attribute.Severity,
                        $"{observableId} must have exactly one {childType.Name} child while in scope, and none outside it.",
                        observableId, requiredChild),
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

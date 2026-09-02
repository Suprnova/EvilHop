using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace EvilHop.Validation;

/// <summary>
/// Reflects once over every declarative validation attribute in the assembly and materializes them
/// into runnable <see cref="ValueRule"/>s and <see cref="Observable"/>s, so that neither validating
/// nor observing a <see cref="Block"/> or an <see cref="Asset"/> ever itself touches reflection.
/// </summary>
public sealed class ValidationCatalogue
{
    /// <summary>
    /// The dependency key naming the asset codec registry, for a consumer whose output depends on
    /// what an asset parses into rather than on any one observable's declaration.
    /// </summary>
    public const string AssetCodecsKey = "AssetCodecs.shapes";

    /// <summary>The catalogue, built and cached on first use.</summary>
    public static ValidationCatalogue Instance => Lazy.Value;

    private static readonly Lazy<ValidationCatalogue> Lazy = new(Build);

    private readonly IReadOnlyDictionary<Type, IReadOnlyList<Entry>> _entriesByType;
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<Observable>> _observablesByType;
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<Observable>> _assetObservablesByType;
    private readonly IReadOnlyDictionary<string, Observable> _observablesById;
    private readonly string _assetCodecsMaterial;

    /// <summary>Every <see cref="Observable"/> declared in the assembly, the union every consumer reads.</summary>
    public IReadOnlyList<Observable> Observables { get; }

    /// <summary>Every <see cref="ValueRule"/> materialized from a declarative attribute in the assembly.</summary>
    public IReadOnlyList<ValueRule> Rules { get; }

    private ValidationCatalogue(
        IReadOnlyDictionary<Type, IReadOnlyList<Entry>> entriesByType,
        IReadOnlyDictionary<Type, IReadOnlyList<Observable>> observablesByType,
        IReadOnlyDictionary<Type, IReadOnlyList<Observable>> assetObservablesByType,
        IReadOnlyList<Observable> observables,
        string assetCodecsMaterial)
    {
        _entriesByType = entriesByType;
        _observablesByType = observablesByType;
        _assetObservablesByType = assetObservablesByType;
        _observablesById = ById(observables);
        _assetCodecsMaterial = assetCodecsMaterial;
        Observables = observables;
        Rules = [.. entriesByType.Values.SelectMany(entries => entries).Select(entry => entry.Rule)];
    }

    /// <summary>
    /// Indexes every observable by ID, rejecting a collision outright: the flat catalogue is what
    /// <see cref="DigestOf"/> and every inventory record are keyed on, so two observables sharing an
    /// ID would silently record one over the other.
    /// </summary>
    private static Dictionary<string, Observable> ById(IReadOnlyList<Observable> observables)
    {
        var byId = new Dictionary<string, Observable>();
        foreach (var observable in observables)
            if (!byId.TryAdd(observable.Id, observable))
                throw new InvalidOperationException(
                    $"Two observables share the id '{observable.Id}'. Observable IDs must be unique across the catalogue.");

        return byId;
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
    /// <returns>Every value the matching observables yielded.</returns>
    public IEnumerable<Observation> Observe(Block subject)
    {
        if (!_observablesByType.TryGetValue(subject.GetType(), out var observables)) yield break;

        var source = new BlockObservationSource(subject);
        foreach (var observable in observables)
            foreach (var value in observable.Select(source))
                yield return new Observation(observable.Id, value, GroupKey: null);
    }

    /// <summary>
    /// Projects <paramref name="subject"/> through every <see cref="Observable"/> declared for its
    /// runtime type, on either surface.
    /// </summary>
    /// <remarks>
    /// An observable declared on a member the subject doesn't carry - a base type on an asset that
    /// failed to parse and degraded past <see cref="BaseAsset"/> - yields nothing, which is the
    /// faithful record: the bytes were never read, so there is no value to record.
    /// </remarks>
    /// <param name="subject">The asset to observe.</param>
    /// <returns>Every value the matching observables yielded, with the key each is grouped under.</returns>
    public IEnumerable<Observation> Observe(Asset subject)
    {
        if (!_assetObservablesByType.TryGetValue(subject.GetType(), out var observables)) yield break;

        var source = new AssetObservationSource(subject);
        foreach (var observable in observables)
        {
            uint? key = GroupKeyFor(observable.Grouping, subject);
            foreach (var value in observable.Select(source))
                yield return new Observation(observable.Id, value, key);
        }
    }

    /// <summary>
    /// Reads the raw key <paramref name="asset"/>'s occurrences are partitioned under for
    /// <paramref name="grouping"/>. Always a primitive - never an <see cref="AssetType"/> - so a
    /// recorded key stays independent of what the library currently names.
    /// </summary>
    private static uint? GroupKeyFor(ObservableGrouping grouping, Asset asset) => grouping switch
    {
        ObservableGrouping.AssetType => (uint)asset.Type,
        _ => null
    };

    /// <summary>
    /// Looks up a declared <see cref="Observable"/> by its identifier.
    /// </summary>
    /// <param name="observableId">The observable's identifier.</param>
    /// <param name="observable">The observable, if one is declared under that identifier.</param>
    /// <returns><see langword="true"/> if one was found; otherwise <see langword="false"/>.</returns>
    public bool TryGetObservable(string observableId, [NotNullWhen(true)] out Observable? observable) =>
        _observablesById.TryGetValue(observableId, out observable);

    /// <summary>
    /// Produces a digest of what <paramref name="key"/> names, so a fingerprint built from it
    /// changes exactly when that declaration does.
    /// </summary>
    /// <param name="key">
    /// An observable's identifier, or <see cref="AssetCodecsKey"/> for the codec registry.
    /// </param>
    /// <returns>The digest, as a lowercase hex string.</returns>
    public string DigestOf(string key)
    {
        string material = key == AssetCodecsKey ? _assetCodecsMaterial : ObservableMaterial(key);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    /// <summary>
    /// Renders one observable's declaration to the string its digest is taken over. Grouping is
    /// appended only when it isn't <see cref="ObservableGrouping.None"/>, so introducing the axis
    /// leaves every existing observable's digest - and therefore every existing facet's cached map
    /// output - untouched.
    /// </summary>
    private string ObservableMaterial(string key)
    {
        if (!_observablesById.TryGetValue(key, out var observable))
            throw new ArgumentException($"'{key}' is not a known observable.", nameof(key));

        string material = $"{observable.Id}|{observable.Scope}|{observable.Cardinality}|{observable.Presentation}|{observable.Kind}";
        return observable.Grouping is ObservableGrouping.None ? material : $"{material}|{observable.Grouping}";
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

            var noChildren = type.GetCustomAttributes<NoChildrenAttribute>().ToList();
            foreach (var attribute in noChildren)
                entries.Add(BuildNoChildrenEntry(tag, attribute));
            if (noChildren.Count > 0) observables.Add(BuildNoChildrenObservable(tag));

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attributes = property.GetCustomAttributes<ValidationAttribute>().ToList();
                var countByType = attributes.GroupBy(a => a.GetType()).ToDictionary(g => g.Key, g => g.Count());

                foreach (var attribute in attributes)
                {
                    // A property can legally carry more than one attribute of the same kind - disjoint
                    // per-game allowed-value sets, for instance - so only attributes that actually
                    // collide with a sibling get a scope suffix. A single attribute of its kind keeps
                    // the plain id every existing rule reference and recorded corpus id assumes.
                    string? scopeSuffix = countByType[attribute.GetType()] > 1 ? ScopeSuffix(attribute) : null;
                    var entry = BuildPropertyEntry(tag, property, attribute, scopeSuffix);
                    if (entry is { } value) entries.Add(value);
                }

                if (BuildObservable(tag, property, attributes) is { } observable) observables.Add(observable);
            }

            var collidingIds = entries.GroupBy(entry => entry.Rule.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (collidingIds.Count > 0)
                throw new InvalidOperationException(
                    $"{type.Name} declares rules with colliding IDs: {string.Join(", ", collidingIds)}. " +
                    "Scope each attribute (Games, From/To, Quirks, or Platforms) so they don't overlap.");

            if (entries.Count > 0) entriesByType[type] = entries;
            if (observables.Count > 0) observablesByType[type] = observables;
        }

        var assetObservables = BuildAssetObservables();
        var assetObservablesByType = typeof(Asset).Assembly.GetTypes()
            .Where(type => typeof(Asset).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => (Type: type, Observables: MatchingObservables(assetObservables, type)))
            .Where(match => match.Observables.Count > 0)
            .ToDictionary(match => match.Type, match => match.Observables);

        IReadOnlyList<Observable> allObservables =
        [
            .. observablesByType.Values.SelectMany(o => o),
            .. assetObservables.Select(declaration => declaration.Observable)
        ];

        return new ValidationCatalogue(
            entriesByType, observablesByType, assetObservablesByType, allObservables,
            string.Join('\n', AssetCodecs.Registrations));
    }

    /// <summary>
    /// One asset-scoped observable and the type that declared it - an <see cref="Asset"/> subclass
    /// for a logical member, an <c>IPhysical*</c> interface for a physical one.
    /// </summary>
    private readonly record struct AssetObservableDeclaration(Type DeclaringType, Observable Observable);

    /// <summary>
    /// Every observable a concrete asset type carries. Unlike a block, whose observables are looked
    /// up by its exact runtime type, an asset inherits declarations from every level of its
    /// hierarchy and every physical surface it implements - and the type that carries them is
    /// usually a <c>Generic*</c> shape class rather than one named for the asset type.
    /// </summary>
    private static IReadOnlyList<Observable> MatchingObservables(
        IReadOnlyList<AssetObservableDeclaration> declarations, Type assetType) =>
        [.. declarations.Where(d => d.DeclaringType.IsAssignableFrom(assetType)).Select(d => d.Observable)];

    /// <summary>
    /// Reflects over every asset-scoped declaration site - the <see cref="Asset"/> hierarchy for
    /// logical members, the <see cref="IPhysicalAsset"/> interfaces for physical ones - and builds
    /// one <see cref="Observable"/> per member per declared granularity.
    /// </summary>
    private static IReadOnlyList<AssetObservableDeclaration> BuildAssetObservables()
    {
        var declarationSites = typeof(Asset).Assembly.GetTypes()
            .Where(type => typeof(Asset).IsAssignableFrom(type) ||
                           (type.IsInterface && typeof(IPhysicalAsset).IsAssignableFrom(type)))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        var declarations = new List<AssetObservableDeclaration>();

        foreach (var site in declarationSites)
            foreach (var property in site.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var observed = property.GetCustomAttributes<ObservedAttribute>().ToList();
                if (observed.Count == 0) continue;

                foreach (var grouping in observed.Select(o => o.By).Distinct())
                    declarations.Add(new AssetObservableDeclaration(site, BuildAssetObservable(site, property, observed, grouping)));
            }

        return declarations;
    }

    private static Observable BuildAssetObservable(
        Type declaringType, PropertyInfo property, IReadOnlyList<ObservedAttribute> observed, ObservableGrouping grouping)
    {
        var declared = observed.FirstOrDefault(o => o.By == grouping && o.IsCardinalityDeclared);
        if (declared is null)
            throw new InvalidOperationException(
                $"{declaringType.Name}.{property.Name} is asset-scoped, so its [Observed] must state a Cardinality. " +
                "Hundreds of thousands of assets sit behind one declaration; inferring it from the member's type " +
                "is how a value set becomes unreviewable.");

        var (_, presentation) = InferObservableShape(property.PropertyType);

        return new Observable(
            AssetObservableId(declaringType, property, grouping), ObservableScope.Asset, declared.Cardinality,
            presentation, BuildAssetSelector(declaringType, property), ObservableKind.FieldValue, grouping,
            KeyPresentationFor(grouping));
    }

    /// <summary>
    /// How a group's key renders, read from the key's own type rather than declared, so it can't
    /// disagree with how the same value renders anywhere else.
    /// </summary>
    private static ObservablePresentation? KeyPresentationFor(ObservableGrouping grouping) => grouping switch
    {
        ObservableGrouping.AssetType => InferObservableShape(typeof(AssetType)).Presentation,
        _ => null
    };

    /// <summary>
    /// Builds an asset-scoped observable's identifier from the member it reads, so the two can never
    /// disagree about which field they mean.
    /// </summary>
    /// <remarks>
    /// The shape is <c>&lt;assetClass&gt;[.physical].&lt;member&gt;[@&lt;grouping&gt;]</c>. The
    /// <c>physical</c> segment is what keeps <see cref="Asset.Type"/> and
    /// <see cref="IPhysicalAsset.Type"/> - two members that can legitimately disagree - from claiming
    /// one identifier, and it mirrors the <c>asset.Physical.Alignment</c> path a reader would type.
    /// The <c>@</c> separator cannot appear in a C# identifier, so a grouped identifier is provably
    /// distinct from every ungrouped one.
    /// </remarks>
    private static string AssetObservableId(Type declaringType, PropertyInfo property, ObservableGrouping grouping)
    {
        const string physicalPrefix = "IPhysical";

        bool isPhysical = declaringType.IsInterface && declaringType.Name.StartsWith(physicalPrefix, StringComparison.Ordinal);
        string assetClass = isPhysical ? declaringType.Name[physicalPrefix.Length..] : declaringType.Name;
        string surface = isPhysical ? ".physical" : "";
        string suffix = grouping is ObservableGrouping.None ? "" : $"@{CamelCase(grouping.ToString())}";

        return $"{CamelCase(assetClass)}{surface}.{CamelCase(property.Name)}{suffix}";
    }

    /// <summary>
    /// Reads an asset-scoped member, routing a physical one through <see cref="Asset.Physical"/>
    /// rather than casting the asset directly, so an asset whose physical surface is a separate
    /// object is still read correctly.
    /// </summary>
    private static Func<ObservationSource, IEnumerable<object>> BuildAssetSelector(Type declaringType, PropertyInfo property)
    {
        var accessor = CompileAssetAccessor(declaringType, property);

        return source =>
        {
            if (source is not AssetObservationSource(var asset) || !declaringType.IsInstanceOfType(asset))
                return [];

            object? value = ToPrimitive(accessor(asset));
            return value is null ? [] : [value];
        };
    }

    private static Func<Asset, object?> CompileAssetAccessor(Type declaringType, PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(Asset), "asset");
        Expression subject = declaringType.IsInterface
            ? Expression.Convert(Expression.Property(parameter, nameof(Asset.Physical)), declaringType)
            : Expression.Convert(parameter, declaringType);

        var boxed = Expression.Convert(Expression.Property(subject, property), typeof(object));
        return Expression.Lambda<Func<Asset, object?>>(boxed, parameter).Compile();
    }

    private static string CamelCase(string name) => $"{char.ToLowerInvariant(name[0])}{name[1..]}";

    private static Observable? BuildObservable(string tag, PropertyInfo property, IReadOnlyList<ValidationAttribute> attributes)
    {
        // special handling for attributes on helpers, since they aren't primitives like others
        if (attributes.OfType<RequiredChildAttribute>().Any())
            return BuildRequiredChildObservable(tag, property);

        bool isValue = attributes.Any(a =>
            a is ObservedAttribute or ConstantValueAttribute or AllowedValuesAttribute or ClosedEnumAttribute
                or DefinedBitsAttribute or RequiredBitsAttribute);
        if (!isValue) return null;

        var (inferred, presentation) = InferObservableShape(property.PropertyType);
        var declared = attributes.OfType<ObservedAttribute>().FirstOrDefault(o => o.IsCardinalityDeclared);

        return new Observable(
            ObservableId(tag, property), ObservableScope.Block, declared?.Cardinality ?? inferred, presentation,
            BuildSelector(property));
    }

    private static Observable BuildRequiredChildObservable(string tag, PropertyInfo property)
    {
        Type childType = property.PropertyType;
        Type declaringType = property.DeclaringType!;

        return new Observable(
            ObservableId(tag, property), ObservableScope.Block, ObservableCardinality.Enumerated, ObservablePresentation.Number,
            source => source is BlockObservationSource(var block) && declaringType.IsInstanceOfType(block)
                ? [(long)block.Children.Count(childType.IsInstanceOfType)]
                : [],
            ObservableKind.Structural);
    }

    /// <summary>
    /// Every <c>[NoChildren]</c>-attributed type gets one structural observable recording its own
    /// child count, the same value <see cref="NoChildrenRule"/> checks - built once per type
    /// regardless of how many scoped <c>[NoChildren]</c> attributes it carries, since they all check
    /// the same fact.
    /// </summary>
    private static Observable BuildNoChildrenObservable(string tag) =>
        new(
            $"{tag}.childCount", ObservableScope.Block, ObservableCardinality.Enumerated, ObservablePresentation.Number,
            source => source is BlockObservationSource(var block) ? [(long)block.Children.Count] : [],
            ObservableKind.Structural);

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

    /// <summary>
    /// Reduces a member's value to the primitive an observable yields: a whole number, a string, a
    /// bool, or bytes.
    /// </summary>
    /// <remarks>
    /// Every whole number widens to <see cref="long"/>, one canonical type, for two reasons. It is
    /// wide enough to hold every field the format has without losing the sign of one that is
    /// meaningfully negative - <see cref="AssetDebug.Alignment"/> stores -1 to mean "use this type's
    /// default". And it means a value read fresh from an archive and the same value read back from a
    /// recorder's cache are always the same CLR type, so they compare equal.
    /// </remarks>
    private static object? ToPrimitive(object? value)
    {
        object? underlying = value is Enum e ? Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType())) : value;

        return underlying switch
        {
            sbyte or byte or short or ushort or int or uint or long => Convert.ToInt64(underlying),
            ulong u => checked((long)u),
            _ => underlying
        };
    }

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

    private static string ObservableId(string tag, PropertyInfo property) => $"{tag}.{CamelCase(property.Name)}";

    private static string ReadTag(Type blockType) =>
        ((Block)Activator.CreateInstance(blockType, nonPublic: true)!).Tag;

    private static Entry BuildNoChildrenEntry(string tag, NoChildrenAttribute attribute)
    {
        string id = $"{tag.ToLowerInvariant()}-no-children";
        string description = $"{tag} has no children.";
        var rule = new NoChildrenRule(id, attribute.Severity, description, tag, attribute);

        return new Entry(rule, Member: null, Accessor: block => block.Children.Count);
    }

    private static Entry? BuildPropertyEntry(string tag, PropertyInfo property, ValidationAttribute attribute, string? scopeSuffix)
    {
        string member = property.Name;
        string observableId = ObservableId(tag, property);
        string idBase = $"{tag.ToLowerInvariant()}.{member.ToLowerInvariant()}{(scopeSuffix is null ? "" : $"@{scopeSuffix}")}";

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

    /// <summary>
    /// Renders <paramref name="attribute"/>'s scoping axes into a short, deterministic id suffix, so
    /// two same-kind attributes stacked on one property - disjoint per-game allowed-value sets, for
    /// instance - get distinct rule IDs instead of silently colliding.
    /// </summary>
    private static string? ScopeSuffix(ValidationAttribute attribute)
    {
        var parts = new List<string>();

        if (attribute.Games.Length > 0)
            parts.Add(string.Join("+", attribute.Games.OrderBy(g => g).Select(g => g.ToString().ToLowerInvariant())));
        else if (attribute.From != GameVersion.N100F || attribute.To != GameVersion.Ratatouille)
        {
            string from = attribute.From.ToString().ToLowerInvariant();
            string to = attribute.To.ToString().ToLowerInvariant();
            parts.Add(
                attribute.From != GameVersion.N100F && attribute.To != GameVersion.Ratatouille ? $"{from}-{to}" :
                attribute.From != GameVersion.N100F ? $"{from}+" : $"-{to}");
        }

        if (attribute.Quirks != FormatQuirks.None)
            parts.Add(attribute.Quirks.ToString().ToLowerInvariant());

        if (attribute.Platforms.Length > 0)
            parts.Add(string.Join("+", attribute.Platforms.OrderBy(p => p).Select(p => p.ToString().ToLowerInvariant())));

        return parts.Count > 0 ? string.Join("-", parts) : null;
    }
}

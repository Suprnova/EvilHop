using EvilHop.Common;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace EvilHop.Assets;

/// <summary>
/// Finds the asset IDs an asset names by walking its members: every <see cref="AssetId"/>, every
/// collection of them, and the same inside nested EvilHop types and their collections.
/// </summary>
/// <remarks>
/// A member's type is what says it holds an asset ID, so a new typed asset is covered without
/// listing its fields. Public members are walked, and the members of a <see cref="Physical"/>
/// interface that the type implements explicitly, under the path <c>Physical.</c>; the trait
/// interfaces (<see cref="IHasModel"/> and the like) only alias those. Skipped are the asset's own
/// <see cref="Asset.Id"/>, <see cref="Asset.Layer"/>, members marked
/// <see cref="OwnAssetIdAttribute"/>, and every <see cref="AssetId.None"/>.
/// </remarks>
internal static class AssetReferenceWalker
{
    private sealed record Member(string Path, PropertyInfo Property);

    private static readonly Type[] LibraryTypes = typeof(Asset).Assembly.GetTypes();
    private static readonly ConcurrentDictionary<Type, Member[]> CandidatesByType = new();
    private static readonly ConcurrentDictionary<Type, Member[]> MembersByType = new();
    private static readonly ConcurrentDictionary<Type, bool> ReachesByType = new();

    /// <summary>
    /// Calls <paramref name="found"/> with the member path and value of every asset ID
    /// <paramref name="asset"/> names, in member order.
    /// </summary>
    internal static void Walk(Asset asset, Action<string, AssetId> found) => WalkObject(asset, "", found);

    private static void WalkObject(object value, string prefix, Action<string, AssetId> found)
    {
        foreach (var member in MembersOf(value.GetType()))
            WalkValue(member.Property.GetValue(value), prefix + member.Path, found);
    }

    private static void WalkValue(object? value, string path, Action<string, AssetId> found)
    {
        switch (value)
        {
            case null:
                return;
            case AssetId id:
                if (id != AssetId.None) found(path, id);
                return;
            case IEnumerable items:
                int index = 0;
                foreach (object? item in items)
                    WalkValue(item, $"{path}[{index++}]", found);
                return;
            default:
                WalkObject(value, path + ".", found);
                return;
        }
    }

    /// <summary>The members of <paramref name="type"/> that can hold an asset ID.</summary>
    private static Member[] MembersOf(Type type) => MembersByType.GetOrAdd(type, static type =>
        [.. CandidatesOf(type).Where(member => Reaches(member.Property.PropertyType))]);

    /// <summary>The members of <paramref name="type"/> the walk reads, whatever their type.</summary>
    private static Member[] CandidatesOf(Type type) => CandidatesByType.GetOrAdd(type, static type =>
    {
        if (!IsLibraryType(type)) return [];

        var members = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0 && !IsSkipped(type, property))
            .Select(property => new Member(property.Name, property));

        var physical = type.GetInterfaces()
            .Where(face => face.DeclaringType == typeof(Physical))
            .SelectMany(face => ExplicitMembers(type, face))
            .Where(property => property.GetCustomAttribute<OwnAssetIdAttribute>() is null)
            .Select(property => new Member($"{nameof(Physical)}.{property.Name}", property));

        return [.. members, .. physical];
    });

    private static bool IsSkipped(Type type, PropertyInfo property) =>
        property.Name is nameof(Physical)
        || typeof(Asset).IsAssignableFrom(type) && property.Name is nameof(Asset.Id) or nameof(Asset.Layer);

    /// <summary>
    /// The properties of <paramref name="face"/> that <paramref name="type"/> implements explicitly;
    /// an implicit implementation is a public member, which the walk already reads.
    /// </summary>
    private static IEnumerable<PropertyInfo> ExplicitMembers(Type type, Type face)
    {
        if (type.IsInterface) return [];

        var map = type.GetInterfaceMap(face);
        var implementations = map.InterfaceMethods
            .Zip(map.TargetMethods)
            .ToDictionary(pair => pair.First, pair => pair.Second);

        return face.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(property => property.GetMethod is { } getter && !implementations[getter].IsPublic);
    }

    /// <summary>
    /// Whether a value declared as <paramref name="type"/> can hold an asset ID: directly, in a
    /// collection, or inside an EvilHop type or any concrete EvilHop type that derives from it.
    /// </summary>
    private static bool Reaches(Type type) => ReachesByType.GetOrAdd(type, static type => Search(type, []));

    private static bool Search(Type type, HashSet<Type> visited)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(AssetId)) return true;
        if (type == typeof(string) || !visited.Add(type)) return false;
        if (ElementTypeOf(type) is { } element) return Search(element, visited);

        return IsLibraryType(type) && LibraryTypes
            .Where(candidate => !candidate.IsAbstract && !candidate.IsInterface && type.IsAssignableFrom(candidate))
            .Any(candidate => CandidatesOf(candidate).Any(member => Search(member.Property.PropertyType, visited)));
    }

    private static Type? ElementTypeOf(Type type) =>
        (IsEnumerableOfT(type) ? type : type.GetInterfaces().FirstOrDefault(IsEnumerableOfT))?.GetGenericArguments()[0];

    private static bool IsEnumerableOfT(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    private static bool IsLibraryType(Type type) =>
        type.Assembly == typeof(Asset).Assembly
        && !type.IsEnum
        && !typeof(Layer).IsAssignableFrom(type)
        && type != typeof(AssetId);
}

using EvilHop.Blocks;
using System.Collections.Concurrent;
using System.Reflection;

namespace EvilHop.Corpus.Extraction;

/// <summary>
/// Walks the public properties of a <see cref="Block"/> type to find the ones worth inventorying.
/// No per-type code - the same reflection rules cover every block type, and later, every asset type.
/// </summary>
internal static class FieldExtractor
{
    // Concurrent because callers include parallel test runners, not just the tool's own single-
    // threaded archive loop - a plain Dictionary corrupts under concurrent first-touch writes.
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Cache = new();

    /// <summary>
    /// Returns the inventoried properties of <paramref name="blockType"/>, cached per type.
    /// </summary>
    public static PropertyInfo[] GetFields(Type blockType) => Cache.GetOrAdd(blockType, static type =>
        [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(IsFieldWorthy)]);

    /// <summary>
    /// Invokes <paramref name="property"/>'s getter on <paramref name="instance"/>, defensively.
    /// A throwing getter (e.g. a missing <c>GetRequiredChild&lt;T&gt;()</c> child on a malformed
    /// archive) is reported to <see cref="Console.Error"/> and skipped rather than aborting the run.
    /// </summary>
    public static bool TryGetValue(PropertyInfo property, Block instance, out object? value)
    {
        try
        {
            value = property.GetValue(instance);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Skipping {instance.GetType().Name}.{property.Name}: {(ex.InnerException ?? ex).Message}");
            value = null;
            return false;
        }
    }

    private static bool IsFieldWorthy(PropertyInfo property)
    {
        if (property.GetGetMethod() == null || property.GetSetMethod() == null) return false;

        var type = property.PropertyType;
        if (typeof(Block).IsAssignableFrom(type)) return false;

        var elementType = GetEnumerableElementType(type);
        return elementType == null || !typeof(Block).IsAssignableFrom(elementType);
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type == typeof(string)) return null;

        var enumerableInterface = type.GetInterfaces().Prepend(type)
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0];
    }
}

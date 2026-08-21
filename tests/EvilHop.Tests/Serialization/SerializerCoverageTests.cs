using EvilHop.Serialization;
using System.Reflection;

namespace EvilHop.Tests.Serialization;

/// <summary>
/// Closes the one gap <see cref="SerializerContractTests"/>'s per-serializer subclassing leaves
/// open: nothing forces a contributor adding a new game <see cref="Serializer"/> to also write its
/// contract test subclass, and forgetting it would otherwise be silent zero coverage.
/// </summary>
public class SerializerCoverageTests
{
    [Fact]
    public void EveryConcreteSerializer_HasAMatchingContractTestsSubclass()
    {
        var serializerTypes = typeof(Serializer).Assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(Serializer)) && !t.IsAbstract);

        var coveredTypes = typeof(SerializerContractTests).Assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(SerializerContractTests)) && !t.IsAbstract)
            .Select(t =>
            {
                var createSerializer = t.GetMethod("CreateSerializer", BindingFlags.NonPublic | BindingFlags.Instance)!;
                var instance = Activator.CreateInstance(t)!;
                return ((Serializer)createSerializer.Invoke(instance, null)!).GetType();
            })
            .ToHashSet();

        var uncovered = serializerTypes.Where(t => !coveredTypes.Contains(t)).Select(t => t.Name).ToList();

        Assert.True(uncovered.Count == 0, $"No SerializerContractTests subclass covers: {string.Join(", ", uncovered)}");
    }
}

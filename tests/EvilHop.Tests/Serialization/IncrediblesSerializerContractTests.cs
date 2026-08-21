using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class IncrediblesSerializerContractTests : SerializerContractTests
{
    protected override Serializer CreateSerializer() => new IncrediblesSerializer();

    protected override Stream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "incredibles", "minimal.hip"));
}

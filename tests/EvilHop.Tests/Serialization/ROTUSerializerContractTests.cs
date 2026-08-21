using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class ROTUSerializerContractTests : SerializerContractTests
{
    protected override Serializer CreateSerializer() => new ROTUSerializer();

    protected override Stream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "rotu", "minimal.hip"));
}

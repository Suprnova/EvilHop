using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class RatatouilleSerializerContractTests : SerializerContractTests
{
    protected override Serializer CreateSerializer() => new RatatouilleSerializer();

    protected override Stream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "ratatouille", "minimal.hip"));
}

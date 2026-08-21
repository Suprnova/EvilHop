using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class TSSMSerializerContractTests : SerializerContractTests
{
    protected override Serializer CreateSerializer() => new TSSMSerializer();

    protected override Stream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "tssm", "minimal.hip"));
}

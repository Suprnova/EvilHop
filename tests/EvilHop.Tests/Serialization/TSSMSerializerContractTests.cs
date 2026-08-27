using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class TSSMSerializerContractTests : SerializerContractTests
{
    protected override Serializer CreateSerializer() => new TSSMSerializer();

    protected override FileStream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "tssm", "minimal.hip"));
}

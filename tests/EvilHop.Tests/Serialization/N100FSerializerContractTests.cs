using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class N100FSerializerContractTests : SerializerContractTests
{
    protected override Serializer CreateSerializer() => new N100FSerializer();
}

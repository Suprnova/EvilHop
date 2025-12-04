using EvilHop.Blocks;
using EvilHop.Serialization.Validation;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V3Serializer : V2Serializer
{
    protected internal V3Serializer(IFormatValidator validator) : base(validator)
    {
    }

    protected override PackagePlatform InitPackagePlatform()
    {
        throw new NotImplementedException();
    }
}

public partial class MovieSerializer() : V3Serializer(new MovieValidator())
{
}

public partial class IncrediblesSerializer() : V3Serializer(new IncrediblesValidator())
{
}

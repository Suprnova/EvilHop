using EvilHop.Serialization.Validation;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V4Serializer : V3Serializer
{
    protected internal V4Serializer(IFormatValidator validator) : base(validator)
    {
    }
}

public partial class ROTUSerializer() : V4Serializer(new ROTUValidator())
{
}

public partial class RatSerializer() : V4Serializer(new RatValidator())
{
}

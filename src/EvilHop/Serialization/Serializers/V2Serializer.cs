using EvilHop.Serialization.Validation;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V2Serializer : V1Serializer
{
    protected internal V2Serializer(IFormatValidator validator) : base(validator)
    {
        Register("PLAT", (r, l) => ReadPackagePlatform(r), WritePackagePlatform);
    }
}

// todo: replace with BattleValidator
public partial class BattleSerializer() : V2Serializer(new ScoobyValidator())
{
}

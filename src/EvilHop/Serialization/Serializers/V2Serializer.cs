using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization.Validation;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V2Serializer : V1Serializer
{
    public V2Serializer()
    {
        Register("PLAT", (r, l) => ReadPackagePlatform(r), WritePackagePlatform);
    }
}

public partial class BattleSerializer : V2Serializer
{
}

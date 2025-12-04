using EvilHop.Blocks;
using EvilHop.Serialization.Validation;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V2Serializer : V1Serializer
{
    protected internal V2Serializer(IFormatValidator validator) : base(validator)
    {
        RegisterBlock("PLAT", InitPackagePlatform, (r, l) => ReadPackagePlatform(r), WritePackagePlatform);
    }
}

public partial class BattleSerializer() : V2Serializer(new BattleValidator())
{
    protected override PackagePlatform InitPackagePlatform()
    {
        return new PackagePlatform("GC", "GameCube", "NTSC", "US Common", "Sponge Bob");
    }
}

using EvilHop.Blocks;
using EvilHop.Serialization.Validation;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V2Serializer : V1Serializer
{
    protected internal V2Serializer(IFormatValidator validator) : base(validator)
    {
        Register("PLAT", InitPackagePlatform, (r, l) => ReadPackagePlatform(r), WritePackagePlatform);
    }

    // todo: will be abstract, implemented per serializer
    protected virtual PackagePlatform InitPackagePlatform()
    {
        throw new NotImplementedException();
    }

    protected override PackageVersion InitPackageVersion() => new(ClientVersion.Default);
}

// todo: replace with BattleValidator
public partial class BattleSerializer() : V2Serializer(new ScoobyValidator())
{
}

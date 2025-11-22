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

    //private readonly V2Validator _validator = new V2Validator();
    public override IEnumerable<ValidationIssue> Validate(Block block)
    {
        yield break;
    }

    public override void Write(BinaryWriter writer, Block block)
    {
        writer.Write(block.Id.ToEvilBytes()[..^2]);
        writer.Write(GetBlockLength(block).ToEvilBytes());

        if (_writeFactory.TryGetValue(block.GetType(), out var handler)) handler(writer, block);
    }
}

public partial class BattleSerializer : V2Serializer
{
}

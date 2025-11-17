using System.Text;
using EvilHop.Extensions;

namespace EvilHop.Blocks;

public abstract class Block
{
    public string Id { get; }
    public uint Length { get; set; }
    public List<Block> Children;

    public Block(string id, uint length, List<Block> children)
    {
        Id = id;
        Length = length;
        Children = children;
    }

    public Block(uint id, uint length, List<Block> children)
    {
        Id = Encoding.ASCII.GetString(id.ToEvilBytes());
        Length = length;
        Children = children;
    }

    // TODO: pass file format for blocks that have dependent styles
    public virtual byte[] ToSpan()
    {
        // TODO: learn more about span to make everything more efficient
        Span<byte> bytes = stackalloc byte[8];
        // TODO: implement
        return bytes.ToArray();
    }
}

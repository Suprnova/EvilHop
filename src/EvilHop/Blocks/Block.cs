using System.Text;
using EvilHop.Extensions;

namespace EvilHop.Blocks;

public abstract class Block
{
    protected string _id;
    protected uint _length;
    protected List<Block> _children;

    public Block(string id, uint length, List<Block> children)
    {
        _id = id;
        _length = length;
        _children = children;
    }

    public Block(uint id, uint length, List<Block> children)
    {
        _id = Encoding.ASCII.GetString(id.ToEvilBytes());
        _length = length;
        _children = children;
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

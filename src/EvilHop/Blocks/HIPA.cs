using EvilHop.Extensions;

namespace EvilHop.Blocks;

public class HIPA : Block
{
    public HIPA() : base("HIPA", 0, [])
    {
    }

    public HIPA(BinaryReader reader) : base(reader.ReadEvilInt(), reader.ReadEvilInt(), [])
    {
        if (!this._id.Equals("HIPA")) throw new ArgumentException("Invalid magic number; not a HIPA block.");
    }
}


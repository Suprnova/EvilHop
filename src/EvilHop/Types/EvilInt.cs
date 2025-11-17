using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace EvilHop.Types;

public sealed class EvilInt : IEquatable<EvilInt>
{
    public UInt32 Value { get; set; }

    public EvilInt(Span<byte> bytes)
    {
        // maybe only warn about this? it's concerning, but it's not the end of the world
        Value = BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    public EvilInt(UInt32 value)
    {
        Value = value;
    }

    public bool Equals(EvilInt? other)
    {
        return this.Value == other.Value;
    }

    // allows "EvilInt num = 1;"
    public static implicit operator EvilInt(UInt32 value) => new EvilInt(value);
}

using EvilHop.Primitives;
using System.Buffers.Binary;

namespace EvilHop.Assets;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// TODO: I'll write the comments later
// TODO: Should we enforce a max length of 4 bytes? If so, where and how?

public abstract class Parameter
{
    private protected Parameter() { }

    internal abstract void WriteTo(Span<byte> destination);
}

public sealed class RawParameter(byte[] bytes) : Parameter
{
    public byte[] Bytes { get; set; } = bytes;

    internal override void WriteTo(Span<byte> destination) => Bytes.CopyTo(destination);
}

public sealed class FloatParameter(float value) : Parameter
{
    public float Value { get; set; } = value;

    internal override void WriteTo(Span<byte> destination) =>
        BinaryPrimitives.WriteSingleBigEndian(destination, Value);
}

public sealed class IntParameter(int value) : Parameter
{
    public int Value { get; set; } = value;

    internal override void WriteTo(Span<byte> destination) =>
        BinaryPrimitives.WriteInt32BigEndian(destination, Value);
}

public sealed class AssetIdParameter(AssetId value) : Parameter
{
    public AssetId Value { get; set; } = value;

    internal override void WriteTo(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, Value.Value);
}

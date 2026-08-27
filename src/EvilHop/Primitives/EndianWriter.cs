using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace EvilHop.Primitives;

/// <summary>
/// A <see cref="BinaryWriter"/> that writes multi-byte primitives in a fixed <see cref="Primitives.Endianness"/>,
/// decided once at construction.
/// </summary>
/// <param name="output">The stream to write to.</param>
/// <param name="endianness">The byte order to write multi-byte fields in.</param>
/// <param name="leaveOpen">Whether or not <paramref name="output"/> is left open after this writer is disposed.</param>
public sealed class EndianWriter(Stream output, Endianness endianness, bool leaveOpen = false)
    : BinaryWriter(output, Encoding.ASCII, leaveOpen)
{
    /// <summary>The byte order this writer was constructed with.</summary>
    public Endianness Endianness { get; } = endianness;

    /// <inheritdoc/>
    public override void Write(short value)
    {
        Span<byte> bytes = stackalloc byte[2];
        if (Endianness == Endianness.Big) BinaryPrimitives.WriteInt16BigEndian(bytes, value);
        else BinaryPrimitives.WriteInt16LittleEndian(bytes, value);
        Write(bytes);
    }

    /// <inheritdoc/>
    public override void Write(int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (Endianness == Endianness.Big) BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        else BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        Write(bytes);
    }

    /// <inheritdoc/>
    public override void Write(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (Endianness == Endianness.Big) BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        else BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        Write(bytes);
    }

    /// <inheritdoc/>
    public override void Write(float value)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (Endianness == Endianness.Big) BinaryPrimitives.WriteSingleBigEndian(bytes, value);
        else BinaryPrimitives.WriteSingleLittleEndian(bytes, value);
        Write(bytes);
    }

    /// <summary>Writes a <see cref="Vector3"/> as three consecutive <see cref="Write(float)"/>s.</summary>
    public void Write(Vector3 value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
    }

    /// <summary>Writes an <see cref="AssetId"/>.</summary>
    public void Write(AssetId value) => Write(value.Value);
}

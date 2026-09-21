using EvilHop.Common;
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
    public override void Write(ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        if (Endianness == Endianness.Big) BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        else BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
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

    /// <inheritdoc/>
    public override void Write(double value)
    {
        Span<byte> bytes = stackalloc byte[8];
        if (Endianness == Endianness.Big) BinaryPrimitives.WriteDoubleBigEndian(bytes, value);
        else BinaryPrimitives.WriteDoubleLittleEndian(bytes, value);
        Write(bytes);
    }

    /// <inheritdoc/>
    public override void Write(long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        if (Endianness == Endianness.Big) BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        else BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        Write(bytes);
    }

    /// <inheritdoc/>
    public override void Write(ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        if (Endianness == Endianness.Big) BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        else BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        Write(bytes);
    }

    /// <summary>Writes a <see cref="Vector2"/> as two consecutive <see cref="Write(float)"/>s.</summary>
    public void Write(Vector2 value)
    {
        Write(value.X);
        Write(value.Y);
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

    /// <summary>Writes an <see cref="Rgb"/> as three consecutive <see cref="Write(float)"/>s.</summary>
    public void Write(Rgb value)
    {
        Write(value.R);
        Write(value.G);
        Write(value.B);
    }

    /// <summary>Writes an <see cref="Rgb"/> as three consecutive bytes, each channel scaled from 0-1 to 0-255.</summary>
    public void WriteRgb24(Rgb value)
    {
        Write(ToByte(value.R));
        Write(ToByte(value.G));
        Write(ToByte(value.B));
    }

    /// <summary>Writes an <see cref="Rgba"/> as four consecutive <see cref="Write(float)"/>s.</summary>
    public void Write(Rgba value)
    {
        Write(value.R);
        Write(value.G);
        Write(value.B);
        Write(value.A);
    }

    /// <summary>Writes an <see cref="Rgba"/> as four consecutive bytes, each channel scaled from 0-1 to 0-255.</summary>
    public void WriteRgba32(Rgba value)
    {
        Write(ToByte(value.R));
        Write(ToByte(value.G));
        Write(ToByte(value.B));
        Write(ToByte(value.A));
    }

    private static byte ToByte(float channel) => (byte)Math.Clamp(MathF.Round(channel * 255f), 0f, 255f);
}

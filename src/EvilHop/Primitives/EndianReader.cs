using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Common;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace EvilHop.Primitives;

/// <summary>
/// A <see cref="BinaryReader"/> that reads multi-byte primitives in a fixed <see cref="Primitives.Endianness"/>,
/// decided once at construction.
/// </summary>
/// <param name="input">The stream to read from.</param>
/// <param name="endianness">The byte order to read multi-byte fields in.</param>
/// <param name="leaveOpen">Whether or not <paramref name="input"/> is left open after this reader is disposed.</param>
public sealed class EndianReader(Stream input, Endianness endianness, bool leaveOpen = false)
    : BinaryReader(input, Encoding.ASCII, leaveOpen)
{
    /// <summary>The byte order this reader was constructed with.</summary>
    public Endianness Endianness { get; } = endianness;

    /// <summary>Records each read as a field, when a <see cref="FieldMap"/> is being built.</summary>
    internal FieldRecorder? Recorder { get; set; }

    /// <summary>
    /// A reader over the same stream in <paramref name="endianness"/>, recorded alongside this one:
    /// this reader when it already reads in that order.
    /// </summary>
    /// <remarks>Never needs disposing: it leaves the stream open and owns nothing else.</remarks>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Thin, non-owning wrapper over this reader's own stream - see remarks.")]
    internal EndianReader WithEndianness(Endianness endianness) => endianness == Endianness
        ? this
        : new(BaseStream, endianness, leaveOpen: true) { Recorder = Recorder };

    private FieldRead Record(FieldKind kind) => new(Recorder, kind);

    /// <summary>
    /// Reads <paramref name="count"/> bytes, throwing <see cref="EndOfStreamException"/> instead of
    /// silently returning fewer if the stream runs out first - every multi-byte read in this class,
    /// and every codec that reads a fixed-size field, goes through this.
    /// </summary>
    public override byte[] ReadBytes(int count)
    {
        using var read = Record(FieldKind.Bytes);
        byte[] bytes = base.ReadBytes(count);
        return bytes.Length == count
            ? bytes
            : throw new EndOfStreamException($"Expected to read {count} bytes, but only {bytes.Length} remained in the stream.");
    }

    /// <inheritdoc/>
    public override byte ReadByte()
    {
        using var read = Record(FieldKind.U8);
        return base.ReadByte();
    }

    /// <inheritdoc/>
    public override sbyte ReadSByte()
    {
        using var read = Record(FieldKind.S8);
        return base.ReadSByte();
    }

    /// <inheritdoc/>
    public override short ReadInt16()
    {
        using var read = Record(FieldKind.S16);
        return Endianness == Endianness.Big
            ? BinaryPrimitives.ReadInt16BigEndian(ReadBytes(2))
            : BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(2));
    }

    /// <inheritdoc/>
    public override ushort ReadUInt16()
    {
        using var read = Record(FieldKind.U16);
        return Endianness == Endianness.Big
            ? BinaryPrimitives.ReadUInt16BigEndian(ReadBytes(2))
            : BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(2));
    }

    /// <inheritdoc/>
    public override int ReadInt32()
    {
        using var read = Record(FieldKind.S32);
        return Endianness == Endianness.Big
            ? BinaryPrimitives.ReadInt32BigEndian(ReadBytes(4))
            : BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));
    }

    /// <inheritdoc/>
    public override uint ReadUInt32()
    {
        using var read = Record(FieldKind.U32);
        return Endianness == Endianness.Big
            ? BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(4))
            : BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(4));
    }

    /// <inheritdoc/>
    public override float ReadSingle()
    {
        using var read = Record(FieldKind.F32);
        return Endianness == Endianness.Big
            ? BinaryPrimitives.ReadSingleBigEndian(ReadBytes(4))
            : BinaryPrimitives.ReadSingleLittleEndian(ReadBytes(4));
    }

    /// <inheritdoc/>
    public override double ReadDouble() => Endianness == Endianness.Big
        ? BinaryPrimitives.ReadDoubleBigEndian(ReadBytes(8))
        : BinaryPrimitives.ReadDoubleLittleEndian(ReadBytes(8));

    /// <inheritdoc/>
    public override long ReadInt64() => Endianness == Endianness.Big
        ? BinaryPrimitives.ReadInt64BigEndian(ReadBytes(8))
        : BinaryPrimitives.ReadInt64LittleEndian(ReadBytes(8));

    /// <inheritdoc/>
    public override ulong ReadUInt64() => Endianness == Endianness.Big
        ? BinaryPrimitives.ReadUInt64BigEndian(ReadBytes(8))
        : BinaryPrimitives.ReadUInt64LittleEndian(ReadBytes(8));

    /// <summary>Reads two consecutive <see cref="ReadSingle"/>s as a <see cref="Vector2"/>.</summary>
    public Vector2 ReadVector2()
    {
        using var read = Record(FieldKind.Vector2);
        return new(ReadSingle(), ReadSingle());
    }

    /// <summary>Reads three consecutive <see cref="ReadSingle"/>s as a <see cref="Vector3"/>.</summary>
    public Vector3 ReadVector3()
    {
        using var read = Record(FieldKind.Vector3);
        return new(ReadSingle(), ReadSingle(), ReadSingle());
    }

    /// <summary>Reads an <see cref="AssetId"/>.</summary>
    public AssetId ReadAssetId()
    {
        using var read = Record(FieldKind.AssetId);
        return new(ReadUInt32());
    }

    /// <summary>Reads three consecutive <see cref="ReadSingle"/>s as an <see cref="Rgb"/>.</summary>
    public Rgb ReadRgb()
    {
        using var read = Record(FieldKind.Rgb);
        return new(ReadSingle(), ReadSingle(), ReadSingle());
    }

    /// <summary>Reads three consecutive bytes, each scaled from 0-255 to 0-1, as an <see cref="Rgb"/>.</summary>
    public Rgb ReadRgb24()
    {
        using var read = Record(FieldKind.Rgb24);
        return new(ReadByte() / 255f, ReadByte() / 255f, ReadByte() / 255f);
    }

    /// <summary>Reads four consecutive <see cref="ReadSingle"/>s as an <see cref="Rgba"/>.</summary>
    public Rgba ReadRgba()
    {
        using var read = Record(FieldKind.Rgba);
        return new(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
    }

    /// <summary>Reads four consecutive bytes, each scaled from 0-255 to 0-1, as an <see cref="Rgba"/>.</summary>
    public Rgba ReadRgba32()
    {
        using var read = Record(FieldKind.Rgba32);
        return new(ReadByte() / 255f, ReadByte() / 255f, ReadByte() / 255f, ReadByte() / 255f);
    }

    /// <summary>Reads every byte remaining between the current position and the end of the stream.</summary>
    public byte[] ReadRemainingBytes() => ReadBytes((int)(BaseStream.Length - BaseStream.Position));
}

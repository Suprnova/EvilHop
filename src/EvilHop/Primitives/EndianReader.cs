using System.Buffers.Binary;
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

    /// <inheritdoc/>
    public override short ReadInt16() => Endianness == Endianness.Big
        ? BinaryPrimitives.ReadInt16BigEndian(ReadBytes(2))
        : BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(2));

    /// <inheritdoc/>
    public override int ReadInt32() => Endianness == Endianness.Big
        ? BinaryPrimitives.ReadInt32BigEndian(ReadBytes(4))
        : BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));

    /// <inheritdoc/>
    public override uint ReadUInt32() => Endianness == Endianness.Big
        ? BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(4))
        : BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(4));

    /// <inheritdoc/>
    public override float ReadSingle() => Endianness == Endianness.Big
        ? BinaryPrimitives.ReadSingleBigEndian(ReadBytes(4))
        : BinaryPrimitives.ReadSingleLittleEndian(ReadBytes(4));

    /// <summary>Reads three consecutive <see cref="ReadSingle"/>s as a <see cref="Vector3"/>.</summary>
    public Vector3 ReadVector3() => new(ReadSingle(), ReadSingle(), ReadSingle());

    /// <summary>Reads an <see cref="AssetId"/>.</summary>
    public AssetId ReadAssetId() => new(ReadUInt32());

    /// <summary>Reads every byte remaining between the current position and the end of the stream.</summary>
    public byte[] ReadRemainingBytes() => ReadBytes((int)(BaseStream.Length - BaseStream.Position));
}

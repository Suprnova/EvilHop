using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets.Serialization;

/// <summary>
/// Reads that name the field they read, for <see cref="FieldMap"/>. Each costs a null check when no
/// map is being built.
/// </summary>
/// <remarks>
/// A name is a member name, built with <see langword="nameof"/>: a member of a <c>Physical</c>
/// surface is named under <see cref="PhysicalPrefix"/>. A nested type's reads are named inside a
/// <see cref="Field"/> scope, which prefixes the element's path.
/// </remarks>
internal static class FieldReads
{
    /// <summary>The path prefix of a member of a <c>Physical</c> surface.</summary>
    public const string PhysicalPrefix = nameof(Physical) + ".";

    extension(EndianReader reader)
    {
        /// <summary>
        /// Opens the scope of the element at <paramref name="index"/> of <paramref name="name"/>:
        /// the fields read until it is disposed are named under <c>name[index]</c>.
        /// </summary>
        public FieldScope Field(string name, int index)
        {
            reader.Recorder?.Push(name, index);
            return new FieldScope(reader.Recorder);
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadByte"/>.</summary>
        public byte ReadByte(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadByte();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadSByte"/>.</summary>
        public sbyte ReadSByte(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadSByte();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadInt16"/>.</summary>
        public short ReadInt16(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadInt16();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadUInt16"/>.</summary>
        public ushort ReadUInt16(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadUInt16();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadInt32"/>.</summary>
        public int ReadInt32(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadInt32();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadUInt32"/>.</summary>
        public uint ReadUInt32(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadUInt32();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadSingle"/>.</summary>
        public float ReadSingle(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadSingle();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadVector2"/>.</summary>
        public Vector2 ReadVector2(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadVector2();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadVector3"/>.</summary>
        public Vector3 ReadVector3(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadVector3();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadRgb"/>.</summary>
        public Rgb ReadRgb(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadRgb();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadRgb24"/>.</summary>
        public Rgb ReadRgb24(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadRgb24();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadRgba"/>.</summary>
        public Rgba ReadRgba(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadRgba();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadRgba32"/>.</summary>
        public Rgba ReadRgba32(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadRgba32();
        }

        /// <summary>Reads <paramref name="name"/> with <see cref="EndianReader.ReadAssetId"/>.</summary>
        public AssetId ReadAssetId(string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadAssetId();
        }

        /// <summary>Reads <paramref name="count"/> bytes as <paramref name="name"/>.</summary>
        public byte[] ReadBytes(int count, string name)
        {
            reader.Recorder?.Name(name);
            return reader.ReadBytes(count);
        }

        /// <summary>
        /// Reads and discards <paramref name="count"/> bytes of padding, named for the field before it.
        /// </summary>
        public void SkipPadding(int count)
        {
            reader.Recorder?.NamePadding();
            reader.ReadBytes(count);
        }

        /// <summary>
        /// Reads every remaining byte as <paramref name="name"/>, bytes no surface models.
        /// </summary>
        public byte[] ReadUnmodeled(string name)
        {
            reader.Recorder?.NameUnmodeled(name);
            return reader.ReadRemainingBytes();
        }

        /// <summary>
        /// Reads every remaining byte as the asset's <see cref="FieldMap.UnparsedTail"/>.
        /// </summary>
        public byte[] ReadUnparsedTail() => reader.ReadUnmodeled(FieldMap.UnparsedTail);
    }
}

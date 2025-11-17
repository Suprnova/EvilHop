using System.Buffers.Binary;

namespace EvilHop.Extensions;

public static class EvilInt
{
    extension(uint val)
    {
        /// <summary>
        /// Returns the EvilEngine byte representation of this <see cref="uint"/>.
        /// </summary>
        /// <returns>the EvilEngine byte representation of this <see cref="uint"/>.</returns>
        public byte[] ToEvilBytes()
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, val);
            return bytes.ToArray();
        }
    }

    extension(Span<byte> bytes)
    {
        /// <summary>
        /// Returns the <see cref="uint"/> representation of this <see cref="Span{T}"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <returns>the <see cref="uint"/> representation of this <see cref="Span{T}"/>.</returns>
        public uint ToEvilInt() => BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }
}

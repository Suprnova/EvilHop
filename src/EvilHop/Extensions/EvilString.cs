using System.Buffers.Binary;

namespace EvilHop.Extensions;

public static class EvilString
{
    extension(string str)
    {
        /// <summary>
        /// Returns the EvilEngine byte representation of this <see cref="string"/>.
        /// </summary>
        /// <returns>the EvilEngine byte representation of this <see cref="string"/>.</returns>
        public byte[] ToEvilBytes() => throw new NotImplementedException();
    }

    // TODO: bad in theory, since strings are null terminated?
    // we can't make a string from a slice if we don't know the dimensions of the slice
    extension(Span<byte> bytes)
    {
        /// <summary>
        /// Returns the <see cref="string"/> representation of this <see cref="Span{T}"/>.
        /// </summary>
        /// <returns>the <see cref="string"/> representation of this <see cref="Span{T}"/>.</returns>
        public uint ToEvilString() => throw new NotImplementedException();
    }
}

using EvilHop.Primitives;
using System.Buffers.Binary;

namespace EvilHop.Assets;

/// <summary>
/// One 4-byte slot of a <see cref="Link"/>'s <see cref="Link.Params"/>, whose actual meaning -
/// float, int, or asset reference - depends on which event the <see cref="Link"/> responds to.
/// </summary>
public abstract class Parameter
{
    private protected Parameter() { }

    /// <summary>
    /// Writes this <see cref="Parameter"/>'s value to <paramref name="destination"/>, which must be
    /// exactly 4 bytes, big-endian.
    /// </summary>
    /// <param name="destination">The destination to write to.</param>
    internal abstract void WriteTo(Span<byte> destination);
}

/// <summary>
/// A <see cref="Parameter"/> whose meaning is not yet known, storing its 4 bytes verbatim.
/// </summary>
/// <param name="bytes">The parameter's raw bytes. Must be exactly 4 bytes.</param>
public sealed class RawParameter(byte[] bytes) : Parameter
{
    /// <summary>
    /// The parameter's raw bytes. Always exactly 4 bytes.
    /// </summary>
    public byte[] Bytes { get; set; } = bytes;

    /// <inheritdoc/>
    internal override void WriteTo(Span<byte> destination) => Bytes.CopyTo(destination);
}

/// <summary>
/// A <see cref="Parameter"/> known to hold a floating-point value.
/// </summary>
/// <param name="value">The parameter's value.</param>
public sealed class FloatParameter(float value) : Parameter
{
    /// <summary>
    /// The parameter's value.
    /// </summary>
    public float Value { get; set; } = value;

    /// <inheritdoc/>
    internal override void WriteTo(Span<byte> destination) =>
        BinaryPrimitives.WriteSingleBigEndian(destination, Value);
}

/// <summary>
/// A <see cref="Parameter"/> known to hold an integer value.
/// </summary>
/// <param name="value">The parameter's value.</param>
public sealed class IntParameter(int value) : Parameter
{
    /// <summary>
    /// The parameter's value.
    /// </summary>
    public int Value { get; set; } = value;

    /// <inheritdoc/>
    internal override void WriteTo(Span<byte> destination) =>
        BinaryPrimitives.WriteInt32BigEndian(destination, Value);
}

/// <summary>
/// A <see cref="Parameter"/> known to hold a reference to another asset.
/// </summary>
/// <param name="value">The parameter's value.</param>
public sealed class AssetIdParameter(AssetId value) : Parameter
{
    /// <summary>
    /// The parameter's value.
    /// </summary>
    public AssetId Value { get; set; } = value;

    /// <inheritdoc/>
    internal override void WriteTo(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, Value.Value);
}

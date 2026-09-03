using EvilHop.Primitives;
using System.Collections.Immutable;

namespace EvilHop.Assets;

/// <summary>
/// One 4-byte slot of a <see cref="Link"/>'s <see cref="Link.Params"/>, whose actual meaning -
/// float, int, or asset reference - depends on which event the <see cref="Link"/> responds to.
/// </summary>
public abstract class Parameter
{
    private protected Parameter() { }

    /// <summary>
    /// Writes this <see cref="Parameter"/>'s value, exactly 4 bytes, to <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    internal abstract void WriteTo(EndianWriter writer);
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
    /// <exception cref="ArgumentException">The assigned value's length isn't 4.</exception>
    public ImmutableArray<byte> Bytes
    {
        get;
        set => field = Validate(value);
    } = Validate([.. bytes]);

    private static ImmutableArray<byte> Validate(ImmutableArray<byte> value) =>
        value.Length == 4
            ? value
            : throw new ArgumentException($"{nameof(Bytes)} must contain exactly 4 elements.", nameof(value));

    /// <inheritdoc/>
    internal override void WriteTo(EndianWriter writer) => writer.Write(Bytes.AsSpan());
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
    internal override void WriteTo(EndianWriter writer) => writer.Write(Value);
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
    internal override void WriteTo(EndianWriter writer) => writer.Write(Value);
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
    internal override void WriteTo(EndianWriter writer) => writer.Write(Value);
}

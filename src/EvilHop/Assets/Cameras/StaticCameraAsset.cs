using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="CameraAsset"/> that does not move.
/// </summary>
public sealed class StaticCameraAsset() : CameraAsset, Physical.IStaticCameraAsset
{
    /// <inheritdoc/>
    public override CameraKind Kind => CameraKind.Static;

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IStaticCameraAsset Physical => this;

    private uint _unused;
    uint Physical.IStaticCameraAsset.Unused { get => _unused; set => _unused = value; }

    private byte[] _reserved = new byte[ReservedSize];
    byte[] Physical.IStaticCameraAsset.Reserved { get => _reserved; set => _reserved = value; }

    private const int ReservedSize = 20;

    internal static StaticCameraAsset Read(EndianReader reader, FormatProfile _)
    {
        var value = new StaticCameraAsset();
        value.Physical.Unused = reader.ReadUInt32();
        value.Physical.Reserved = reader.ReadBytes(ReservedSize);
        return value;
    }

    internal static void Write(StaticCameraAsset value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.Physical.Unused);
        writer.Write(value.Physical.Reserved);
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="StaticCameraAsset"/>'s underlying values.
    /// </summary>
    public interface IStaticCameraAsset : ICameraAsset
    {
        /// <summary>
        /// Unused.
        /// </summary>
        uint Unused { get; set; }

        /// <summary>
        /// The unused 20-byte remainder of the type-specific storage this <see cref="CameraKind"/> shares
        /// with every other one.
        /// </summary>
        [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Fixed-size raw padding with no field structure of its own; a byte[] is the natural representation.")]
        byte[] Reserved { get; set; }
    }
}

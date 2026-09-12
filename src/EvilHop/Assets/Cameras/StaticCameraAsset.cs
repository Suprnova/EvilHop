using EvilHop.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="CameraAsset"/> that does not move.
/// </summary>
public sealed class StaticCameraAsset : CameraAsset, IPhysicalStaticCameraAsset
{
    /// <inheritdoc/>
    public override CameraKind Kind => CameraKind.Static;

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalStaticCameraAsset Physical => this;

    private uint _unused;
    uint IPhysicalStaticCameraAsset.Unused { get => _unused; set => _unused = value; }

    private byte[] _reserved = new byte[ReservedSize];
    byte[] IPhysicalStaticCameraAsset.Reserved { get => _reserved; set => _reserved = value; }

    private const int ReservedSize = 20;

    private protected override void ReadTypeFields(EndianReader reader)
    {
        Physical.Unused = reader.ReadUInt32();
        Physical.Reserved = reader.ReadBytes(ReservedSize);
    }

    private protected override void WriteTypeFields(EndianWriter writer)
    {
        writer.Write(Physical.Unused);
        writer.Write(Physical.Reserved);
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="StaticCameraAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalStaticCameraAsset : IPhysicalCameraAsset
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

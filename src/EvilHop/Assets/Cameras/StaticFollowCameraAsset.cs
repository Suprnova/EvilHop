using EvilHop.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="CameraAsset"/> that does not move but eases its framing toward its target, unlike
/// <see cref="StaticCameraAsset"/>.
/// </summary>
public sealed class StaticFollowCameraAsset : CameraAsset, IPhysicalStaticFollowCameraAsset
{
    /// <summary>How much the camera's framing lags behind its target before catching up.</summary>
    public float RubberBand { get; set; }

    /// <inheritdoc/>
    public override CameraKind Kind => CameraKind.StaticFollow;

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalStaticFollowCameraAsset Physical => this;

    private byte[] _reserved = new byte[ReservedSize];
    byte[] IPhysicalStaticFollowCameraAsset.Reserved { get => _reserved; set => _reserved = value; }

    private const int ReservedSize = 20;

    private protected override void ReadTypeFields(EndianReader reader)
    {
        RubberBand = reader.ReadSingle();
        Physical.Reserved = reader.ReadBytes(ReservedSize);
    }

    private protected override void WriteTypeFields(EndianWriter writer)
    {
        writer.Write(RubberBand);
        writer.Write(Physical.Reserved);
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="StaticFollowCameraAsset"/>'s underlying
/// values.
/// </summary>
public interface IPhysicalStaticFollowCameraAsset : IPhysicalCameraAsset
{
    /// <summary>
    /// The unused 20-byte remainder of the type-specific storage this <see cref="CameraKind"/> shares
    /// with every other one.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Fixed-size raw padding with no field structure of its own; a byte[] is the natural representation.")]
    byte[] Reserved { get; set; }
}

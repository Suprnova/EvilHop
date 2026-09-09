using EvilHop.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="CameraAsset"/> that hovers just behind and to the side of its target, realigning
/// itself as the target moves.
/// </summary>
public sealed class ShoulderCameraAsset : CameraAsset, IPhysicalShoulderCameraAsset
{
    /// <summary>The camera's distance from its target.</summary>
    public float Distance { get; set; }

    /// <summary>The camera's height above its target.</summary>
    public float Height { get; set; }

    /// <summary>How quickly the camera realigns itself behind its target.</summary>
    public float RealignSpeed { get; set; }

    /// <summary>The delay, in seconds, before the camera starts realigning after its target turns.</summary>
    public float RealignDelay { get; set; }

    /// <inheritdoc/>
    public override CameraKind Kind => CameraKind.Shoulder;

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalShoulderCameraAsset Physical => this;

    private byte[] _reserved = new byte[ReservedSize];
    byte[] IPhysicalShoulderCameraAsset.Reserved { get => _reserved; set => _reserved = value; }

    private const int ReservedSize = 8;

    internal ShoulderCameraAsset() { }

    private protected override void ReadTypeFields(EndianReader reader)
    {
        Distance = reader.ReadSingle();
        Height = reader.ReadSingle();
        RealignSpeed = reader.ReadSingle();
        RealignDelay = reader.ReadSingle();
        Physical.Reserved = reader.ReadBytes(ReservedSize);
    }

    private protected override void WriteTypeFields(EndianWriter writer)
    {
        writer.Write(Distance);
        writer.Write(Height);
        writer.Write(RealignSpeed);
        writer.Write(RealignDelay);
        writer.Write(Physical.Reserved);
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="ShoulderCameraAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalShoulderCameraAsset : IPhysicalCameraAsset
{
    /// <summary>
    /// The unused 8-byte remainder of the type-specific storage this <see cref="CameraKind"/> shares
    /// with every other one.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Fixed-size raw padding with no field structure of its own; a byte[] is the natural representation.")]
    byte[] Reserved { get; set; }
}

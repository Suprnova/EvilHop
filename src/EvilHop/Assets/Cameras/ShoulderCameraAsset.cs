using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="CameraAsset"/> that hovers just behind and to the side of its target, realigning
/// itself as the target moves.
/// </summary>
public sealed class ShoulderCameraAsset() : CameraAsset, Physical.IShoulderCameraAsset
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
    public override Physical.IShoulderCameraAsset Physical => this;

    private byte[] _reserved = new byte[ReservedSize];
    byte[] Physical.IShoulderCameraAsset.Reserved { get => _reserved; set => _reserved = value; }

    private const int ReservedSize = 8;

    internal static ShoulderCameraAsset Read(EndianReader reader, FormatProfile _)
    {
        var value = new ShoulderCameraAsset
        {
            Distance = reader.ReadSingle(),
            Height = reader.ReadSingle(),
            RealignSpeed = reader.ReadSingle(),
            RealignDelay = reader.ReadSingle(),
        };
        value.Physical.Reserved = reader.ReadBytes(ReservedSize);
        return value;
    }

    internal static void Write(ShoulderCameraAsset value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.Distance);
        writer.Write(value.Height);
        writer.Write(value.RealignSpeed);
        writer.Write(value.RealignDelay);
        writer.Write(value.Physical.Reserved);
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="ShoulderCameraAsset"/>'s underlying values.
    /// </summary>
    public interface IShoulderCameraAsset : ICameraAsset
    {
        /// <summary>
        /// The unused 8-byte remainder of the type-specific storage this <see cref="CameraKind"/> shares
        /// with every other one.
        /// </summary>
        [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Fixed-size raw padding with no field structure of its own; a byte[] is the natural representation.")]
        byte[] Reserved { get; set; }
    }
}

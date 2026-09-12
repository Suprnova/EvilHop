using EvilHop.Common;
using EvilHop.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="CameraAsset"/> that moves along a fixed path over time.
/// </summary>
public sealed class PathCameraAsset : CameraAsset, IPhysicalPathCameraAsset
{
    /// <summary>
    /// The <see cref="AssetId"/> of the path this camera follows.
    /// </summary>
    public AssetId PathId { get; set; }

    /// <summary>The time, in seconds, at which the camera reaches the end of its path.</summary>
    public float TimeEnd { get; set; }

    /// <summary>The delay, in seconds, before the camera starts moving along its path.</summary>
    public float TimeDelay { get; set; }

    /// <inheritdoc/>
    public override CameraKind Kind => CameraKind.Path;

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalPathCameraAsset Physical => this;

    private byte[] _reserved = new byte[ReservedSize];
    byte[] IPhysicalPathCameraAsset.Reserved { get => _reserved; set => _reserved = value; }

    private const int ReservedSize = 12;

    private protected override void ReadTypeFields(EndianReader reader)
    {
        PathId = reader.ReadAssetId();
        TimeEnd = reader.ReadSingle();
        TimeDelay = reader.ReadSingle();
        Physical.Reserved = reader.ReadBytes(ReservedSize);
    }

    private protected override void WriteTypeFields(EndianWriter writer)
    {
        writer.Write(PathId);
        writer.Write(TimeEnd);
        writer.Write(TimeDelay);
        writer.Write(Physical.Reserved);
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="PathCameraAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalPathCameraAsset : IPhysicalCameraAsset
{
    /// <summary>
    /// The unused 12-byte remainder of the type-specific storage this <see cref="CameraKind"/> shares
    /// with every other one.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Fixed-size raw padding with no field structure of its own; a byte[] is the natural representation.")]
    byte[] Reserved { get; set; }
}

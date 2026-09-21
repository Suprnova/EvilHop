using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="CameraAsset"/> that moves along a fixed path over time.
/// </summary>
public sealed class PathCameraAsset() : CameraAsset, Physical.IPathCameraAsset
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
    public override Physical.IPathCameraAsset Physical => this;

    private byte[] _reserved = new byte[ReservedSize];
    byte[] Physical.IPathCameraAsset.Reserved { get => _reserved; set => _reserved = value; }

    private const int ReservedSize = 12;

    internal static PathCameraAsset Read(EndianReader reader, FormatProfile _)
    {
        var value = new PathCameraAsset
        {
            PathId = reader.ReadAssetId(),
            TimeEnd = reader.ReadSingle(),
            TimeDelay = reader.ReadSingle(),
        };
        value.Physical.Reserved = reader.ReadBytes(ReservedSize);
        return value;
    }

    internal static void Write(PathCameraAsset value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.PathId);
        writer.Write(value.TimeEnd);
        writer.Write(value.TimeDelay);
        writer.Write(value.Physical.Reserved);
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="PathCameraAsset"/>'s underlying values.
    /// </summary>
    public interface IPathCameraAsset : ICameraAsset
    {
        /// <summary>
        /// The unused 12-byte remainder of the type-specific storage this <see cref="CameraKind"/> shares
        /// with every other one.
        /// </summary>
        [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Fixed-size raw padding with no field structure of its own; a byte[] is the natural representation.")]
        byte[] Reserved { get; set; }
    }
}

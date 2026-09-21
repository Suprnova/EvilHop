using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityMotion"/> that travels along a path of <see cref="AssetType.MovePoint"/>s.
/// </summary>
public sealed class MovePointMotion() : EntityMotion
{
    /// <summary>
    /// Flags controlling how this motion travels between move points.
    /// </summary>
    public MovePointFlags MovePointFlags { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.MovePoint"/> to start at.
    /// </summary>
    public AssetId MovePointId { get; set; }

    /// <summary>
    /// The speed, in units per second, to travel at.
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// Whether the entity banks into turns while following a curved path.
    /// </summary>
    public bool UseBanking { get; set; }

    private protected override MotionType Type => MotionType.MovePoint;

    /// <remarks>
    /// Not read here - <see cref="EntityMotion.Read"/> sets it from the shared Motion block header,
    /// not from this motion's own fields.
    /// </remarks>
    internal static new MovePointMotion Read(EndianReader reader, FormatProfile _) => new()
    {
        MovePointFlags = (MovePointFlags)reader.ReadUInt32(),
        MovePointId = reader.ReadAssetId(),
        Speed = reader.ReadSingle(),
    };

    internal static void Write(MovePointMotion value, EndianWriter writer, FormatProfile _)
    {
        writer.Write((uint)value.MovePointFlags);
        writer.Write(value.MovePointId);
        writer.Write(value.Speed);
    }
}

/// <summary>
/// Flags controlling movement pacing and stopping behavior between move points.
/// </summary>
[Flags]
public enum MovePointFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The motion stops on reaching each move point, until run again.
    /// </summary>
    StopAtEachPoint = 1 << 0,
}

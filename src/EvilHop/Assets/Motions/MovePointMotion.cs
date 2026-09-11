using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityMotion"/> that travels along a path of <see cref="AssetType.MovePoint"/>s.
/// </summary>
public sealed class MovePointMotion : EntityMotion
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
    /// Whether the entity banks into turns while following a curved path. Always
    /// <see langword="false"/> in known files.
    /// </summary>
    public bool UseBanking { get; set; }

    private protected override MotionType Type => MotionType.MovePoint;

    private protected override void ReadFields(EndianReader reader, GameVersion game)
    {
        MovePointFlags = (MovePointFlags)reader.ReadUInt32();
        MovePointId = reader.ReadAssetId();
        Speed = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion game)
    {
        writer.Write((uint)MovePointFlags);
        writer.Write(MovePointId);
        writer.Write(Speed);
    }
}

/// <summary>
/// Represents all known values for <see cref="MovePointMotion.MovePointFlags"/>.
/// </summary>
[Flags]
public enum MovePointFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The motion stops on reaching each move point, until run again. Per decompiled source.
    /// </summary>
    StopAtEachPoint = 1 << 0,
}

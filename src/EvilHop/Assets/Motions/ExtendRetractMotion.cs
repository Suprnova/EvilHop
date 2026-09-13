using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityMotion"/> that moves out to one position and back again, waiting at each end.
/// </summary>
public sealed class ExtendRetractMotion : EntityMotion
{
    /// <summary>The position the entity starts at, and retracts back to.</summary>
    public Vector3 RetractPosition { get; set; }

    /// <summary>The offset from <see cref="RetractPosition"/> the entity extends to.</summary>
    public Vector3 ExtendOffset { get; set; }

    /// <summary>The time, in seconds, the entity takes to extend.</summary>
    public float ExtendTime { get; set; }

    /// <summary>The time, in seconds, the entity waits once extended.</summary>
    public float ExtendWaitTime { get; set; }

    /// <summary>The time, in seconds, the entity takes to retract.</summary>
    public float RetractTime { get; set; }

    /// <summary>The time, in seconds, the entity waits once retracted.</summary>
    public float RetractWaitTime { get; set; }

    private protected override MotionType Type => MotionType.ExtendRetract;

    private protected override void ReadFields(EndianReader reader, GameVersion _)
    {
        RetractPosition = reader.ReadVector3();
        ExtendOffset = reader.ReadVector3();
        ExtendTime = reader.ReadSingle();
        ExtendWaitTime = reader.ReadSingle();
        RetractTime = reader.ReadSingle();
        RetractWaitTime = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion _)
    {
        writer.Write(RetractPosition);
        writer.Write(ExtendOffset);
        writer.Write(ExtendTime);
        writer.Write(ExtendWaitTime);
        writer.Write(RetractTime);
        writer.Write(RetractWaitTime);
    }
}

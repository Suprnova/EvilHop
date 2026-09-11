using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="PlatformMotion"/> that moves forward, then returns.
/// </summary>
/// <remarks>
/// No known file uses this motion, and <see cref="GameVersion.BFBB"/>'s code never reads it.
/// </remarks>
public sealed class ForwardReturnMotion : PlatformMotion
{
    /// <summary>The speed to move forward at.</summary>
    public float ForwardSpeed { get; set; }

    /// <summary>The speed to return at.</summary>
    public float ReturnSpeed { get; set; }

    /// <summary>The time, in seconds, to wait before returning.</summary>
    public float ReturnDelay { get; set; }

    /// <summary>The time, in seconds, to wait after returning.</summary>
    public float PostReturnDelay { get; set; }

    internal override PlatformType PlatformType => PlatformType.ForwardReturn;

    private protected override void ReadFields(EndianReader reader, GameVersion game)
    {
        ForwardSpeed = reader.ReadSingle();
        ReturnSpeed = reader.ReadSingle();
        ReturnDelay = reader.ReadSingle();
        PostReturnDelay = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion game)
    {
        writer.Write(ForwardSpeed);
        writer.Write(ReturnSpeed);
        writer.Write(ReturnDelay);
        writer.Write(PostReturnDelay);
    }
}

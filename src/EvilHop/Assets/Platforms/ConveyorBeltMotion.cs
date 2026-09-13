using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="PlatformMotion"/> that carries the player along while they stand on it.
/// </summary>
public sealed class ConveyorBeltMotion : PlatformMotion
{
    /// <summary>
    /// The speed, in units per second, the player slides along the platform's X axis while
    /// standing on it.
    /// </summary>
    public float Speed { get; set; }

    internal override PlatformType PlatformType => PlatformType.ConveyorBelt;

    private protected override void ReadFields(EndianReader reader, GameVersion _) =>
        Speed = reader.ReadSingle();

    private protected override void WriteFields(EndianWriter writer, GameVersion _) =>
        writer.Write(Speed);
}

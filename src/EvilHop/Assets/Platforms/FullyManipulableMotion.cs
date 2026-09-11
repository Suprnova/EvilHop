using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="PlatformMotion"/> that never moves by itself, only in response to events that
/// translate or rotate it.
/// </summary>
public sealed class FullyManipulableMotion : PlatformMotion
{
    internal override PlatformType PlatformType => PlatformType.FullyManipulable;

    private protected override void ReadFields(EndianReader reader, GameVersion game) { }

    private protected override void WriteFields(EndianWriter writer, GameVersion game) { }
}

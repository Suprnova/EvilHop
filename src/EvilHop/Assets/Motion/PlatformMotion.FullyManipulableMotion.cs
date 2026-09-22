using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class PlatformMotion
{
    /// <summary>
    /// A <see cref="PlatformMotion"/> that never moves by itself, only in response to events that
    /// translate or rotate it.
    /// </summary>
    public sealed class FullyManipulableMotion() : PlatformMotion
    {
        internal override PlatformType PlatformType => PlatformType.FullyManipulable;

        internal static FullyManipulableMotion Read(EndianReader _, FormatProfile __) => new();

        internal static void Write(FullyManipulableMotion _, EndianWriter __, FormatProfile ___) { }
    }
}

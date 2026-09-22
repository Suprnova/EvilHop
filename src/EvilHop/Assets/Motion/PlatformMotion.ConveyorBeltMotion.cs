using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class PlatformMotion
{
    /// <summary>
    /// A <see cref="PlatformMotion"/> that carries the player along while they stand on it.
    /// </summary>
    public sealed class ConveyorBeltMotion() : PlatformMotion
    {
        /// <summary>
        /// The speed, in units per second, the player slides along the platform's X axis while
        /// standing on it.
        /// </summary>
        public float Speed { get; set; }

        internal override PlatformType PlatformType => PlatformType.ConveyorBelt;

        internal static ConveyorBeltMotion Read(EndianReader reader, FormatProfile _) => new()
        {
            Speed = reader.ReadSingle(),
        };

        internal static void Write(ConveyorBeltMotion value, EndianWriter writer, FormatProfile _) =>
            writer.Write(value.Speed);
    }
}

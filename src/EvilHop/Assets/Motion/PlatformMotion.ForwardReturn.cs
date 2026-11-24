using EvilHop.Assets.Serialization;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class PlatformMotion
{
    /// <summary>
    /// A <see cref="PlatformMotion"/> with no known behavior.
    /// </summary>
    public sealed class ForwardReturn() : PlatformMotion
    {
        /// <summary>Unknown.</summary>
        public float ForwardSpeed { get; set; }

        /// <summary>Unknown.</summary>
        public float ReturnSpeed { get; set; }

        /// <summary>Unknown.</summary>
        public float ReturnDelay { get; set; }

        /// <summary>Unknown.</summary>
        public float PostReturnDelay { get; set; }

        internal override PlatformType PlatformType => PlatformType.ForwardReturn;

        internal static ForwardReturn Read(EndianReader reader, FormatProfile _) => new()
        {
            ForwardSpeed = reader.ReadSingle(nameof(ForwardSpeed)),
            ReturnSpeed = reader.ReadSingle(nameof(ReturnSpeed)),
            ReturnDelay = reader.ReadSingle(nameof(ReturnDelay)),
            PostReturnDelay = reader.ReadSingle(nameof(PostReturnDelay)),
        };

        internal static void Write(ForwardReturn value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.ForwardSpeed);
            writer.Write(value.ReturnSpeed);
            writer.Write(value.ReturnDelay);
            writer.Write(value.PostReturnDelay);
        }
    }
}

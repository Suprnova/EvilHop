using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class PlatformMotion
{
    /// <summary>
    /// A <see cref="PlatformMotion"/> with no known behavior.
    /// </summary>
    public sealed class Falling() : PlatformMotion
    {
        /// <summary>Unknown.</summary>
        public float Speed { get; set; }

        /// <summary>Unknown.</summary>
        public AssetId BustModelId { get; set; }

        internal override PlatformType PlatformType => PlatformType.Falling;

        internal static Falling Read(EndianReader reader, FormatProfile _) => new()
        {
            Speed = reader.ReadSingle(),
            BustModelId = reader.ReadAssetId(),
        };

        internal static void Write(Falling value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Speed);
            writer.Write(value.BustModelId);
        }
    }
}

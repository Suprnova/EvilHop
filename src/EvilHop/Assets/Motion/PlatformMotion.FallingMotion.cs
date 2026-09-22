using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class PlatformMotion
{
    /// <summary>
    /// A <see cref="PlatformMotion"/> for a falling platform.
    /// </summary>
    public sealed class FallingMotion() : PlatformMotion
    {
        /// <summary>Unknown.</summary>
        public float Speed { get; set; }

        /// <summary>An unknown <see cref="AssetType.Model"/> <see cref="AssetId"/>.</summary>
        public AssetId BustModelId { get; set; }

        internal override PlatformType PlatformType => PlatformType.Falling;

        internal static FallingMotion Read(EndianReader reader, FormatProfile _) => new()
        {
            Speed = reader.ReadSingle(),
            BustModelId = reader.ReadAssetId(),
        };

        internal static void Write(FallingMotion value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Speed);
            writer.Write(value.BustModelId);
        }
    }
}

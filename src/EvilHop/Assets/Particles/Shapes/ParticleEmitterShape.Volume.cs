using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ShapeKind.Volume"/>.
    /// </summary>
    public sealed class Volume : ParticleEmitterShape
    {
        /// <summary>The <see cref="AssetType.Volume"/> particles are emitted from anywhere within.</summary>
        public AssetId VolumeId { get; set; }

        internal static Volume Read(EndianReader reader, FormatProfile _) => new()
        {
            VolumeId = reader.ReadAssetId(),
        };

        internal static void Write(Volume value, EndianWriter writer, FormatProfile _) =>
            writer.Write(value.VolumeId);
    }
}

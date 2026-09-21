using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.Volume"/>.
/// </summary>
public sealed class VolumeEmitterShape : ParticleEmitterShape
{
    /// <summary>The <see cref="AssetType.Volume"/> particles are emitted from anywhere within.</summary>
    public AssetId VolumeId { get; set; }

    internal static VolumeEmitterShape Read(EndianReader reader, FormatProfile _) => new()
    {
        VolumeId = reader.ReadAssetId(),
    };

    internal static void Write(VolumeEmitterShape value, EndianWriter writer, FormatProfile _) =>
        writer.Write(value.VolumeId);
}

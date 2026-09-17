using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.Volume"/>.
/// </summary>
public sealed class VolumeEmitterShape : ParticleEmitterShape
{
    /// <summary>The <see cref="AssetType.Volume"/> particles are emitted from anywhere within.</summary>
    public AssetId VolumeId { get; set; }

    private protected override void ReadFields(EndianReader reader, GameVersion _) =>
        VolumeId = reader.ReadAssetId();

    private protected override void WriteFields(EndianWriter writer, GameVersion _) =>
        writer.Write(VolumeId);
}

using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.VCylEdge"/>. Not used in
/// <see cref="GameVersion.N100F"/>.
/// </summary>
public sealed class VCylEmitterShape : ParticleEmitterShape
{
    /// <summary>The cylinder's height.</summary>
    public float Height { get; set; }

    /// <summary>The cylinder's radius.</summary>
    public float Radius { get; set; }

    /// <summary>How much a particle's emitted direction deflects away from the cylinder's edge.</summary>
    public float Deflection { get; set; }

    private protected override void ReadFields(EndianReader reader, GameVersion _)
    {
        Height = reader.ReadSingle();
        Radius = reader.ReadSingle();
        Deflection = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion _)
    {
        writer.Write(Height);
        writer.Write(Radius);
        writer.Write(Deflection);
    }
}

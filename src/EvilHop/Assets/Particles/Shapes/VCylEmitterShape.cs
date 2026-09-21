using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

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

    internal static VCylEmitterShape Read(EndianReader reader, FormatProfile _) => new()
    {
        Height = reader.ReadSingle(),
        Radius = reader.ReadSingle(),
        Deflection = reader.ReadSingle(),
    };

    internal static void Write(VCylEmitterShape value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.Height);
        writer.Write(value.Radius);
        writer.Write(value.Deflection);
    }
}

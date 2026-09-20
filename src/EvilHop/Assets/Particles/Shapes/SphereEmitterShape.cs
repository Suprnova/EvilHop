using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.SphereEdge1"/>,
/// <see cref="ParticleEmitterKind.Sphere"/>, <see cref="ParticleEmitterKind.SphereEdge2"/>, and
/// <see cref="ParticleEmitterKind.SphereEdge3"/>.
/// </summary>
public sealed class SphereEmitterShape : ParticleEmitterShape
{
    /// <summary>The sphere's radius.</summary>
    public float Radius { get; set; }

    private protected override void ReadFields(EndianReader reader, GameVersion _) =>
        Radius = reader.ReadSingle();

    private protected override void WriteFields(EndianWriter writer, GameVersion _) =>
        writer.Write(Radius);
}

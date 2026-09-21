using EvilHop.Primitives;
using EvilHop.Serialization;

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

    internal static SphereEmitterShape Read(EndianReader reader, FormatProfile _) => new()
    {
        Radius = reader.ReadSingle(),
    };

    internal static void Write(SphereEmitterShape value, EndianWriter writer, FormatProfile _) =>
        writer.Write(value.Radius);
}

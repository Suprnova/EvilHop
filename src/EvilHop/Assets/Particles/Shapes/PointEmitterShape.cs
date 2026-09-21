using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.Point"/>, which emits
/// directly from its <see cref="ParticleEmitterAsset.Position"/> and holds no shape data of its own.
/// </summary>
public sealed class PointEmitterShape : ParticleEmitterShape
{
    internal static PointEmitterShape Read(EndianReader _, FormatProfile __) => new();

    internal static void Write(PointEmitterShape _, EndianWriter __, FormatProfile ___) { }
}

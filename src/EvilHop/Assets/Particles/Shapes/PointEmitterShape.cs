using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.Point"/>, which emits
/// directly from its <see cref="ParticleEmitterAsset.Position"/> and holds no shape data of its own.
/// </summary>
public sealed class PointEmitterShape : ParticleEmitterShape
{
    private protected override void ReadFields(EndianReader _, GameVersion __) { }

    private protected override void WriteFields(EndianWriter _, GameVersion __) { }
}

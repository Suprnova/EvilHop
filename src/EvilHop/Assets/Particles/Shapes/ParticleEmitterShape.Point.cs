using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ShapeKind.Point"/>, which emits
    /// directly from its <see cref="ParticleEmitterAsset.Position"/> and holds no shape data of its own.
    /// </summary>
    public sealed class Point : ParticleEmitterShape
    {
        internal static Point Read(EndianReader _, FormatProfile __) => new();

        internal static void Write(Point _, EndianWriter __, FormatProfile ___) { }
    }
}

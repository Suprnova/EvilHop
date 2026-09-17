using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.RectEdge"/> and
/// <see cref="ParticleEmitterKind.Rect"/>.
/// </summary>
public sealed class RectEmitterShape : ParticleEmitterShape
{
    /// <summary>The rectangle's length along the X axis.</summary>
    public float XLength { get; set; }

    /// <summary>The rectangle's length along the Z axis.</summary>
    public float ZLength { get; set; }

    private protected override void ReadFields(EndianReader reader, GameVersion _)
    {
        XLength = reader.ReadSingle();
        ZLength = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion _)
    {
        writer.Write(XLength);
        writer.Write(ZLength);
    }
}

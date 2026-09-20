using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.OffsetPoint"/>.
/// </summary>
public sealed class OffsetPointEmitterShape : ParticleEmitterShape
{
    /// <summary>The offset from <see cref="ParticleEmitterAsset.Position"/> particles are emitted from.</summary>
    public Vector3 Offset { get; set; }

    private protected override void ReadFields(EndianReader reader, GameVersion _) =>
        Offset = reader.ReadVector3();

    private protected override void WriteFields(EndianWriter writer, GameVersion _) =>
        writer.Write(Offset);
}

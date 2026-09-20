using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.CircleEdge"/>,
/// <see cref="ParticleEmitterKind.Circle"/>, <see cref="ParticleEmitterKind.OCircleEdge"/>, and
/// <see cref="ParticleEmitterKind.OCircle"/>.
/// </summary>
public sealed class CircleEmitterShape : ParticleEmitterShape
{
    /// <summary>The circle's radius.</summary>
    public float Radius { get; set; }

    /// <summary>How much a particle's emitted direction deflects away from the circle's normal.</summary>
    public float Deflection { get; set; }

    /// <summary>
    /// The circle's normal direction. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public Vector3 Direction { get; set; }

    private protected override void ReadFields(EndianReader reader, GameVersion game)
    {
        Radius = reader.ReadSingle();
        Deflection = reader.ReadSingle();
        if (game is not GameVersion.N100F) Direction = reader.ReadVector3();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion game)
    {
        writer.Write(Radius);
        writer.Write(Deflection);
        if (game is not GameVersion.N100F) writer.Write(Direction);
    }
}

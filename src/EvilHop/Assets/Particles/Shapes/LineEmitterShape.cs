using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.Line"/>.
/// </summary>
public sealed class LineEmitterShape : ParticleEmitterShape
{
    /// <summary>One end of the line.</summary>
    public Vector3 Position1 { get; set; }

    /// <summary>The other end of the line.</summary>
    public Vector3 Position2 { get; set; }

    /// <summary>The line's thickness.</summary>
    public float Radius { get; set; }

    private protected override void ReadFields(EndianReader reader, GameVersion _)
    {
        Position1 = reader.ReadVector3();
        Position2 = reader.ReadVector3();
        Radius = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion _)
    {
        writer.Write(Position1);
        writer.Write(Position2);
        writer.Write(Radius);
    }
}

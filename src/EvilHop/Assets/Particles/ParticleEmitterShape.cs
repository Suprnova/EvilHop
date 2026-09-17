using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterAsset.Kind"/>-specific description of the shape particles are emitted
/// from or along, stored in a fixed-size block regardless of which shape is active.
/// </summary>
public abstract class ParticleEmitterShape
{
    private protected ParticleEmitterShape() { }

    /// <summary>
    /// The size, in bytes, of a <see cref="ParticleEmitterAsset"/>'s shape block, regardless of
    /// <see cref="ParticleEmitterAsset.Kind"/> or game.
    /// </summary>
    internal const int BlockSize = 0x1C;

    /// <summary>
    /// Reads this shape's fields from a reader scoped to the rest of its block.
    /// </summary>
    private protected abstract void ReadFields(EndianReader reader, GameVersion game);

    /// <summary>
    /// Writes this shape's fields, no more than the rest of its block holds.
    /// </summary>
    private protected abstract void WriteFields(EndianWriter writer, GameVersion game);

    /// <summary>
    /// Reads one shape block for <paramref name="kind"/>.
    /// </summary>
    /// <exception cref="InvalidDataException"><paramref name="kind"/> has no known shape.</exception>
    internal static ParticleEmitterShape Read(EndianReader reader, GameVersion game, ParticleEmitterKind kind)
    {
        using var block = new EndianReader(new MemoryStream(reader.ReadBytes(BlockSize)), reader.Endianness);

        ParticleEmitterShape shape = kind switch
        {
            ParticleEmitterKind.Point => new PointEmitterShape(),
            ParticleEmitterKind.CircleEdge or ParticleEmitterKind.Circle
                or ParticleEmitterKind.OCircleEdge or ParticleEmitterKind.OCircle => new CircleEmitterShape(),
            ParticleEmitterKind.RectEdge or ParticleEmitterKind.Rect => new RectEmitterShape(),
            ParticleEmitterKind.Line => new LineEmitterShape(),
            ParticleEmitterKind.Volume => new VolumeEmitterShape(),
            ParticleEmitterKind.SphereEdge1 or ParticleEmitterKind.Sphere
                or ParticleEmitterKind.SphereEdge2 or ParticleEmitterKind.SphereEdge3 => new SphereEmitterShape(),
            ParticleEmitterKind.OffsetPoint => new OffsetPointEmitterShape(),
            ParticleEmitterKind.VCylEdge => new VCylEmitterShape(),
            ParticleEmitterKind.EntityBone => new EntityBoneEmitterShape(),
            ParticleEmitterKind.EntityBound => new EntityBoundEmitterShape(),
            _ => throw new InvalidDataException($"Particle emitter kind 0x{(byte)kind:X2} has no {nameof(ParticleEmitterShape)}."),
        };

        shape.ReadFields(block, game);
        return shape;
    }

    /// <summary>
    /// Writes this shape as one block.
    /// </summary>
    internal void Write(EndianWriter writer, GameVersion game)
    {
        using var stream = new MemoryStream();
        using (var block = new EndianWriter(stream, writer.Endianness, leaveOpen: true))
            WriteFields(block, game);

        writer.Write(stream.ToArray());
        writer.Write(new byte[BlockSize - (int)stream.Length]);
    }
}

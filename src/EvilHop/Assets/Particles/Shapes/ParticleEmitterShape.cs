using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterAsset.Kind"/>-specific description of the shape particles are emitted
/// from or along, stored in a fixed-size block regardless of which shape is active.
/// </summary>
public abstract partial class ParticleEmitterShape
{
    private protected ParticleEmitterShape() { }

    /// <summary>
    /// The size, in bytes, of a <see cref="ParticleEmitterAsset"/>'s shape block, regardless of
    /// <see cref="ParticleEmitterAsset.Kind"/> or game.
    /// </summary>
    internal const int BlockSize = 0x1C;

    /// <summary>
    /// Reads one shape block for <paramref name="kind"/>.
    /// </summary>
    /// <exception cref="InvalidDataException"><paramref name="kind"/> has no known shape.</exception>
    internal static ParticleEmitterShape Read(EndianReader reader, ParticleEmitterAsset.ShapeKind kind, FormatProfile profile)
    {
        using var block = new EndianReader(new MemoryStream(reader.ReadBytes(BlockSize)), reader.Endianness);

        return kind switch
        {
            ParticleEmitterAsset.ShapeKind.Point => Point.Read(block, profile),
            ParticleEmitterAsset.ShapeKind.CircleEdge or ParticleEmitterAsset.ShapeKind.Circle
                or ParticleEmitterAsset.ShapeKind.OCircleEdge or ParticleEmitterAsset.ShapeKind.OCircle => Circle.Read(block, profile),
            ParticleEmitterAsset.ShapeKind.RectEdge or ParticleEmitterAsset.ShapeKind.Rect => Rectangle.Read(block, profile),
            ParticleEmitterAsset.ShapeKind.Line => Line.Read(block, profile),
            ParticleEmitterAsset.ShapeKind.Volume => Volume.Read(block, profile),
            ParticleEmitterAsset.ShapeKind.SphereEdge1 or ParticleEmitterAsset.ShapeKind.Sphere
                or ParticleEmitterAsset.ShapeKind.SphereEdge2 or ParticleEmitterAsset.ShapeKind.SphereEdge3 => Sphere.Read(block, profile),
            ParticleEmitterAsset.ShapeKind.OffsetPoint => OffsetPoint.Read(block, profile),
            ParticleEmitterAsset.ShapeKind.VCylEdge => Cylinder.Read(block, profile),
            ParticleEmitterAsset.ShapeKind.EntityBone => EntityBone.Read(block, profile),
            ParticleEmitterAsset.ShapeKind.EntityBound => EntityBound.Read(block, profile),
            _ => throw new InvalidDataException($"Particle emitter kind 0x{(byte)kind:X2} has no {nameof(ParticleEmitterShape)}."),
        };
    }

    /// <summary>
    /// Writes <paramref name="value"/> as one shape block.
    /// </summary>
    internal static void Write(ParticleEmitterShape value, EndianWriter writer, FormatProfile profile)
    {
        using var stream = new MemoryStream();
        using (var block = new EndianWriter(stream, writer.Endianness, leaveOpen: true))
        {
            switch (value)
            {
                case Point s: Point.Write(s, block, profile); break;
                case Circle s: Circle.Write(s, block, profile); break;
                case Rectangle s: Rectangle.Write(s, block, profile); break;
                case Line s: Line.Write(s, block, profile); break;
                case Volume s: Volume.Write(s, block, profile); break;
                case Sphere s: Sphere.Write(s, block, profile); break;
                case OffsetPoint s: OffsetPoint.Write(s, block, profile); break;
                case Cylinder s: Cylinder.Write(s, block, profile); break;
                case EntityBone s: EntityBone.Write(s, block, profile); break;
                case EntityBound s: EntityBound.Write(s, block, profile); break;
            }
        }

        writer.Write(stream.ToArray());
        writer.Write(new byte[BlockSize - (int)stream.Length]);
    }
}

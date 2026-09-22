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
    internal static ParticleEmitterShape Read(EndianReader reader, ParticleEmitterAsset.ParticleEmitterKind kind, FormatProfile profile)
    {
        using var block = new EndianReader(new MemoryStream(reader.ReadBytes(BlockSize)), reader.Endianness);

        return kind switch
        {
            ParticleEmitterAsset.ParticleEmitterKind.Point => PointEmitterShape.Read(block, profile),
            ParticleEmitterAsset.ParticleEmitterKind.CircleEdge or ParticleEmitterAsset.ParticleEmitterKind.Circle
                or ParticleEmitterAsset.ParticleEmitterKind.OCircleEdge or ParticleEmitterAsset.ParticleEmitterKind.OCircle => CircleEmitterShape.Read(block, profile),
            ParticleEmitterAsset.ParticleEmitterKind.RectEdge or ParticleEmitterAsset.ParticleEmitterKind.Rect => RectEmitterShape.Read(block, profile),
            ParticleEmitterAsset.ParticleEmitterKind.Line => LineEmitterShape.Read(block, profile),
            ParticleEmitterAsset.ParticleEmitterKind.Volume => VolumeEmitterShape.Read(block, profile),
            ParticleEmitterAsset.ParticleEmitterKind.SphereEdge1 or ParticleEmitterAsset.ParticleEmitterKind.Sphere
                or ParticleEmitterAsset.ParticleEmitterKind.SphereEdge2 or ParticleEmitterAsset.ParticleEmitterKind.SphereEdge3 => SphereEmitterShape.Read(block, profile),
            ParticleEmitterAsset.ParticleEmitterKind.OffsetPoint => OffsetPointEmitterShape.Read(block, profile),
            ParticleEmitterAsset.ParticleEmitterKind.VCylEdge => VCylEmitterShape.Read(block, profile),
            ParticleEmitterAsset.ParticleEmitterKind.EntityBone => EntityBoneEmitterShape.Read(block, profile),
            ParticleEmitterAsset.ParticleEmitterKind.EntityBound => EntityBoundEmitterShape.Read(block, profile),
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
                case PointEmitterShape s: PointEmitterShape.Write(s, block, profile); break;
                case CircleEmitterShape s: CircleEmitterShape.Write(s, block, profile); break;
                case RectEmitterShape s: RectEmitterShape.Write(s, block, profile); break;
                case LineEmitterShape s: LineEmitterShape.Write(s, block, profile); break;
                case VolumeEmitterShape s: VolumeEmitterShape.Write(s, block, profile); break;
                case SphereEmitterShape s: SphereEmitterShape.Write(s, block, profile); break;
                case OffsetPointEmitterShape s: OffsetPointEmitterShape.Write(s, block, profile); break;
                case VCylEmitterShape s: VCylEmitterShape.Write(s, block, profile); break;
                case EntityBoneEmitterShape s: EntityBoneEmitterShape.Write(s, block, profile); break;
                case EntityBoundEmitterShape s: EntityBoundEmitterShape.Write(s, block, profile); break;
            }
        }

        writer.Write(stream.ToArray());
        writer.Write(new byte[BlockSize - (int)stream.Length]);
    }
}

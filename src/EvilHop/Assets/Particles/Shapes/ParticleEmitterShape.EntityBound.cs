using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ShapeKind.EntityBound"/>, emitting
    /// from within the <see cref="ParticleEmitterAsset.AttachToId"/> entity's bounding volume. Not used
    /// in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public sealed class EntityBound : ParticleEmitterShape
    {
        /// <summary>Unknown.</summary>
        public byte Flags { get; set; }

        /// <summary>Unknown.</summary>
        public byte AttachType { get; set; }

        /// <summary>How much the bounding volume expands before particles are emitted within it.</summary>
        public float Expand { get; set; }

        /// <summary>How much a particle's emitted direction deflects away from the bounding volume's surface.</summary>
        public float Deflection { get; set; }

        internal static EntityBound Read(EndianReader reader, FormatProfile _)
        {
            var shape = new EntityBound
            {
                Flags = reader.ReadByte(),
                AttachType = reader.ReadByte(),
            };
            reader.ReadByte(); // pad1, always zero
            reader.ReadByte(); // pad2, always zero
            shape.Expand = reader.ReadSingle();
            shape.Deflection = reader.ReadSingle();
            return shape;
        }

        internal static void Write(EntityBound value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Flags);
            writer.Write(value.AttachType);
            writer.Write((byte)0); // pad1
            writer.Write((byte)0); // pad2
            writer.Write(value.Expand);
            writer.Write(value.Deflection);
        }
    }
}

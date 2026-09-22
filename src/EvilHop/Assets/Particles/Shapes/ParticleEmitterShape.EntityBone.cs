using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class ParticleEmitterShape
{
    /// <summary>
    /// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterAsset.ShapeKind.EntityBone"/>, emitting
    /// from a bone on the <see cref="ParticleEmitterAsset.AttachToId"/> entity. Not used in
    /// <see cref="GameVersion.N100F"/>.
    /// </summary>
    public sealed class EntityBone : ParticleEmitterShape
    {
        /// <summary>Unknown.</summary>
        public byte Flags { get; set; }

        /// <summary>Unknown.</summary>
        public byte AttachType { get; set; }

        /// <summary>The index of the bone particles are emitted from.</summary>
        public byte Bone { get; set; }

        /// <summary>The offset from the bone particles are emitted from.</summary>
        public Vector3 Offset { get; set; }

        /// <summary>The radius around the offset bone position particles may be emitted within.</summary>
        public float Radius { get; set; }

        /// <summary>How much a particle's emitted direction deflects away from the bone's orientation.</summary>
        public float Deflection { get; set; }

        internal static EntityBone Read(EndianReader reader, FormatProfile _)
        {
            var shape = new EntityBone
            {
                Flags = reader.ReadByte(),
                AttachType = reader.ReadByte(),
                Bone = reader.ReadByte(),
            };
            reader.ReadByte(); // pad1, always zero
            shape.Offset = reader.ReadVector3();
            shape.Radius = reader.ReadSingle();
            shape.Deflection = reader.ReadSingle();
            return shape;
        }

        internal static void Write(EntityBone value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Flags);
            writer.Write(value.AttachType);
            writer.Write(value.Bone);
            writer.Write((byte)0); // pad1
            writer.Write(value.Offset);
            writer.Write(value.Radius);
            writer.Write(value.Deflection);
        }
    }
}

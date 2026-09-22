using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class AttackTableAsset
{
    /// <summary>
    /// One hit detection bone for an <see cref="State"/>: a skeleton bone plus an offset from
    /// it, checked against a target's collision volume.
    /// </summary>
    public sealed class HitBone
    {
        /// <summary>
        /// The skeleton bone index this hit check is relative to, or <c>0xFFFF</c> if unused.
        /// </summary>
        public ushort Bone { get; set; }

        /// <summary>
        /// The offset from <see cref="Bone"/> the hit check is performed at.
        /// </summary>
        public Vector3 Offset { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public short Atomic { get; set; }

        internal static HitBone Read(EndianReader reader, FormatProfile _)
        {
            var hitBone = new HitBone { Bone = reader.ReadUInt16() };
            reader.ReadInt16(); // padding, always zero
            hitBone.Offset = reader.ReadVector3();
            hitBone.Atomic = reader.ReadInt16();
            reader.ReadInt16(); // padding, always zero
            return hitBone;
        }

        internal static void Write(HitBone hitBone, EndianWriter writer, FormatProfile _)
        {
            writer.Write(hitBone.Bone);
            writer.Write((short)0); // padding
            writer.Write(hitBone.Offset);
            writer.Write(hitBone.Atomic);
            writer.Write((short)0); // padding
        }
    }
}

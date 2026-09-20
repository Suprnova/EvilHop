using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.EntityBone"/>, emitting
/// from a bone on the <see cref="ParticleEmitterAsset.AttachToId"/> entity. Not used in
/// <see cref="GameVersion.N100F"/>.
/// </summary>
public sealed class EntityBoneEmitterShape : ParticleEmitterShape
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

    private protected override void ReadFields(EndianReader reader, GameVersion _)
    {
        Flags = reader.ReadByte();
        AttachType = reader.ReadByte();
        Bone = reader.ReadByte();
        reader.ReadByte(); // pad1, always zero
        Offset = reader.ReadVector3();
        Radius = reader.ReadSingle();
        Deflection = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion _)
    {
        writer.Write(Flags);
        writer.Write(AttachType);
        writer.Write(Bone);
        writer.Write((byte)0); // pad1
        writer.Write(Offset);
        writer.Write(Radius);
        writer.Write(Deflection);
    }
}

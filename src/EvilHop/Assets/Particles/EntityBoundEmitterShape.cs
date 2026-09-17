using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="ParticleEmitterShape"/> for <see cref="ParticleEmitterKind.EntityBound"/>, emitting
/// from within the <see cref="ParticleEmitterAsset.AttachToId"/> entity's bounding volume. Not used
/// in <see cref="GameVersion.N100F"/>.
/// </summary>
public sealed class EntityBoundEmitterShape : ParticleEmitterShape
{
    /// <summary>Unknown.</summary>
    public byte Flags { get; set; }

    /// <summary>Unknown.</summary>
    public byte AttachType { get; set; }

    /// <summary>How much the bounding volume expands before particles are emitted within it.</summary>
    public float Expand { get; set; }

    /// <summary>How much a particle's emitted direction deflects away from the bounding volume's surface.</summary>
    public float Deflection { get; set; }

    private protected override void ReadFields(EndianReader reader, GameVersion _)
    {
        Flags = reader.ReadByte();
        AttachType = reader.ReadByte();
        reader.ReadByte(); // pad1, always zero
        reader.ReadByte(); // pad2, always zero
        Expand = reader.ReadSingle();
        Deflection = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion _)
    {
        writer.Write(Flags);
        writer.Write(AttachType);
        writer.Write((byte)0); // pad1
        writer.Write((byte)0); // pad2
        writer.Write(Expand);
        writer.Write(Deflection);
    }
}

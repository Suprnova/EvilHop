using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class ParticleEmitterAsset
{
    internal static ParticleEmitterAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ParticleEmitterAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Flags = (ParticleEmitterFlags)reader.ReadByte();
        asset.Kind = (ParticleEmitterKind)reader.ReadByte();

        if (profile.Game is GameVersion.N100F)
        {
            asset.InlineProperties.Count = reader.ReadByte();
            asset.InlineProperties.CountVariation = reader.ReadByte();
            asset.InlineProperties.Interval = reader.ReadSingle();
        }
        else
        {
            reader.ReadInt16(); // pad, always zero
            asset.PropId = reader.ReadAssetId();
        }

        asset.Shape = ParticleEmitterShape.Read(reader, asset.Kind, profile);
        asset.AttachToId = reader.ReadAssetId();

        if (profile.Game is GameVersion.N100F)
        {
            asset.InlineProperties.ParSysId = reader.ReadAssetId();
            asset.Position = reader.ReadVector3();
            asset.Velocity = reader.ReadVector3();
            asset.VelocityAngleVariation = reader.ReadSingle();
            asset.InlineProperties.ColorBirth = reader.ReadRgba32();
            asset.InlineProperties.ColorDeath = reader.ReadRgba32();
            asset.InlineProperties.SizeBirth = reader.ReadSingle();
            asset.InlineProperties.SizeBirthVariation = reader.ReadSingle();
            asset.InlineProperties.SizeDeath = reader.ReadSingle();
            asset.InlineProperties.Life = reader.ReadSingle();
            asset.InlineProperties.LifeVariation = reader.ReadSingle();
            reader.ReadBytes(2); // pad_emit, always zero
            asset.CullMode = reader.ReadByte();
            reader.ReadByte(); // alignment padding, always zero
            asset.CullDistanceSquared = reader.ReadSingle();
            asset.InlineProperties.MaxEmit = reader.ReadByte();
            reader.ReadBytes(3); // trailing alignment padding, always zero
        }
        else
        {
            asset.Position = reader.ReadVector3();
            asset.Velocity = reader.ReadVector3();
            asset.VelocityAngleVariation = reader.ReadSingle();
            asset.CullMode = reader.ReadUInt32();
            asset.CullDistanceSquared = reader.ReadSingle();
        }

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ParticleEmitterAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write((byte)asset.Flags);
        writer.Write((byte)asset.Kind);

        if (profile.Game is GameVersion.N100F)
        {
            writer.Write(asset.InlineProperties.Count);
            writer.Write(asset.InlineProperties.CountVariation);
            writer.Write(asset.InlineProperties.Interval);
        }
        else
        {
            writer.Write((short)0); // pad
            writer.Write(asset.PropId);
        }

        ParticleEmitterShape.Write(asset.Shape, writer, profile);
        writer.Write(asset.AttachToId);

        if (profile.Game is GameVersion.N100F)
        {
            writer.Write(asset.InlineProperties.ParSysId);
            writer.Write(asset.Position);
            writer.Write(asset.Velocity);
            writer.Write(asset.VelocityAngleVariation);
            writer.WriteRgba32(asset.InlineProperties.ColorBirth);
            writer.WriteRgba32(asset.InlineProperties.ColorDeath);
            writer.Write(asset.InlineProperties.SizeBirth);
            writer.Write(asset.InlineProperties.SizeBirthVariation);
            writer.Write(asset.InlineProperties.SizeDeath);
            writer.Write(asset.InlineProperties.Life);
            writer.Write(asset.InlineProperties.LifeVariation);
            writer.Write(new byte[2]); // pad_emit
            writer.Write((byte)asset.CullMode);
            writer.Write((byte)0); // alignment padding
            writer.Write(asset.CullDistanceSquared);
            writer.Write(asset.InlineProperties.MaxEmit);
            writer.Write(new byte[3]); // trailing alignment padding
        }
        else
        {
            writer.Write(asset.Position);
            writer.Write(asset.Velocity);
            writer.Write(asset.VelocityAngleVariation);
            writer.Write(asset.CullMode);
            writer.Write(asset.CullDistanceSquared);
        }

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

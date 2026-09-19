using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class ParticleSystemAsset
{
    internal static ParticleSystemAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new ParticleSystemAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.SystemType = reader.ReadInt32();
        asset.ParentId = reader.ReadAssetId();
        asset.TextureId = reader.ReadAssetId();
        asset.Flags = (ParticleSystemFlags)reader.ReadByte();
        asset.Priority = reader.ReadByte();
        asset.MaxParticles = reader.ReadUInt16();
        asset.RenderFunction = (ParticleSystemRenderFunction)reader.ReadByte();
        asset.SourceBlend = (RwBlendFunction)(byte)(reader.ReadByte() + 1);
        asset.DestinationBlend = (RwBlendFunction)(byte)(reader.ReadByte() + 1);
        asset.Physical.CommandCount = reader.ReadByte();

        int commandDataSize = reader.ReadInt32();
        asset.Physical.CommandData = reader.ReadBytes(commandDataSize);

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ParticleSystemAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.SystemType);
        writer.Write(asset.ParentId);
        writer.Write(asset.TextureId);
        writer.Write((byte)asset.Flags);
        writer.Write(asset.Priority);
        writer.Write(asset.MaxParticles);
        writer.Write((byte)asset.RenderFunction);
        writer.Write((byte)(asset.SourceBlend - 1));
        writer.Write((byte)(asset.DestinationBlend - 1));
        writer.Write(asset.Physical.CommandCount);

        writer.Write(asset.Physical.CommandData.Length);
        writer.Write(asset.Physical.CommandData);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

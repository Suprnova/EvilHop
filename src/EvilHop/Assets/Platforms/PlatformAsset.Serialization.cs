using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class PlatformAsset
{
    /// <exception cref="InvalidDataException">
    /// The stored <see cref="PlatformType"/> is unknown, or its blocks don't hold what it selects.
    /// </exception>
    internal static PlatformAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new PlatformAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);

        // Subtype derives from PlatformType, which derives from Motion - reassigned once both are read.
        byte subtype = asset.Physical.Subtype;
        var platformType = (PlatformType)reader.ReadByte();
        reader.ReadByte(); // padding, always zero
        asset.Flags = (PlatformFlags)reader.ReadInt16();

        if (platformType <= PlatformType.Pendulum)
        {
            PlatformMotion.ReadEmpty(reader, profile.Game);
            asset.Motion = EntityMotion.Read(reader, profile.Game);
        }
        else
        {
            var motion = PlatformMotion.Read(reader, platformType, profile.Game);
            motion.Flags = EntityMotion.ReadEmpty(reader, profile.Game);
            asset.Motion = motion;
        }

        asset.Physical.PlatformType = platformType;
        asset.Physical.Subtype = subtype;

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PlatformAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);

        writer.Write((byte)asset.Physical.PlatformType);
        writer.Write((byte)0); // padding
        writer.Write((short)asset.Flags);

        switch (asset.Motion)
        {
            case EntityMotion motion:
                PlatformMotion.WriteEmpty(writer, profile.Game);
                motion.Write(writer, profile.Game);
                break;
            case PlatformMotion motion:
                motion.Write(writer, profile.Game);
                EntityMotion.WriteEmpty(writer, profile.Game, motion.Flags);
                break;
        }

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

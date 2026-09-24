using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Buffers.Binary;

namespace EvilHop.Assets;

public sealed partial class EnvironmentAsset
{
    internal static EnvironmentAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new EnvironmentAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.BspId = reader.ReadAssetId();
        asset.StartCameraId = reader.ReadAssetId();

        if (profile.EnvironmentHasExtendedFields)
        {
            asset.Climate = (Weather)reader.ReadUInt32();
            asset.ClimateStrengthMin = reader.ReadSingle();
            asset.ClimateStrengthMax = reader.ReadSingle();
            asset.BspLightKitId = reader.ReadAssetId();
            asset.ObjectLightKitId = reader.ReadAssetId();
            asset.Physical.EnvironmentFlags = reader.ReadUInt32();
            asset.BspCollisionId = reader.ReadAssetId();
            asset.BspFxId = reader.ReadAssetId();
            asset.BspCameraId = reader.ReadAssetId();
            asset.BspMapperId = reader.ReadAssetId();
            asset.BspMapperCollisionId = reader.ReadAssetId();
            asset.BspMapperFxId = reader.ReadAssetId();
        }

        if (profile.Game is not GameVersion.N100F)
            asset.Physical.LoldHeight = BinaryPrimitives.ReadSingleLittleEndian(reader.ReadBytes(4));

        if (profile.Game is GameVersion.TSSM or GameVersion.Incredibles or GameVersion.ROTU or GameVersion.Ratatouille)
        {
            asset.MinBounds = reader.ReadVector3();
            asset.MaxBounds = reader.ReadVector3();
        }

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(EnvironmentAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.BspId);
        writer.Write(asset.StartCameraId);

        if (profile.EnvironmentHasExtendedFields)
        {
            writer.Write((uint)asset.Climate);
            writer.Write(asset.ClimateStrengthMin);
            writer.Write(asset.ClimateStrengthMax);
            writer.Write(asset.BspLightKitId);
            writer.Write(asset.ObjectLightKitId);
            writer.Write(asset.Physical.EnvironmentFlags);
            writer.Write(asset.BspCollisionId);
            writer.Write(asset.BspFxId);
            writer.Write(asset.BspCameraId);
            writer.Write(asset.BspMapperId);
            writer.Write(asset.BspMapperCollisionId);
            writer.Write(asset.BspMapperFxId);
        }

        if (profile.Game is not GameVersion.N100F)
        {
            Span<byte> loldHeight = stackalloc byte[4];
            BinaryPrimitives.WriteSingleLittleEndian(loldHeight, asset.Physical.LoldHeight);
            writer.Write(loldHeight);
        }

        if (profile.Game is GameVersion.TSSM or GameVersion.Incredibles or GameVersion.ROTU or GameVersion.Ratatouille)
        {
            writer.Write(asset.MinBounds);
            writer.Write(asset.MaxBounds);
        }

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

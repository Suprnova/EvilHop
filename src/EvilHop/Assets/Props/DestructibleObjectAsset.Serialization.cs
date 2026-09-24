using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class DestructibleObjectAsset
{
    internal static DestructibleObjectAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new DestructibleObjectAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        asset.AnimationSpeed = reader.ReadSingle();
        asset.InitialAnimationState = reader.ReadUInt32();
        asset.Health = reader.ReadUInt32();
        asset.SpawnItemId = reader.ReadAssetId();
        asset.HitFlags = (DamageSource)reader.ReadUInt32();
        asset.CollisionType = reader.ReadByte();
        asset.FxType = (Effect)reader.ReadByte();
        reader.ReadInt16(); // padding, always zero
        asset.BlastRadius = reader.ReadSingle();
        asset.BlastStrength = reader.ReadSingle();

        if (profile.Game is GameVersion.BFBB)
        {
            asset.DestroyShrapnelId = reader.ReadAssetId();
            asset.HitShrapnelId = reader.ReadAssetId();
            asset.DestroySfxId = reader.ReadAssetId();

            if (profile.DestructibleObjectHasSwapEffects)
            {
                asset.HitSfxId = reader.ReadAssetId();
                asset.HitModelId = reader.ReadAssetId();
                asset.DestroyModelId = reader.ReadAssetId();
            }
        }

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(DestructibleObjectAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        writer.Write(asset.AnimationSpeed);
        writer.Write(asset.InitialAnimationState);
        writer.Write(asset.Health);
        writer.Write(asset.SpawnItemId);
        writer.Write((uint)asset.HitFlags);
        writer.Write(asset.CollisionType);
        writer.Write((byte)asset.FxType);
        writer.Write((short)0); // padding
        writer.Write(asset.BlastRadius);
        writer.Write(asset.BlastStrength);

        if (profile.Game is GameVersion.BFBB)
        {
            writer.Write(asset.DestroyShrapnelId);
            writer.Write(asset.HitShrapnelId);
            writer.Write(asset.DestroySfxId);

            if (profile.DestructibleObjectHasSwapEffects)
            {
                writer.Write(asset.HitSfxId);
                writer.Write(asset.HitModelId);
                writer.Write(asset.DestroyModelId);
            }
        }

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

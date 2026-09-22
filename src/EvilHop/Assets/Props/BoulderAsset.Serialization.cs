using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class BoulderAsset
{
    internal static BoulderAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new BoulderAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        asset.Gravity = reader.ReadSingle();
        asset.Mass = reader.ReadSingle();
        asset.Bounce = reader.ReadSingle();
        asset.Friction = reader.ReadSingle();
        if (profile.Game is GameVersion.BFBB) asset.StaticFriction = reader.ReadSingle();
        asset.MaxVelocity = reader.ReadSingle();
        asset.MaxAngularVelocity = reader.ReadSingle();
        asset.Stickiness = reader.ReadSingle();
        asset.BounceDamping = reader.ReadSingle();
        asset.Flags = (Behavior)reader.ReadUInt32();
        asset.KillTimer = reader.ReadSingle();
        asset.Hitpoints = reader.ReadUInt32();
        asset.BounceSoundId = reader.ReadAssetId();
        if (profile.Game is GameVersion.BFBB) asset.Volume = reader.ReadSingle();
        asset.MinSoundVelocity = reader.ReadSingle();
        if (profile.BoulderHasSoundFalloff) asset.MaxSoundVelocity = reader.ReadSingle();

        if (profile.Game is GameVersion.BFBB)
        {
            if (profile.BoulderHasSoundFalloff)
            {
                asset.InnerRadius = reader.ReadSingle();
                asset.OuterRadius = reader.ReadSingle();
            }
        }
        else
        {
            asset.SoundRadius = reader.ReadSingle();
            reader.ReadBytes(3); // padding, always zero
            asset.BoneIndex = reader.ReadByte();
            if (profile.Game is GameVersion.ROTU) asset.InitialNonCollideTime = reader.ReadSingle();
        }

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(BoulderAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        writer.Write(asset.Gravity);
        writer.Write(asset.Mass);
        writer.Write(asset.Bounce);
        writer.Write(asset.Friction);
        if (profile.Game is GameVersion.BFBB) writer.Write(asset.StaticFriction);
        writer.Write(asset.MaxVelocity);
        writer.Write(asset.MaxAngularVelocity);
        writer.Write(asset.Stickiness);
        writer.Write(asset.BounceDamping);
        writer.Write((uint)asset.Flags);
        writer.Write(asset.KillTimer);
        writer.Write(asset.Hitpoints);
        writer.Write(asset.BounceSoundId);
        if (profile.Game is GameVersion.BFBB) writer.Write(asset.Volume);
        writer.Write(asset.MinSoundVelocity);
        if (profile.BoulderHasSoundFalloff) writer.Write(asset.MaxSoundVelocity);

        if (profile.Game is GameVersion.BFBB)
        {
            if (profile.BoulderHasSoundFalloff)
            {
                writer.Write(asset.InnerRadius);
                writer.Write(asset.OuterRadius);
            }
        }
        else
        {
            writer.Write(asset.SoundRadius);
            writer.Write((byte)0); // pad0
            writer.Write((byte)0); // pad1
            writer.Write((byte)0); // pad2
            writer.Write(asset.BoneIndex);
            if (profile.Game is GameVersion.ROTU) writer.Write(asset.InitialNonCollideTime);
        }

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class SurfaceAsset
{
    internal static SurfaceAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SurfaceAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Damage = (DamageKind)reader.ReadByte();
        asset.Physical.GameSticky = reader.ReadByte();
        asset.Physical.GameDamageFlags = (DamageBehavior)reader.ReadByte();
        asset.Physical.SurfType = reader.ReadByte();
        reader.ReadByte(); // padding, always zero
        asset.SlideStartAngle = reader.ReadByte();
        asset.SlideStopAngle = reader.ReadByte();
        asset.PhysFlags = (PhysicsBehavior)reader.ReadByte();
        asset.Friction = reader.ReadSingle();

        asset.MaterialFx = MaterialEffect.Read(reader, profile);
        asset.ColorFx = ColorEffect.Read(reader, profile);

        var textureAnimFlags = (AnimationSlot)reader.ReadUInt32();
        var textureAnim0 = TextureEffect.Read(reader, profile);
        var textureAnim1 = TextureEffect.Read(reader, profile);
        textureAnim0.IsEnabled = textureAnimFlags.HasFlag(AnimationSlot.Slot0);
        textureAnim1.IsEnabled = textureAnimFlags.HasFlag(AnimationSlot.Slot1);
        asset.TextureAnims = [textureAnim0, textureAnim1];
        asset.Physical.TextureAnimFlags = textureAnimFlags;

        var uvfxFlags = (UVSlot)reader.ReadUInt32();
        var uvfx0 = UVEffect.Read(reader, profile);
        var uvfx1 = UVEffect.Read(reader, profile);
        uvfx0.IsEnabled = uvfxFlags.HasFlag(UVSlot.Slot0);
        uvfx1.IsEnabled = uvfxFlags.HasFlag(UVSlot.Slot1);
        asset.Uvfxs = [uvfx0, uvfx1];
        asset.Physical.UvfxFlags = uvfxFlags;

        asset.Physical.IsEnabled = reader.ReadByte();
        reader.ReadBytes(3); // padding, always zero
        if (profile.Game is not GameVersion.N100F)
        {
            asset.OutOfBoundsDelay = reader.ReadSingle();
            asset.WallJumpScaleXZ = reader.ReadSingle();
            asset.WallJumpScaleY = reader.ReadSingle();
            if (profile.SurfaceHasDamageFields)
            {
                asset.DamageTimer = reader.ReadSingle();
                asset.DamageBounce = reader.ReadSingle();
            }
        }
        if (profile.Game is not (GameVersion.N100F or GameVersion.BFBB))
            ReadExtendedFields(asset, reader, profile);

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SurfaceAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write((byte)asset.Damage);
        writer.Write(asset.Physical.GameSticky);
        writer.Write((byte)asset.Physical.GameDamageFlags);
        writer.Write(asset.Physical.SurfType);
        writer.Write((byte)0); // padding
        writer.Write(asset.SlideStartAngle);
        writer.Write(asset.SlideStopAngle);
        writer.Write((byte)asset.PhysFlags);
        writer.Write(asset.Friction);

        MaterialEffect.Write(asset.MaterialFx, writer, profile);
        ColorEffect.Write(asset.ColorFx, writer, profile);

        writer.Write((uint)asset.Physical.TextureAnimFlags);
        TextureEffect.Write(asset.TextureAnims[0], writer, profile);
        TextureEffect.Write(asset.TextureAnims[1], writer, profile);

        writer.Write((uint)asset.Physical.UvfxFlags);
        UVEffect.Write(asset.Uvfxs[0], writer, profile);
        UVEffect.Write(asset.Uvfxs[1], writer, profile);

        writer.Write(asset.Physical.IsEnabled);
        writer.Write(new byte[3]); // padding
        if (profile.Game is not GameVersion.N100F)
        {
            writer.Write(asset.OutOfBoundsDelay);
            writer.Write(asset.WallJumpScaleXZ);
            writer.Write(asset.WallJumpScaleY);
            if (profile.SurfaceHasDamageFields)
            {
                writer.Write(asset.DamageTimer);
                writer.Write(asset.DamageBounce);
            }
        }
        if (profile.Game is not (GameVersion.N100F or GameVersion.BFBB))
            WriteExtendedFields(asset, writer, profile);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    private static void ReadExtendedFields(SurfaceAsset asset, EndianReader reader, FormatProfile profile)
    {
        asset.Physical.ImpactSound = reader.ReadAssetId();
        asset.Physical.DashImpactType = reader.ReadByte();
        reader.ReadBytes(3); // padding, always zero
        asset.Physical.DashImpactThrowBack = reader.ReadSingle();
        asset.Physical.DashSprayMagnitude = reader.ReadSingle();
        asset.Physical.DashCoolRate = reader.ReadSingle();
        asset.Physical.DashCoolAmount = reader.ReadSingle();
        asset.Physical.DashPass = reader.ReadSingle();
        asset.Physical.DashRampMaxDistance = reader.ReadSingle();
        asset.Physical.DashRampMinDistance = reader.ReadSingle();
        asset.Physical.DashRampKeySpeed = reader.ReadSingle();
        asset.Physical.DashRampHeight = reader.ReadSingle();
        asset.Physical.DashRampTarget = reader.ReadAssetId();
        asset.DamageAmount = reader.ReadInt32();
        asset.DamageSource = reader.ReadInt32();
        asset.OffSurfaceFootsteps = FootstepEffect.Read(reader, profile);
        asset.OnSurfaceFootsteps = FootstepEffect.Read(reader, profile);
        asset.Physical.HitDecals = [HitDecal.Read(reader, profile), HitDecal.Read(reader, profile), HitDecal.Read(reader, profile)];
        asset.OffSurfaceTime = reader.ReadSingle();
        asset.Physical.Swimmable = reader.ReadByte();
        asset.Physical.DashFall = reader.ReadByte();
        asset.Physical.NeedButtonPress = reader.ReadByte();
        asset.Physical.DashAttach = reader.ReadByte();
        asset.Physical.FootstepDecals = reader.ReadByte();
        reader.ReadBytes(4); // padding, always zero
        asset.Physical.DrivingSurfaceType = reader.ReadByte();
        reader.ReadBytes(2); // padding, always zero
    }

    private static void WriteExtendedFields(SurfaceAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.ImpactSound);
        writer.Write(asset.Physical.DashImpactType);
        writer.Write(new byte[3]); // padding
        writer.Write(asset.Physical.DashImpactThrowBack);
        writer.Write(asset.Physical.DashSprayMagnitude);
        writer.Write(asset.Physical.DashCoolRate);
        writer.Write(asset.Physical.DashCoolAmount);
        writer.Write(asset.Physical.DashPass);
        writer.Write(asset.Physical.DashRampMaxDistance);
        writer.Write(asset.Physical.DashRampMinDistance);
        writer.Write(asset.Physical.DashRampKeySpeed);
        writer.Write(asset.Physical.DashRampHeight);
        writer.Write(asset.Physical.DashRampTarget);
        writer.Write(asset.DamageAmount);
        writer.Write(asset.DamageSource);
        FootstepEffect.Write(asset.OffSurfaceFootsteps, writer, profile);
        FootstepEffect.Write(asset.OnSurfaceFootsteps, writer, profile);
        foreach (var decal in asset.Physical.HitDecals)
            HitDecal.Write(decal, writer, profile);
        writer.Write(asset.OffSurfaceTime);
        writer.Write(asset.Physical.Swimmable);
        writer.Write(asset.Physical.DashFall);
        writer.Write(asset.Physical.NeedButtonPress);
        writer.Write(asset.Physical.DashAttach);
        writer.Write(asset.Physical.FootstepDecals);
        writer.Write(new byte[4]); // padding
        writer.Write(asset.Physical.DrivingSurfaceType);
        writer.Write(new byte[2]); // padding
    }
}

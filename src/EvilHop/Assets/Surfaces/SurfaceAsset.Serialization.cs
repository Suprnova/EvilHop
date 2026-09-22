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
        asset.OutOfBoundsDelay = reader.ReadSingle();
        asset.WallJumpScaleXZ = reader.ReadSingle();
        asset.WallJumpScaleY = reader.ReadSingle();
        if (profile.SurfaceHasDamageFields)
        {
            asset.DamageTimer = reader.ReadSingle();
            asset.DamageBounce = reader.ReadSingle();
        }

        // Whatever's left before the links - 0 in most BFBB archives, otherwise a game/build-specific
        // amount of unmodelled data. Computed rather than assumed, so every observed size round-trips.
        int remaining = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        asset.ExtendedData = [.. reader.ReadBytes(remaining - 32 * asset.Physical.LinkCount)];

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
        writer.Write(asset.OutOfBoundsDelay);
        writer.Write(asset.WallJumpScaleXZ);
        writer.Write(asset.WallJumpScaleY);
        if (profile.SurfaceHasDamageFields)
        {
            writer.Write(asset.DamageTimer);
            writer.Write(asset.DamageBounce);
        }

        writer.Write(asset.ExtendedData.AsSpan());

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

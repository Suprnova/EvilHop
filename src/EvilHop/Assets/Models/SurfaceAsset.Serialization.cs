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

        asset.GameDamageType = (SurfaceGameDamageType)reader.ReadByte();
        asset.Physical.GameSticky = reader.ReadByte();
        asset.Physical.GameDamageFlags = (SurfaceGameDamageFlags)reader.ReadByte();
        asset.Physical.SurfType = reader.ReadByte();
        reader.ReadByte(); // padding, always zero
        asset.SlideStartAngle = reader.ReadByte();
        asset.SlideStopAngle = reader.ReadByte();
        asset.PhysFlags = (SurfacePhysicsFlags)reader.ReadByte();
        asset.Friction = reader.ReadSingle();

        asset.MaterialFx = new SurfaceMaterialFx
        {
            Flags = (SurfaceMaterialFxFlags)reader.ReadUInt32(),
            BumpMapId = reader.ReadAssetId(),
            EnvMapId = reader.ReadAssetId(),
            Shininess = reader.ReadSingle(),
            Bumpiness = reader.ReadSingle(),
            DualMapId = reader.ReadAssetId(),
        };

        asset.ColorFx = new SurfaceColorFx
        {
            Flags = (SurfaceColorFxFlags)reader.ReadUInt16(),
            Mode = reader.ReadUInt16(),
            Speed = reader.ReadSingle(),
        };

        var textureAnimFlags = (SurfaceTextureAnimFlags)reader.ReadUInt32();
        var textureAnim0 = ReadTextureAnim(reader);
        var textureAnim1 = ReadTextureAnim(reader);
        textureAnim0.IsEnabled = textureAnimFlags.HasFlag(SurfaceTextureAnimFlags.Slot0);
        textureAnim1.IsEnabled = textureAnimFlags.HasFlag(SurfaceTextureAnimFlags.Slot1);
        asset.TextureAnims = [textureAnim0, textureAnim1];
        asset.Physical.TextureAnimFlags = textureAnimFlags;

        var uvfxFlags = (SurfaceUvfxFlags)reader.ReadUInt32();
        var uvfx0 = ReadUvfx(reader);
        var uvfx1 = ReadUvfx(reader);
        uvfx0.IsEnabled = uvfxFlags.HasFlag(SurfaceUvfxFlags.Slot0);
        uvfx1.IsEnabled = uvfxFlags.HasFlag(SurfaceUvfxFlags.Slot1);
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

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SurfaceAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write((byte)asset.GameDamageType);
        writer.Write(asset.Physical.GameSticky);
        writer.Write((byte)asset.Physical.GameDamageFlags);
        writer.Write(asset.Physical.SurfType);
        writer.Write((byte)0); // padding
        writer.Write(asset.SlideStartAngle);
        writer.Write(asset.SlideStopAngle);
        writer.Write((byte)asset.PhysFlags);
        writer.Write(asset.Friction);

        writer.Write((uint)asset.MaterialFx.Flags);
        writer.Write(asset.MaterialFx.BumpMapId);
        writer.Write(asset.MaterialFx.EnvMapId);
        writer.Write(asset.MaterialFx.Shininess);
        writer.Write(asset.MaterialFx.Bumpiness);
        writer.Write(asset.MaterialFx.DualMapId);

        writer.Write((ushort)asset.ColorFx.Flags);
        writer.Write(asset.ColorFx.Mode);
        writer.Write(asset.ColorFx.Speed);

        writer.Write((uint)asset.Physical.TextureAnimFlags);
        WriteTextureAnim(writer, asset.TextureAnims[0]);
        WriteTextureAnim(writer, asset.TextureAnims[1]);

        writer.Write((uint)asset.Physical.UvfxFlags);
        WriteUvfx(writer, asset.Uvfxs[0]);
        WriteUvfx(writer, asset.Uvfxs[1]);

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

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }

    private static SurfaceTextureAnim ReadTextureAnim(EndianReader reader)
    {
        reader.ReadUInt16(); // padding, always zero
        var mode = (SurfaceTextureAnimMode)reader.ReadUInt16();
        return new SurfaceTextureAnim
        {
            Mode = mode,
            Group = reader.ReadAssetId(),
            Speed = reader.ReadSingle(),
        };
    }

    private static void WriteTextureAnim(EndianWriter writer, SurfaceTextureAnim anim)
    {
        writer.Write((ushort)0); // padding
        writer.Write((ushort)anim.Mode);
        writer.Write(anim.Group);
        writer.Write(anim.Speed);
    }

    private static SurfaceUvfx ReadUvfx(EndianReader reader) => new()
    {
        Mode = (SurfaceUvfxMode)reader.ReadInt32(),
        Rotation = reader.ReadSingle(),
        RotationSpeed = reader.ReadSingle(),
        Translation = reader.ReadVector3(),
        TranslationSpeed = reader.ReadVector3(),
        Scale = reader.ReadVector3(),
        ScaleSpeed = reader.ReadVector3(),
        Min = reader.ReadVector3(),
        Max = reader.ReadVector3(),
        MinMaxSpeed = reader.ReadVector3(),
    };

    private static void WriteUvfx(EndianWriter writer, SurfaceUvfx uvfx)
    {
        writer.Write((int)uvfx.Mode);
        writer.Write(uvfx.Rotation);
        writer.Write(uvfx.RotationSpeed);
        writer.Write(uvfx.Translation);
        writer.Write(uvfx.TranslationSpeed);
        writer.Write(uvfx.Scale);
        writer.Write(uvfx.ScaleSpeed);
        writer.Write(uvfx.Min);
        writer.Write(uvfx.Max);
        writer.Write(uvfx.MinMaxSpeed);
    }
}

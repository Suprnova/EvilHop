using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class SurfaceAsset
{
    internal static SurfaceAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new SurfaceAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.GameDamageType = (SurfaceGameDamageType)reader.ReadByte();
        asset.Physical.GameSticky = reader.ReadByte();
        asset.GameDamageFlags = (SurfaceGameDamageFlags)reader.ReadByte();
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
            // TODO: double cast? why?
            Flags = (SurfaceColorFxFlags)(ushort)reader.ReadInt16(),
            Mode = (ushort)reader.ReadInt16(),
            Speed = reader.ReadSingle(),
        };

        uint textureAnimFlags = reader.ReadUInt32();
        var textureAnim0 = ReadTextureAnim(reader);
        var textureAnim1 = ReadTextureAnim(reader);
        textureAnim0.IsEnabled = (textureAnimFlags & 1 << 0) != 0;
        textureAnim1.IsEnabled = (textureAnimFlags & 1 << 1) != 0;
        asset.TextureAnims = [textureAnim0, textureAnim1];
        asset.Physical.TextureAnimFlags = textureAnimFlags;

        uint uvfxFlags = reader.ReadUInt32();
        var uvfx0 = ReadUvfx(reader);
        var uvfx1 = ReadUvfx(reader);
        uvfx0.IsEnabled = (uvfxFlags & 1 << 0) != 0;
        uvfx1.IsEnabled = (uvfxFlags & 1 << 1) != 0;
        asset.Uvfxs = [uvfx0, uvfx1];
        asset.Physical.UvfxFlags = uvfxFlags;

        asset.Physical.OnValue = reader.ReadByte();
        reader.ReadBytes(3); // padding, always zero
        asset.OutOfBoundsDelay = reader.ReadSingle();
        asset.WallJumpScaleXZ = reader.ReadSingle();
        asset.WallJumpScaleY = reader.ReadSingle();
        asset.Physical.DamageTimer = reader.ReadSingle();
        asset.Physical.DamageBounce = reader.ReadSingle();

        // Whatever's left before the links - 0 in most BFBB archives, otherwise a game/build-specific
        // amount of unmodelled data. Computed rather than assumed, so every observed size round-trips.
        int remaining = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        asset.ExtendedData = [.. reader.ReadBytes(remaining - 32 * asset.Physical.LinkCount)];

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SurfaceAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write((byte)asset.GameDamageType);
        writer.Write(asset.Physical.GameSticky);
        writer.Write((byte)asset.GameDamageFlags);
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

        // TODO: ditto; double cast?
        writer.Write((short)(ushort)asset.ColorFx.Flags);
        writer.Write((short)asset.ColorFx.Mode);
        writer.Write(asset.ColorFx.Speed);

        writer.Write(asset.Physical.TextureAnimFlags);
        WriteTextureAnim(writer, asset.TextureAnims[0]);
        WriteTextureAnim(writer, asset.TextureAnims[1]);

        writer.Write(asset.Physical.UvfxFlags);
        WriteUvfx(writer, asset.Uvfxs[0]);
        WriteUvfx(writer, asset.Uvfxs[1]);

        writer.Write(asset.Physical.OnValue);
        writer.Write(new byte[3]); // padding
        writer.Write(asset.OutOfBoundsDelay);
        writer.Write(asset.WallJumpScaleXZ);
        writer.Write(asset.WallJumpScaleY);
        writer.Write(asset.Physical.DamageTimer);
        writer.Write(asset.Physical.DamageBounce);

        writer.Write(asset.ExtendedData.AsSpan());

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }

    private static SurfaceTextureAnim ReadTextureAnim(EndianReader reader)
    {
        reader.ReadInt16(); // padding, always zero
        var mode = (SurfaceTextureAnimMode)(ushort)reader.ReadInt16();
        return new SurfaceTextureAnim
        {
            Mode = mode,
            Group = reader.ReadAssetId(),
            Speed = reader.ReadSingle(),
        };
    }

    private static void WriteTextureAnim(EndianWriter writer, SurfaceTextureAnim anim)
    {
        writer.Write((short)0); // padding
        writer.Write((short)(ushort)anim.Mode);
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

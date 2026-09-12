using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class SurfaceAsset
{
    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Surface"/> is known to be read by.
    /// </summary>
    /// <remarks>
    /// <see cref="GameVersion.N100F"/>'s <c>SURF</c> layout is substantially smaller than every
    /// other game's and does not match decompiled source; it is not modelled here.
    /// </remarks>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static SurfaceAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SurfaceAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.GameDamageType = reader.ReadByte();
        asset.GameSticky = reader.ReadByte();
        asset.GameDamageFlags = (SurfaceGameDamageFlags)reader.ReadByte();
        asset.Physical.SurfType = reader.ReadByte();
        reader.ReadByte(); // phys_pad, always zero
        asset.SlideStartAngle = reader.ReadByte();
        asset.SlideStopAngle = reader.ReadByte();
        asset.PhysFlags = (SurfacePhysicsFlags)reader.ReadByte();
        asset.Friction = reader.ReadSingle();

        asset.MaterialFx = new SurfaceMaterialFx
        {
            Flags = reader.ReadUInt32(),
            BumpMapId = reader.ReadAssetId(),
            EnvMapId = reader.ReadAssetId(),
            Shininess = reader.ReadSingle(),
            Bumpiness = reader.ReadSingle(),
            DualMapId = reader.ReadAssetId(),
        };

        asset.ColorFx = new SurfaceColorFx
        {
            Flags = (ushort)reader.ReadInt16(),
            Mode = (ushort)reader.ReadInt16(),
            Speed = reader.ReadSingle(),
        };

        asset.TextureAnimFlags = (SurfaceTextureAnimFlags)reader.ReadUInt32();
        asset.TextureAnims = [ReadTextureAnim(reader), ReadTextureAnim(reader)];

        asset.UvfxFlags = (SurfaceUvfxFlags)reader.ReadUInt32();
        asset.Uvfxs = [ReadUvfx(reader), ReadUvfx(reader)];

        asset.On = reader.ReadByte();
        reader.ReadBytes(3); // surf_pad, always zero
        asset.OutOfBoundsDelay = reader.ReadSingle();
        asset.WallJumpScaleXZ = reader.ReadSingle();
        asset.WallJumpScaleY = reader.ReadSingle();
        asset.DamageTimer = reader.ReadSingle();
        asset.DamageBounce = reader.ReadSingle();

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

        writer.Write(asset.GameDamageType);
        writer.Write(asset.GameSticky);
        writer.Write((byte)asset.GameDamageFlags);
        writer.Write(asset.Physical.SurfType);
        writer.Write((byte)0); // phys_pad
        writer.Write(asset.SlideStartAngle);
        writer.Write(asset.SlideStopAngle);
        writer.Write((byte)asset.PhysFlags);
        writer.Write(asset.Friction);

        writer.Write(asset.MaterialFx.Flags);
        writer.Write(asset.MaterialFx.BumpMapId);
        writer.Write(asset.MaterialFx.EnvMapId);
        writer.Write(asset.MaterialFx.Shininess);
        writer.Write(asset.MaterialFx.Bumpiness);
        writer.Write(asset.MaterialFx.DualMapId);

        writer.Write((short)asset.ColorFx.Flags);
        writer.Write((short)asset.ColorFx.Mode);
        writer.Write(asset.ColorFx.Speed);

        writer.Write((uint)asset.TextureAnimFlags);
        WriteTextureAnim(writer, asset.TextureAnims[0]);
        WriteTextureAnim(writer, asset.TextureAnims[1]);

        writer.Write((uint)asset.UvfxFlags);
        WriteUvfx(writer, asset.Uvfxs[0]);
        WriteUvfx(writer, asset.Uvfxs[1]);

        writer.Write(asset.On);
        writer.Write(new byte[3]); // surf_pad
        writer.Write(asset.OutOfBoundsDelay);
        writer.Write(asset.WallJumpScaleXZ);
        writer.Write(asset.WallJumpScaleY);
        writer.Write(asset.DamageTimer);
        writer.Write(asset.DamageBounce);

        writer.Write(asset.ExtendedData.AsSpan());

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }

    private static SurfaceTextureAnim ReadTextureAnim(EndianReader reader)
    {
        reader.ReadInt16(); // pad, always zero
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
        writer.Write((short)0); // pad
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

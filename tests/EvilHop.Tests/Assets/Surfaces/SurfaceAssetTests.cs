using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;
using static EvilHop.Assets.SurfaceAsset;

namespace EvilHop.Tests.Serialization;

public class SurfaceAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Surface;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile profile)
    {
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile profile)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x1A,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] U16(ushort value) => [(byte)(value >> 8), (byte)value];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Vec3(float x, float y, float z) => [.. F32(x), .. F32(y), .. F32(z)];

    private static byte[] TextureAnim(ushort mode, uint group, float speed) =>
    [
        .. U16(0), // pad
        .. U16(mode),
        .. U32(group),
        .. F32(speed),
    ];

    private static byte[] Uvfx(int mode, float rot, float rotSpd, Vector3 trans, Vector3 transSpd,
        Vector3 scale, Vector3 scaleSpd, Vector3 min, Vector3 max, Vector3 minMaxSpd) =>
    [
        .. U32((uint)mode),
        .. F32(rot),
        .. F32(rotSpd),
        .. Vec3(trans.X, trans.Y, trans.Z),
        .. Vec3(transSpd.X, transSpd.Y, transSpd.Z),
        .. Vec3(scale.X, scale.Y, scale.Z),
        .. Vec3(scaleSpd.X, scaleSpd.Y, scaleSpd.Z),
        .. Vec3(min.X, min.Y, min.Z),
        .. Vec3(max.X, max.Y, max.Z),
        .. Vec3(minMaxSpd.X, minMaxSpd.Y, minMaxSpd.Z),
    ];

    private static readonly byte[] DefaultUvfx = Uvfx(0, 0, 0, default, default, new Vector3(1, 1, 0), default, default, default, default);

    private static byte[] Core(byte gameDamageType, byte phys_flags, float friction, byte on) =>
    [
        gameDamageType,
        0x00,             // game_sticky
        0x00,             // game_damage_flags
        0x00,             // surf_type
        0x00,             // phys_pad
        0x14,             // sld_start = 20
        0x0A,             // sld_stop = 10
        phys_flags,
        .. F32(friction),
        .. U32(0), .. U32(0), .. U32(0), .. F32(0), .. F32(0), .. U32(0), // matfx
        .. U16(0x000E), .. U16(0), .. F32(30f),                          // colorfx
        .. U32(0),                                                       // texture_anim_flags
        .. TextureAnim(0, 0, 0), .. TextureAnim(0, 0, 0),                 // texture_anim[2]
        .. U32(1),                                                       // uvfx_flags
        .. DefaultUvfx, .. DefaultUvfx,                                   // uvfx[2]
        on,
        0x00, 0x00, 0x00, // surf_pad
        .. F32(-1f),      // oob_delay
        .. F32(1f),       // walljump_scale_xz
        .. F32(1f),       // walljump_scale_y
        .. F32(0f),       // damage_timer
        .. F32(0f),       // damage_bounce
    ];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] BfbbData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. Core(gameDamageType: 6, phys_flags: 0x10, friction: 1.0f, on: 1),
    ];

    private static byte[] Extension() =>
    [
        .. U32(0xCAFEF00D),                                      // impact_sound
        0x02, 0x00, 0x00, 0x00,                                  // dash_impact_type + pad
        .. F32(5f), .. F32(1f), .. F32(0.05f), .. F32(0.3f), .. F32(0f), // dash_impact_throw_back..dash_pass
        .. F32(30f), .. F32(20f), .. F32(25f), .. F32(10f),      // dash_ramp_max_distance..dash_ramp_height
        .. U32(0x0BADBEEF),                                      // dash_ramp_target_movepoint_id
        .. U32(100),                                             // damage_amount
        .. U32(6),                                               // damage_type
        .. U32(0x1111), .. U32(0x2222), .. U32(0x3333), .. F32(2f), // off_surface
        .. U32(0x4444), .. U32(0x5555), .. U32(0x6666), .. F32(3f), // on_surface
        .. U32(0x7777), .. F32(0.5f), .. F32(0.25f),             // hit_decal_data[0]
        .. new byte[24],                                         // hit_decal_data[1..2]
        .. F32(1.5f),                                            // off_surface_time
        0x01,                                                    // swimmable_surface
        0x01,                                                    // dash_fall
        0x00,                                                    // need_button_press
        0x01,                                                    // dash_attach
        0x00,                                                    // footstep_decals
        0x00, 0x00, 0x00, 0x00,                                  // pad1..pad4
        0x0B,                                                    // driving_surface_type
        0x00, 0x00,                                              // struct padding
    ];

    private static byte[] TssmData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. Core(gameDamageType: 0, phys_flags: 0, friction: 1.0f, on: 1),
        .. Extension(),
    ];

    private static byte[] N100FUvfx(float rotSpd) =>
    [
        .. U32(0),
        .. F32(0),
        .. F32(rotSpd),
        .. Vec3(0, 0, 0),
        .. Vec3(0, 0, 0),
        .. Vec3(1, 1, 0),
        .. Vec3(0, 0, 0),
    ];

    private static byte[] N100FData()
    {
        byte[] core = Core(gameDamageType: 1, phys_flags: 0, friction: 0.5f, on: 1);
        const int uvfxStart = 0x54 - 8;
        return
        [
            .. Prefix(linkCount: 0),
            .. core[..uvfxStart],
            .. N100FUvfx(rotSpd: 45f), .. N100FUvfx(rotSpd: 0f),
            .. core[(uvfxStart + 2 * DefaultUvfx.Length)..^20], // on + surf_pad
        ];
    }

    [Fact]
    public void Read_Surface_ProducesSurfaceAsset() =>
        Assert.IsType<SurfaceAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_Surface_UnderBfbb_PopulatesEveryField()
    {
        var asset = (SurfaceAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(DamageKind.Damage6, asset.Damage);
        Assert.False(asset.DamagePassthrough);
        Assert.Equal(DamageBehavior.None, asset.Physical.GameDamageFlags);
        Assert.Equal(0, asset.Physical.GameSticky);
        Assert.Equal(0, asset.Physical.SurfType);
        Assert.Equal(20, asset.SlideStartAngle);
        Assert.Equal(10, asset.SlideStopAngle);
        Assert.Equal(PhysicsBehavior.OutOfBounds, asset.PhysFlags);
        Assert.Equal(1.0f, asset.Friction);
        Assert.Equal(ColorEffect.ColorFlags.Valid, asset.ColorFx.Flags);
        Assert.Equal(30f, asset.ColorFx.Speed);
        Assert.Equal(2, asset.TextureAnims.Length);
        Assert.Equal(AnimationSlot.None, asset.Physical.TextureAnimFlags);
        Assert.Equal(2, asset.Uvfxs.Length);
        Assert.Equal(UVSlot.Slot0, asset.Physical.UvfxFlags);
        Assert.Equal(new Vector3(1, 1, 0), asset.Uvfxs[0].Scale);
        Assert.True(asset.Uvfxs[0].IsEnabled);
        Assert.False(asset.Uvfxs[1].IsEnabled);
        Assert.True(asset.StartsOn);
        Assert.Equal(-1f, asset.OutOfBoundsDelay);
        Assert.Equal(1f, asset.WallJumpScaleXZ);
        Assert.Equal(1f, asset.WallJumpScaleY);
        Assert.Equal(0f, asset.DamageTimer);
        Assert.Equal(0f, asset.DamageBounce);
    }

    [Fact]
    public void Read_ThenWrite_SurfaceUnderBfbb_ReproducesInputBytes()
    {
        byte[] data = BfbbData();
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SurfaceWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_Surface_UnderTssm_PopulatesExtendedFields()
    {
        var asset = (SurfaceAsset)Read(TssmData(), TSSMSerializer.DefaultProfile);

        Assert.Equal(new AssetId(0xCAFEF00D), asset.Physical.ImpactSound);
        Assert.Equal(2, asset.Physical.DashImpactType);
        Assert.Equal(5f, asset.Physical.DashImpactThrowBack);
        Assert.Equal(10f, asset.Physical.DashRampHeight);
        Assert.Equal(new AssetId(0x0BADBEEF), asset.Physical.DashRampTarget);
        Assert.Equal(100, asset.DamageAmount);
        Assert.Equal(6, asset.DamageSource);
        Assert.Equal(new AssetId(0x2222), asset.OffSurfaceFootsteps.Sound);
        Assert.Equal(2f, asset.OffSurfaceFootsteps.Duration);
        Assert.Equal(new AssetId(0x4444), asset.OnSurfaceFootsteps.ParticleEmitter);
        Assert.Equal(new AssetId(0x6666), asset.OnSurfaceFootsteps.Texture);
        Assert.Equal(new HitDecal(new AssetId(0x7777), 0.5f, 0.25f), asset.Physical.HitDecals[0]);
        Assert.Equal(default, asset.Physical.HitDecals[2]);
        Assert.Equal(1.5f, asset.OffSurfaceTime);
        Assert.True(asset.IsSwimmable);
        Assert.Equal(1, asset.Physical.DashFall);
        Assert.Equal(0, asset.Physical.NeedButtonPress);
        Assert.Equal(1, asset.Physical.DashAttach);
        Assert.Equal(0, asset.Physical.FootstepDecals);
        Assert.Equal(11, asset.Physical.DrivingSurfaceType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Read_ThenWrite_SurfaceUnderTssm_ReproducesInputBytes(byte linkCount)
    {
        byte[] data = [.. TssmData(linkCount), .. Enumerable.Range(0, linkCount).SelectMany(i => LinkBytes((short)i, 2, 0xAABBCCDD))];
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Write_NewSurfaceUnderTssm_WritesExtendedFields()
    {
        var profile = TSSMSerializer.DefaultProfile;

        byte[] written = Write(new SurfaceAsset(), profile);

        Assert.Equal(TssmData().Length, written.Length);
    }

    [Fact]
    public void Read_Surface_UnderN100F_PopulatesEveryField()
    {
        var asset = (SurfaceAsset)Read(N100FData(), N100FSerializer.DefaultProfile);

        Assert.Equal(DamageKind.Fatal1, asset.Damage);
        Assert.Equal(0.5f, asset.Friction);
        Assert.Equal(45f, asset.Uvfxs[0].RotationSpeed);
        Assert.Equal(new Vector3(1, 1, 0), asset.Uvfxs[1].Scale);
        Assert.True(asset.StartsOn);
        Assert.Empty(asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Read_ThenWrite_SurfaceUnderN100F_ReproducesInputBytes()
    {
        byte[] data = N100FData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SurfaceWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SurfaceWithoutDamageFields_ReproducesInputBytes()
    {
        // BFBB's unused b301 has one leftover SURF asset frozen at a layout that predates
        // DamageTimer/DamageBounce - see BuildProfiles.json's "bfbb/**/b3/b301.hip" entry.
        byte[] core = Core(gameDamageType: 6, phys_flags: 0x10, friction: 1.0f, on: 1);
        byte[] data = [.. Prefix(linkCount: 0), .. core[..^8]];
        var profile = BFBBSerializer.DefaultProfile with { SurfaceHasDamageFields = false };

        var asset = (SurfaceAsset)Read(data, profile);

        Assert.Equal(0f, asset.DamageTimer);
        Assert.Equal(0f, asset.DamageBounce);
        Assert.Equal(data, Write(asset, profile));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void IsSwimmable_Set_ProjectsOntoPhysicalSwimmable(bool value, byte expected)
    {
        var asset = new SurfaceAsset { IsSwimmable = value };

        Assert.Equal(expected, asset.Physical.Swimmable);
    }

    [Fact]
    public void HitDecals_SetWithWrongLength_Throws()
    {
        var asset = new SurfaceAsset();

        Assert.Throws<ArgumentException>(() => asset.Physical.HitDecals = [default, default]);
    }

    [Fact]
    public void StartsOn_SetTrue_ProjectsOntoPhysicalStartsOn()
    {
        var asset = new SurfaceAsset { StartsOn = true };

        Assert.Equal(1, asset.Physical.StartsOn);
    }

    [Fact]
    public void StartsOn_SetFalse_ProjectsOntoPhysicalStartsOn()
    {
        var asset = new SurfaceAsset { StartsOn = false };

        Assert.Equal(0, asset.Physical.StartsOn);
    }

    [Fact]
    public void DamagePassthrough_Default_IsFalse()
    {
        var asset = new SurfaceAsset();

        Assert.False(asset.DamagePassthrough);
        Assert.Equal(DamageBehavior.None, asset.Physical.GameDamageFlags);
    }

    [Fact]
    public void DamagePassthrough_SetTrue_SetsFlagOnPhysical()
    {
        var asset = new SurfaceAsset { DamagePassthrough = true };

        Assert.True(asset.DamagePassthrough);
        Assert.Equal(DamageBehavior.DamagePassthrough, asset.Physical.GameDamageFlags);
    }

    [Fact]
    public void DamagePassthrough_SetFalse_ClearsFlagOnPhysical()
    {
        var asset = new SurfaceAsset
        {
            DamagePassthrough = true,
        };
        asset.DamagePassthrough = false;

        Assert.False(asset.DamagePassthrough);
        Assert.Equal(DamageBehavior.None, asset.Physical.GameDamageFlags);
    }

    [Fact]
    public void DamagePassthrough_Toggling_PreservesOtherFlagsOnPhysical()
    {
        var asset = new SurfaceAsset();
        asset.Physical.GameDamageFlags = (DamageBehavior)0xFE;

        Assert.False(asset.DamagePassthrough);

        asset.DamagePassthrough = true;
        Assert.Equal((DamageBehavior)0xFF, asset.Physical.GameDamageFlags);
        Assert.True(asset.DamagePassthrough);

        asset.DamagePassthrough = false;
        Assert.Equal((DamageBehavior)0xFE, asset.Physical.GameDamageFlags);
        Assert.False(asset.DamagePassthrough);
    }

    [Fact]
    public void DamagePassthrough_ReflectsPhysicalGameDamageFlags()
    {
        var asset = new SurfaceAsset();
        asset.Physical.GameDamageFlags = DamageBehavior.DamagePassthrough;

        Assert.True(asset.DamagePassthrough);
    }

    [Fact]
    public void TextureAnimFlags_DerivesFromTextureAnimsIsEnabled()
    {
        var asset = new SurfaceAsset
        {
            TextureAnims = [new TextureEffect { IsEnabled = false }, new TextureEffect { IsEnabled = true }]
        };

        Assert.Equal(AnimationSlot.Slot1, asset.Physical.TextureAnimFlags);
    }

    [Fact]
    public void TextureAnimFlags_DisagreeingWithTextureAnims_IsStoredIndependently()
    {
        var asset = new SurfaceAsset();

        asset.Physical.TextureAnimFlags = (AnimationSlot)0xFFu;

        Assert.Equal((AnimationSlot)0xFFu, asset.Physical.TextureAnimFlags);
        Assert.False(asset.TextureAnims[0].IsEnabled);
    }

    [Fact]
    public void UvfxFlags_DerivesFromUvfxsIsEnabled()
    {
        var asset = new SurfaceAsset
        {
            Uvfxs = [new UVEffect { IsEnabled = true }, new UVEffect { IsEnabled = false }]
        };

        Assert.Equal(UVSlot.Slot0, asset.Physical.UvfxFlags);
    }

    [Fact]
    public void UvfxFlags_DisagreeingWithUvfxs_IsStoredIndependently()
    {
        var asset = new SurfaceAsset();

        asset.Physical.UvfxFlags = (UVSlot)0xFFu;

        Assert.Equal((UVSlot)0xFFu, asset.Physical.UvfxFlags);
        Assert.False(asset.Uvfxs[0].IsEnabled);
    }

    [Fact]
    public void TextureAnims_SetWithWrongLength_Throws()
    {
        var asset = new SurfaceAsset();

        Assert.Throws<ArgumentException>(() => asset.TextureAnims = [new TextureEffect()]);
    }

    [Fact]
    public void Uvfxs_SetWithWrongLength_Throws()
    {
        var asset = new SurfaceAsset();

        Assert.Throws<ArgumentException>(() => asset.Uvfxs = [new UVEffect(), new UVEffect(), new UVEffect()]);
    }
}

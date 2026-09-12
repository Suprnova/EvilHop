using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

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

    [Fact]
    public void Read_Surface_ProducesSurfaceAsset() =>
        Assert.IsType<SurfaceAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_Surface_UnderBfbb_PopulatesEveryField()
    {
        var asset = (SurfaceAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(SurfaceGameDamageType.Hazard, asset.GameDamageType);
        Assert.Equal(0, asset.Physical.GameSticky);
        Assert.Equal(0, asset.Physical.SurfType);
        Assert.Equal(20, asset.SlideStartAngle);
        Assert.Equal(10, asset.SlideStopAngle);
        Assert.Equal(SurfacePhysicsFlags.OutOfBounds, asset.PhysFlags);
        Assert.Equal(1.0f, asset.Friction);
        Assert.Equal(SurfaceColorFxFlags.Valid, asset.ColorFx.Flags);
        Assert.Equal(30f, asset.ColorFx.Speed);
        Assert.Equal(2, asset.TextureAnims.Length);
        Assert.Equal(2, asset.Uvfxs.Length);
        Assert.Equal(new Vector3(1, 1, 0), asset.Uvfxs[0].Scale);
        Assert.True(asset.Uvfxs[0].IsEnabled);
        Assert.False(asset.Uvfxs[1].IsEnabled);
        Assert.True(asset.IsEnabled);
        Assert.Equal(-1f, asset.OutOfBoundsDelay);
        Assert.Equal(1f, asset.WallJumpScaleXZ);
        Assert.Equal(1f, asset.WallJumpScaleY);
        Assert.Equal(0f, asset.Physical.DamageTimer);
        Assert.Equal(0f, asset.Physical.DamageBounce);
        Assert.Empty(asset.ExtendedData);
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
    public void Read_ThenWrite_SurfaceWithExtendedData_ReproducesInputBytes()
    {
        // TSSM onward appends a variable amount of unmodelled data before any links; this is
        // computed from what's left in the stream, not assumed, so any size round-trips.
        byte[] extendedData = [.. Enumerable.Range(1, 140).Select(i => (byte)i)];
        byte[] data =
        [
            .. Prefix(linkCount: 1),
            .. Core(gameDamageType: 0, phys_flags: 0, friction: 1.0f, on: 1),
            .. extendedData,
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];
        var profile = TSSMSerializer.DefaultProfile;

        var asset = (SurfaceAsset)Read(data, profile);
        Assert.Equal(extendedData, asset.ExtendedData.AsSpan().ToArray());
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_SurfaceWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_Surface_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = BfbbData();

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<SurfaceAsset>(asset);
        Assert.IsType<BaseAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void IsEnabled_SetTrue_ProjectsOntoPhysicalOnValue()
    {
        var asset = new SurfaceAsset { IsEnabled = true };

        Assert.Equal(1, asset.Physical.OnValue);
    }

    [Fact]
    public void IsEnabled_SetFalse_ProjectsOntoPhysicalOnValue()
    {
        var asset = new SurfaceAsset { IsEnabled = false };

        Assert.Equal(0, asset.Physical.OnValue);
    }

    [Fact]
    public void TextureAnimFlags_DerivesFromTextureAnimsIsEnabled()
    {
        var asset = new SurfaceAsset();
        asset.TextureAnims = [new SurfaceTextureAnim { IsEnabled = false }, new SurfaceTextureAnim { IsEnabled = true }];

        Assert.Equal(0b10u, asset.Physical.TextureAnimFlags);
    }

    [Fact]
    public void TextureAnimFlags_DisagreeingWithTextureAnims_IsStoredIndependently()
    {
        var asset = new SurfaceAsset();

        asset.Physical.TextureAnimFlags = 0xFFu;

        Assert.Equal(0xFFu, asset.Physical.TextureAnimFlags);
        Assert.False(asset.TextureAnims[0].IsEnabled);
    }

    [Fact]
    public void UvfxFlags_DerivesFromUvfxsIsEnabled()
    {
        var asset = new SurfaceAsset();
        asset.Uvfxs = [new SurfaceUvfx { IsEnabled = true }, new SurfaceUvfx { IsEnabled = false }];

        Assert.Equal(0b01u, asset.Physical.UvfxFlags);
    }

    [Fact]
    public void TextureAnims_SetWithWrongLength_Throws()
    {
        var asset = new SurfaceAsset();

        Assert.Throws<ArgumentException>(() => asset.TextureAnims = [new SurfaceTextureAnim()]);
    }

    [Fact]
    public void Uvfxs_SetWithWrongLength_Throws()
    {
        var asset = new SurfaceAsset();

        Assert.Throws<ArgumentException>(() => asset.Uvfxs = [new SurfaceUvfx(), new SurfaceUvfx(), new SurfaceUvfx()]);
    }
}

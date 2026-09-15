using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class UIAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.UI;
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
        0x20,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix(bool hasPadding) =>
    [
        0x01, 0x00, 0x00, 0x02,   // EntityFlags = Visible, Subtype, PFlags, CollisionFlags = PreciseCollision
        .. hasPadding ? new byte[4] : [],
        0x00, 0x00, 0x00, 0x00,   // SurfaceId
        .. new byte[36],          // Angle/Position/Scale
        .. new byte[16],          // ColorMultiplier
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        0x11, 0x11, 0x11, 0x11,   // ModelId
        0x22, 0x22, 0x22, 0x22,   // AnimListId
    ];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] UV(float x, float y) => [.. F32(x), .. F32(y)];

    private static byte[] UIFields(uint uiFlags, ushort width, ushort height, uint textureId) =>
    [
        (byte)(uiFlags >> 24), (byte)(uiFlags >> 16), (byte)(uiFlags >> 8), (byte)uiFlags,
        (byte)(width >> 8), (byte)width,
        (byte)(height >> 8), (byte)height,
        (byte)(textureId >> 24), (byte)(textureId >> 16), (byte)(textureId >> 8), (byte)textureId,
        .. UV(0.0f, 0.0f),
        .. UV(1.0f, 0.0f),
        .. UV(1.0f, 1.0f),
        .. UV(0.0f, 1.0f),
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
        .. EntityPrefix(hasPadding: true),
        .. UIFields(uiFlags: 0x34, width: 100, height: 50, textureId: 0xAABBCCDD),
    ];

    private static byte[] N100FData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: false),
        .. UIFields(uiFlags: 0x34, width: 100, height: 50, textureId: 0xAABBCCDD),
    ];

    [Fact]
    public void Read_UI_ProducesUIAsset() =>
        Assert.IsType<UIAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_UI_UnderBfbb_PopulatesEveryField()
    {
        var asset = (UIAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(UIFlags.RendersDirectly | UIFlags.ShowOnFocus | UIFlags.HideOnUnfocus, asset.Flags);
        Assert.Equal((ushort)100, asset.Width);
        Assert.Equal((ushort)50, asset.Height);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.TextureId);
        Assert.Equal(new Vector2(0.0f, 0.0f), asset.TopLeftUV);
        Assert.Equal(new Vector2(1.0f, 0.0f), asset.TopRightUV);
        Assert.Equal(new Vector2(1.0f, 1.0f), asset.BottomRightUV);
        Assert.Equal(new Vector2(0.0f, 1.0f), asset.BottomLeftUV);
        Assert.Equal(new AssetId(0x11111111), asset.Physical.ModelId);
        Assert.Equal(new AssetId(0x22222222), asset.Physical.AnimListId);
    }

    [Fact]
    public void Read_UI_UnderN100F_PopulatesEveryField()
    {
        var asset = (UIAsset)Read(N100FData(), N100FSerializer.DefaultProfile);

        Assert.Equal(UIFlags.RendersDirectly | UIFlags.ShowOnFocus | UIFlags.HideOnUnfocus, asset.Flags);
        Assert.Equal((ushort)100, asset.Width);
        Assert.Equal((ushort)50, asset.Height);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.TextureId);
    }

    [Fact]
    public void Read_ThenWrite_UIUnderBfbb_ReproducesInputBytes()
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
    public void Read_ThenWrite_UIUnderN100F_ReproducesInputBytes()
    {
        byte[] data = [.. N100FData(linkCount: 1), .. LinkBytes(1, 2, 0xAABBCCDD)];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_UIWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_UI_UnderTSSM_DegradesToGenericEntityAsset()
    {
        byte[] data = N100FData();
        var profile = TSSMSerializer.DefaultProfile;

        var asset = Read(data, profile);

        Assert.IsNotType<UIAsset>(asset);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void ModelId_SetThroughIHasModel_ProjectsOntoPhysicalModelId()
    {
        var asset = new UIAsset();

        ((IHasModel)asset).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.ModelId);
    }

    [Fact]
    public void SurfaceId_SetThroughIHasSurface_ProjectsOntoPhysicalSurfaceId()
    {
        var asset = new UIAsset();

        ((IHasSurface)asset).SurfaceId = new AssetId(0xCAFEF00D);

        Assert.Equal(new AssetId(0xCAFEF00D), asset.Physical.SurfaceId);
    }

    [Fact]
    public void AnimListId_SetThroughIHasAnimList_ProjectsOntoPhysicalAnimListId()
    {
        var asset = new UIAsset();

        ((IHasAnimList)asset).AnimListId = new AssetId(0xFEEDFACE);

        Assert.Equal(new AssetId(0xFEEDFACE), asset.Physical.AnimListId);
    }
}

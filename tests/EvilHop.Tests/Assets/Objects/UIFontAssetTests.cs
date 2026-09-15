using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class UIFontAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.UIFont;
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
        0x21,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix(bool hasPadding) =>
    [
        0x01, 0x00, 0x00, 0x02,   // EntityFlags = Visible, Subtype, PFlags, CollisionFlags = PreciseCollision
        .. hasPadding ? new byte[4] : [],
        0x00, 0x00, 0x00, 0x00,   // SurfaceId (always null)
        .. new byte[36],          // Angle/Position/Scale
        .. new byte[16],          // ColorMultiplier
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        0x00, 0x00, 0x00, 0x00,   // ModelId (always null)
        0x00, 0x00, 0x00, 0x00,   // AnimListId (always null)
    ];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] UV(float x, float y) => [.. F32(x), .. F32(y)];

    private static byte[] UIFields(uint uiFlags, ushort width, ushort height) =>
    [
        (byte)(uiFlags >> 24), (byte)(uiFlags >> 16), (byte)(uiFlags >> 8), (byte)uiFlags,
        (byte)(width >> 8), (byte)width,
        (byte)(height >> 8), (byte)height,
        0x00, 0x00, 0x00, 0x00, // TextureId (always null)
        .. UV(0.0f, 0.0f),
        .. UV(1.0f, 0.0f),
        .. UV(1.0f, 1.0f),
        .. UV(0.0f, 1.0f),
    ];

    private static byte[] Short(short value) => [(byte)(value >> 8), (byte)value];

    private static byte[] FontFields(ushort fontFlags, byte mode, byte fontId, uint textAssetId) =>
    [
        (byte)(fontFlags >> 8), (byte)fontFlags,
        mode,
        fontId,
        (byte)(textAssetId >> 24), (byte)(textAssetId >> 16), (byte)(textAssetId >> 8), (byte)textAssetId,
        0x80, 0x80, 0x80, 0x80, // BackdropColor
        0xFF, 0xE6, 0x00, 0xFF, // Color
        .. Short(2), .. Short(3), .. Short(4), .. Short(5), // inset top/bottom/left/right
        .. Short(21), .. Short(28),                         // space x/y
        .. Short(24), .. Short(24),                          // cdim width/height
    ];

    private static byte[] BfbbData(byte linkCount = 0, uint maxHeight = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: true),
        .. UIFields(uiFlags: 0x34, width: 600, height: 40),
        .. FontFields(fontFlags: 0x0C, mode: 1, fontId: 1, textAssetId: 0x93615FD2),
        (byte)(maxHeight >> 24), (byte)(maxHeight >> 16), (byte)(maxHeight >> 8), (byte)maxHeight,
    ];

    private static byte[] N100FData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: false),
        .. UIFields(uiFlags: 0x34, width: 500, height: 100),
        .. FontFields(fontFlags: 0x0C, mode: 1, fontId: 1, textAssetId: 0x93615FD2),
    ];

    [Fact]
    public void Read_UIFont_ProducesUIFontAsset() =>
        Assert.IsType<UIFontAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_UIFont_UnderBfbb_PopulatesEveryField()
    {
        var asset = (UIFontAsset)Read(BfbbData(maxHeight: 480), BFBBSerializer.DefaultProfile);

        Assert.Equal(UIFlags.RendersDirectly | UIFlags.ShowOnFocus | UIFlags.HideOnUnfocus, asset.Flags);
        Assert.Equal((ushort)600, asset.Width);
        Assert.Equal((ushort)40, asset.Height);
        Assert.Equal(new Vector2(0.0f, 0.0f), asset.TopLeftUV);
        Assert.Equal(new Vector2(0.0f, 1.0f), asset.BottomLeftUV);

        Assert.Equal(UIFontFlags.AlignRight | UIFontFlags.HasBackdrop, asset.FontFlags);
        Assert.Equal(UIFontMode.Mode1, asset.Mode);
        Assert.Equal((byte)1, asset.FontId);
        Assert.Equal(new AssetId(0x93615FD2), asset.TextId);
        Assert.Equal(new Rgba(0x80 / 255f, 0x80 / 255f, 0x80 / 255f, 0x80 / 255f), asset.BackdropColor);
        Assert.Equal(new Rgba(0xFF / 255f, 0xE6 / 255f, 0x00 / 255f, 0xFF / 255f), asset.Color);
        Assert.Equal((short)2, asset.InsetTop);
        Assert.Equal((short)3, asset.InsetBottom);
        Assert.Equal((short)4, asset.InsetLeft);
        Assert.Equal((short)5, asset.InsetRight);
        Assert.Equal((short)21, asset.SpaceX);
        Assert.Equal((short)28, asset.SpaceY);
        Assert.Equal((short)24, asset.CharacterWidth);
        Assert.Equal((short)24, asset.CharacterHeight);
        Assert.Equal(480u, asset.MaxHeight);

        Assert.Equal(AssetId.None, asset.Physical.SurfaceId);
        Assert.Equal(AssetId.None, asset.Physical.ModelId);
        Assert.Equal(AssetId.None, asset.Physical.AnimListId);
    }

    [Fact]
    public void Read_UIFont_UnderN100F_PopulatesEveryField()
    {
        var asset = (UIFontAsset)Read(N100FData(), N100FSerializer.DefaultProfile);

        Assert.Equal((ushort)500, asset.Width);
        Assert.Equal((ushort)100, asset.Height);
        Assert.Equal(new AssetId(0x93615FD2), asset.TextId);
        Assert.Equal(0u, asset.MaxHeight);
    }

    [Fact]
    public void Read_ThenWrite_UIFontUnderBfbb_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 1, maxHeight: 480),
            0x00, 0x45, 0x00, 0x10, 0xFB, 0x0D, 0xAA, 0xD0, // SourceEvent/DestinationEvent/DestinationAssetId
            .. new byte[16], // Params
            .. new byte[4],  // ParamWidgetAssetId
            .. new byte[4],  // CheckAssetId
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_UIFontUnderN100F_ReproducesInputBytes()
    {
        byte[] data = N100FData(linkCount: 0);
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_UIFontWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_UIFont_UnderTSSM_DegradesToGenericEntityAsset()
    {
        byte[] data = N100FData();
        var profile = TSSMSerializer.DefaultProfile;

        var asset = Read(data, profile);

        Assert.IsNotType<UIFontAsset>(asset);
        Assert.Equal(data, Write(asset, profile));
    }
}

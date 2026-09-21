using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using static EvilHop.Assets.ProgressScriptAsset;

namespace EvilHop.Tests.Serialization;

public class ProgressScriptAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new ROTUSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.ProgressScript;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= ROTUSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= ROTUSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x75,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] I32(int value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] EventBytes(float percent, int flags, uint widgetId, uint paramEvent, uint paramWidgetId) =>
    [
        .. F32(percent),
        .. I32(flags),
        .. U32(widgetId),
        .. U32(paramEvent),
        .. new byte[16], // Param
        .. U32(paramWidgetId),
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

    private static byte[] Data(byte linkCount = 0, params byte[][] events) =>
    [
        .. Prefix(linkCount),
        .. U32((uint)events.Length),
        .. events.SelectMany(e => e),
    ];

    [Fact]
    public void Read_ProgressScript_ProducesProgressScriptAsset() =>
        Assert.IsType<ProgressScriptAsset>(Read(Data()));

    [Fact]
    public void Read_ProgressScript_PopulatesEvents()
    {
        byte[] data = Data(events:
        [
            EventBytes(100.0f, 1, 0xBE00E1C8, 0x54, 0),
        ]);

        var asset = (ProgressScriptAsset)Read(data);

        Assert.Single(asset.Events);
        Assert.Equal(100.0f, asset.Events[0].Percent);
        Assert.Equal(ProgressScriptEventFlags.Once, asset.Events[0].Flags);
        Assert.Equal(new AssetId(0xBE00E1C8), asset.Events[0].WidgetId);
        Assert.Equal(0x54u, asset.Events[0].ParamEvent);
        Assert.Equal(AssetId.None, asset.Events[0].ParamWidgetId);
    }

    [Fact]
    public void Read_ThenWrite_ProgressScriptWithMultipleEvents_ReproducesInputBytes()
    {
        byte[] data = Data(events:
        [
            EventBytes(0.0f, 0, 0x11223344, 24, 0),
            EventBytes(50.0f, 1, 0x55667788, 25, 0xAABBCCDD),
        ]);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ProgressScriptWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(linkCount: 1, events: [EventBytes(0.0f, 0, 0x11223344, 24, 0)]),
            .. LinkBytes(1, 2, 0x55667788),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ProgressScriptWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ProgressScript_UnderBFBB_DegradesToGenericBaseAsset()
    {
        byte[] data = Data();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<ProgressScriptAsset>(asset);
    }

    [Fact]
    public void Read_ThenWrite_RealROTUExemplar_ReproducesInputBytes()
    {
        // rotu/prototype_2005-09-15/GC/NTSC-J/JP/A1/a103.HIP, AHDR id=0xA1667DA5
        byte[] data =
        [
            0xA1, 0x66, 0x7D, 0xA5, 0x75, 0x00, 0x00, 0x1D,
            0x00, 0x00, 0x00, 0x01,
            0x42, 0xC8, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01,
            0xBE, 0x00, 0xE1, 0xC8,
            0x00, 0x00, 0x00, 0x54,
            .. new byte[16],
            0x00, 0x00, 0x00, 0x00,
        ];

        var asset = (ProgressScriptAsset)Read(data);

        Assert.Equal(new AssetId(0xA1667DA5), asset.Physical.BaseId);
        Assert.Equal(0x75, asset.Physical.BaseType);
        Assert.Single(asset.Events);
        Assert.Equal(100.0f, asset.Events[0].Percent);
        Assert.Equal(ProgressScriptEventFlags.Once, asset.Events[0].Flags);
        Assert.Equal(new AssetId(0xBE00E1C8), asset.Events[0].WidgetId);
        Assert.Equal(0x54u, asset.Events[0].ParamEvent);

        Assert.Equal(data, Write(asset));
    }
}

using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class ScriptAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new TSSMSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Script;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= TSSMSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= TSSMSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x2A,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] EventBytes(float time, uint widgetId, uint paramEvent, uint paramWidgetId, bool? enabled = null) =>
    [
        .. F32(time),
        .. U32(widgetId),
        .. U32(paramEvent),
        .. new byte[16], // Param
        .. U32(paramWidgetId),
        .. enabled is bool e ? new byte[] { (byte)(e ? 1 : 0), 0, 0, 0 } : [],
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

    private static byte[] Data(float scaleFactor = 1.0f, bool loop = false, byte linkCount = 0, params byte[][] events) =>
    [
        .. Prefix(linkCount),
        .. F32(scaleFactor),
        .. U32((uint)events.Length),
        (byte)(loop ? 1 : 0),
        .. new byte[3], // padding
        .. events.SelectMany(e => e),
    ];

    [Fact]
    public void Read_Script_ProducesScriptAsset() =>
        Assert.IsType<ScriptAsset>(Read(Data()));

    [Fact]
    public void Read_Script_PopulatesEvents()
    {
        byte[] data = Data(events: [EventBytes(1.5f, 0xBE00E1C8, 0x54, 0x11223344)]);

        var asset = (ScriptAsset)Read(data);

        Assert.Single(asset.Events);
        Assert.Equal(1.5f, asset.Events[0].Time);
        Assert.Equal(new AssetId(0xBE00E1C8), asset.Events[0].WidgetId);
        Assert.Equal(0x54u, asset.Events[0].ParamEvent);
        Assert.Equal(new AssetId(0x11223344), asset.Events[0].ParamWidgetId);
    }

    [Fact]
    public void Read_Script_PopulatesLoopAndScaleFactor()
    {
        byte[] data = Data(scaleFactor: 2.0f, loop: true);

        var asset = (ScriptAsset)Read(data);

        Assert.Equal(2.0f, asset.ScaleFactor);
        Assert.True(asset.Loop);
    }

    [Fact]
    public void Read_ThenWrite_ScriptWithMultipleEvents_ReproducesInputBytes()
    {
        byte[] data = Data(events:
        [
            EventBytes(0.0f, 0x11223344, 24, 0),
            EventBytes(50.0f, 0x55667788, 25, 0xAABBCCDD),
        ]);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ScriptWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(linkCount: 1, events: [EventBytes(0.0f, 0x11223344, 24, 0)]),
            .. LinkBytes(1, 2, 0x55667788),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ScriptWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ScriptUnderBFBB_NoLoopField_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(0),
            .. F32(1.0f), // ScaleFactor / start time
            .. U32(1),
            .. EventBytes(0.0f, 0x11223344, 24, 0),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_ScriptUnderROTU_WithEnabledField_ReproducesInputBytes()
    {
        byte[] data = Data(events: [EventBytes(0.0f, 0x11223344, 24, 0, enabled: true)]);
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ScriptEvent_UnderTSSM_HasNoEnabledField()
    {
        byte[] data = Data(events: [EventBytes(0.0f, 0x11223344, 24, 0)]);

        var asset = (ScriptAsset)Read(data);

        Assert.True(asset.Events[0].Enabled);
    }

    [Fact]
    public void Read_ThenWrite_RealTSSMExemplar_ReproducesInputBytes()
    {
        // tssm/release/GC/NTSC-U/US-r1/AM/am01.HIP, AHDR id=0x18383281
        byte[] data =
        [
            0x18, 0x38, 0x32, 0x81, 0x2A, 0x01, 0x00, 0x1D,
            0x3F, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04,
            0x00, 0x00, 0x00, 0x00,
            .. EventBytes(BitConverter.Int32BitsToSingle(0x3C23D70A), 0x09B80F4A, 8, 0),
            .. EventBytes(BitConverter.Int32BitsToSingle(0x3C23D70A), 0xD8905CD7, 3, 0),
            .. EventBytes(BitConverter.Int32BitsToSingle(0x3C23D70A), 0xD8905CD7, 0x14E, 0xC6986D0F),
            .. EventBytes(BitConverter.Int32BitsToSingle(0x3F028F5C), 0x6F299AC3, 0x14F, 0),
            .. LinkBytes(0x0057, 0x0012, 0x18383281),
        ];

        var asset = (ScriptAsset)Read(data);

        Assert.Equal(new AssetId(0x18383281), asset.Physical.BaseId);
        Assert.Equal(0x2A, asset.Physical.BaseType);
        Assert.Equal(1.0f, asset.ScaleFactor);
        Assert.False(asset.Loop);
        Assert.Equal(4, asset.Events.Count);
        Assert.Equal(new AssetId(0x09B80F4A), asset.Events[0].WidgetId);
        Assert.Equal(8u, asset.Events[0].ParamEvent);

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_ThenWrite_RealRatatouilleExemplar_ReproducesInputBytesWithEnabledField()
    {
        // rat/prototype_2006-01-18/GC/NTSC-U/US/FP/fp01.HIP, AHDR id=0x0086C66F
        byte[] data =
        [
            0x00, 0x86, 0xC6, 0x6F, 0x2A, 0x00, 0x00, 0x1D,
            0x3F, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03,
            0x00, 0x00, 0x00, 0x00,
            .. EventBytes(3.0f, 0xEEB21E18, 2, 0, enabled: true),
            .. EventBytes(3.0f, 0xA0C0DF7D, 3, 0x42B05E7D, enabled: true),
            .. EventBytes(5.0f, 0xA0C0DF7D, 4, 0, enabled: true),
        ];
        var profile = RatatouilleSerializer.DefaultProfile;

        var asset = (ScriptAsset)Read(data, profile);

        Assert.Equal(3, asset.Events.Count);
        Assert.All(asset.Events, e => Assert.True(e.Enabled));
        Assert.Equal(new AssetId(0xEEB21E18), asset.Events[0].WidgetId);

        Assert.Equal(data, Write(asset, profile));
    }
}

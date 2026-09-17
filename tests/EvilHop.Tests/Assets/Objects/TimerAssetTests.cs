using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.IO;

namespace EvilHop.Tests.Serialization;

public class TimerAssetTests
{
    private readonly TimerAsset _asset;

    public TimerAssetTests()
    {
        _asset = new TimerAsset();
    }

    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(uint id = 0x12345678)
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Timer;
        header.Id = id;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null, uint id = 0x12345678)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var (header, debug) = HeaderFor(id);
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(
        byte linkCount = 0,
        byte baseType = 0x0E,
        ushort baseFlags = 0x001D,
        uint baseId = 0x12345678,
        bool bigEndian = true)
    {
        var idBytes = BitConverter.GetBytes(baseId);
        var flagBytes = BitConverter.GetBytes(baseFlags);
        if (bigEndian)
        {
            Array.Reverse(idBytes);
            Array.Reverse(flagBytes);
        }

        return
        [
            idBytes[0], idBytes[1], idBytes[2], idBytes[3],
            baseType,
            linkCount,
            flagBytes[0], flagBytes[1],
        ];
    }

    private static byte[] F32(float value, bool bigEndian = true)
    {
        var bytes = BitConverter.GetBytes(value);
        if (bigEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16),
        (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        0, 0, 0, 0, // param0
        0, 0, 0, 0, // param1
        0, 0, 0, 0, // param2
        0, 0, 0, 0, // param3
        0, 0, 0, 0, // param4
        0, 0, 0, 0, // param5
    ];

    [Fact]
    public void NewTimerAsset_HasExpectedDefaults()
    {
        Assert.Equal(AssetType.Timer, _asset.Type);
        Assert.Equal(0x0E, _asset.Physical.BaseType);
        Assert.Equal(0.0f, _asset.Seconds);
        Assert.Equal(0.0f, _asset.RandomRange);
        Assert.Empty(_asset.Links);
    }

    [Fact]
    public void Read_TimerWithNoLinks_ParsesCorrectly()
    {
        byte[] data =
        [
            .. Prefix(linkCount: 0),
            .. F32(5.0f),
            .. F32(1.5f),
        ];

        var asset = (TimerAsset)Read(data);

        Assert.Equal(0x0E, asset.Physical.BaseType);
        Assert.Equal(5.0f, asset.Seconds);
        Assert.Equal(1.5f, asset.RandomRange);
        Assert.Empty(asset.Links);

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_RealBFBBExemplar_RoundTripsExactly()
    {
        // From b101.HIP: AHDR id=0x35D26F6A "TEST_TIMER_DELETE", size=112
        byte[] data =
        [
            0x35, 0xD2, 0x6F, 0x6A, 0x0E, 0x03, 0x00, 0x1D,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x14, 0x00, 0x0A, 0x35, 0xD2, 0x6F, 0x6A,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x14, 0x00, 0x12, 0x8A, 0xAF, 0x03, 0x6E,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x14, 0x00, 0x0C, 0x1E, 0xF6, 0xC9, 0x87,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        ];

        var asset = (TimerAsset)Read(data);

        Assert.Equal(new AssetId(0x35D26F6A), asset.Physical.BaseId);
        Assert.Equal(0x0E, asset.Physical.BaseType);
        Assert.Equal(3.0f, asset.Seconds);
        Assert.Equal(0.0f, asset.RandomRange);
        Assert.Equal(3, asset.Links.Count);

        Assert.Equal(20, asset.Links[0].SourceEvent);
        Assert.Equal(10, asset.Links[0].DestinationEvent);
        Assert.Equal(new AssetId(0x35D26F6A), asset.Links[0].DestinationAssetId);

        Assert.Equal(20, asset.Links[1].SourceEvent);
        Assert.Equal(18, asset.Links[1].DestinationEvent);
        Assert.Equal(new AssetId(0x8AAF036E), asset.Links[1].DestinationAssetId);

        Assert.Equal(20, asset.Links[2].SourceEvent);
        Assert.Equal(12, asset.Links[2].DestinationEvent);
        Assert.Equal(new AssetId(0x1EF6C987), asset.Links[2].DestinationAssetId);

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_TimerWithLinks_ParsesCorrectly()
    {
        byte[] data =
        [
            .. Prefix(linkCount: 1),
            .. F32(10.0f),
            .. F32(2.5f),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];

        var asset = (TimerAsset)Read(data);

        Assert.Equal(10.0f, asset.Seconds);
        Assert.Equal(2.5f, asset.RandomRange);
        Assert.Single(asset.Links);
        Assert.Equal(1, asset.Links[0].SourceEvent);
        Assert.Equal(2, asset.Links[0].DestinationEvent);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Links[0].DestinationAssetId);

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_WithUnparsedTail_PreservesTail()
    {
        byte[] tail = [0xDE, 0xAD, 0xBE, 0xEF];
        byte[] data =
        [
            .. Prefix(linkCount: 0),
            .. F32(1.0f),
            .. F32(0.5f),
            .. tail,
        ];

        var asset = (TimerAsset)Read(data);

        Assert.Equal(1.0f, asset.Seconds);
        Assert.Equal(0.5f, asset.RandomRange);
        Assert.Equal(tail, asset.GetUnparsedTail().ToArray());

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Write_GuardedNonTimerAsset_DoesNotThrow()
    {
        var generic = new GenericBaseAsset(AssetType.Timer);
        generic.Physical.BaseId = new AssetId(0x12345678);
        generic.Physical.BaseType = 0x0E;
        generic.Physical.LinkCount = 0;
        generic.BaseFlags = BaseAssetFlags.Valid;
        generic.SetUnparsedTail([0x11, 0x22, 0x33, 0x44]);

        byte[] written = Write(generic);
        Assert.Equal(
            new byte[]
            {
                0x12, 0x34, 0x56, 0x78,
                0x0E, 0x00, 0x00, 0x04,
                0x11, 0x22, 0x33, 0x44,
            },
            written);
    }

    [Theory]
    [InlineData(GameVersion.BFBB)]
    [InlineData(GameVersion.N100F)]
    [InlineData(GameVersion.TSSM)]
    [InlineData(GameVersion.Incredibles)]
    [InlineData(GameVersion.ROTU)]
    [InlineData(GameVersion.Ratatouille)]
    public void Read_UnderAllSupportedGames_ParsesAsTimerAsset(GameVersion game)
    {
        var profile = Serializer.DefaultProfileFor(game);
        bool bigEndian = profile.Endianness == Endianness.Big;
        byte[] data =
        [
            .. Prefix(linkCount: 0, bigEndian: bigEndian),
            .. F32(8.0f, bigEndian),
            .. F32(0.0f, bigEndian),
        ];

        var asset = Read(data, profile);

        Assert.IsType<TimerAsset>(asset);
        var timerAsset = (TimerAsset)asset;
        Assert.Equal(8.0f, timerAsset.Seconds);
        Assert.Equal(0.0f, timerAsset.RandomRange);
        Assert.Equal(data, Write(timerAsset, profile));
    }

    [Fact]
    public void Read_LittleEndian_ParsesAndWritesCorrectly()
    {
        var profile = BFBBSerializer.DefaultProfile with { Platform = Platform.Xbox };
        byte[] data =
        [
            .. Prefix(linkCount: 0, bigEndian: false),
            .. F32(15.0f, bigEndian: false),
            .. F32(3.0f, bigEndian: false),
        ];

        var asset = (TimerAsset)Read(data, profile);

        Assert.Equal(15.0f, asset.Seconds);
        Assert.Equal(3.0f, asset.RandomRange);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_TruncatedData_ThrowsEndOfStreamException()
    {
        byte[] truncated =
        [
            .. Prefix(linkCount: 0),
            .. F32(5.0f),
            // missing RandomRange (4 bytes)
        ];

        Assert.Throws<EndOfStreamException>(() => Read(truncated));
    }
}

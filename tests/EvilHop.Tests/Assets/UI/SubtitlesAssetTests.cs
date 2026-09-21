using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Text;

namespace EvilHop.Tests.Serialization;

public class SubtitlesAssetTests
{
    private readonly SubtitlesAsset _asset;

    public SubtitlesAssetTests()
    {
        _asset = new SubtitlesAsset();
    }

    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Subtitles;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x00,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
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

    private static byte[] U16(ushort value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] LineBytes(float startTime, float stopTime, uint stringOffset) =>
    [
        .. F32(startTime),
        .. F32(stopTime),
        .. U32(stringOffset),
    ];

    /// <summary>Rounds a string pool's naive byte count up to the 4-byte boundary the format pads it to.</summary>
    private static ushort Align4(int naiveByteCount) => (ushort)(naiveByteCount + (4 - naiveByteCount % 4) % 4);

    [Fact]
    public void Type_IsSubtitles()
    {
        Assert.Equal(AssetType.Subtitles, _asset.Type);
    }

    [Fact]
    public void Physical_BaseType_DefaultsTo0x00()
    {
        Assert.Equal(0x00, _asset.Physical.BaseType);
    }

    [Fact]
    public void Lines_DefaultsToEmpty()
    {
        Assert.Empty(_asset.Lines);
    }

    [Fact]
    public void Physical_NumLines_DefaultsToLinesCount()
    {
        Assert.Equal((ushort)0, _asset.Physical.NumLines);

        _asset.Lines.Add(new SubtitleLine());
        Assert.Equal((ushort)1, _asset.Physical.NumLines);

        _asset.Lines.Add(new SubtitleLine());
        Assert.Equal((ushort)2, _asset.Physical.NumLines);
    }

    [Fact]
    public void Physical_ByteCount_DefaultsToCalculatedByteCount()
    {
        Assert.Equal((ushort)0, _asset.Physical.ByteCount);

        _asset.Lines.Add(new SubtitleLine
        {
            StartTime = 1.0f,
            StopTime = 2.5f,
            Text = "Hello",
        });

        // 12 bytes descriptor + 5 bytes "Hello" + 1 byte null = 18 bytes, padded to 20
        Assert.Equal((ushort)20, _asset.Physical.ByteCount);

        _asset.Lines.Add(new SubtitleLine
        {
            StartTime = 3.0f,
            StopTime = 5.0f,
            Text = "World!",
        });

        // 24 bytes descriptors + 6 ("Hello\0") + 7 ("World!\0") = 37 bytes, padded to 40
        Assert.Equal((ushort)40, _asset.Physical.ByteCount);
    }

    [Fact]
    public void Physical_NumLines_WhenOverridden_DoesNotTrackLinesCountUntilCleared()
    {
        _asset.Physical.NumLines = 10;
        Assert.Equal((ushort)10, _asset.Physical.NumLines);

        _asset.Lines.Add(new SubtitleLine());
        Assert.Equal((ushort)10, _asset.Physical.NumLines);

        _asset.Physical.NumLines = 1;
        Assert.Equal((ushort)1, _asset.Physical.NumLines);

        _asset.Lines.Add(new SubtitleLine());
        Assert.Equal((ushort)2, _asset.Physical.NumLines);
    }

    [Fact]
    public void Physical_ByteCount_WhenOverridden_DoesNotTrackCalculatedByteCountUntilCleared()
    {
        _asset.Physical.ByteCount = 200;
        Assert.Equal((ushort)200, _asset.Physical.ByteCount);

        _asset.Lines.Add(new SubtitleLine { Text = "Test" });
        Assert.Equal((ushort)200, _asset.Physical.ByteCount);

        // 12 + 4 + 1 = 17 bytes, padded to 20
        _asset.Physical.ByteCount = 20;
        Assert.Equal((ushort)20, _asset.Physical.ByteCount);

        _asset.Lines.Add(new SubtitleLine { Text = "A" });
        // 24 + 5 + 2 = 31 bytes, padded to 32
        Assert.Equal((ushort)32, _asset.Physical.ByteCount);
    }

    [Fact]
    public void SubtitleLine_PropertiesSetAndGet()
    {
        var line = new SubtitleLine
        {
            StartTime = 2.5f,
            StopTime = 7.8f,
            Text = "Watch out for that robot!",
        };

        Assert.Equal(2.5f, line.StartTime);
        Assert.Equal(7.8f, line.StopTime);
        Assert.Equal("Watch out for that robot!", line.Text);
    }

    [Fact]
    public void Read_EmptyLines_RoundTrips()
    {
        byte[] data =
        [
            .. Prefix(0),
            .. U16(0), // NumLines
            .. U16(0), // ByteCount
        ];

        var asset = (SubtitlesAsset)Read(data);

        Assert.Empty(asset.Lines);
        Assert.Equal((ushort)0, asset.Physical.NumLines);
        Assert.Equal((ushort)0, asset.Physical.ByteCount);
        Assert.Empty(asset.Links);
        Assert.Empty(asset.GetUnparsedTail().ToArray());

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_PopulatesLinesAndRoundTrips()
    {
        string text0 = "Hold on!";
        string text1 = "Incredibles, go!";
        byte[] pool0 = Encoding.Latin1.GetBytes(text0 + "\0");
        byte[] pool1 = Encoding.Latin1.GetBytes(text1 + "\0");

        ushort numLines = 2;
        int naiveByteCount = numLines * 12 + pool0.Length + pool1.Length;
        ushort byteCount = Align4(naiveByteCount);
        byte[] padding = new byte[byteCount - naiveByteCount];

        byte[] data =
        [
            .. Prefix(0),
            .. U16(numLines),
            .. U16(byteCount),
            .. LineBytes(0.5f, 2.0f, 0),
            .. LineBytes(2.5f, 5.0f, (uint)pool0.Length),
            .. pool0,
            .. pool1,
            .. padding,
        ];

        var asset = (SubtitlesAsset)Read(data);

        Assert.Equal(2, asset.Lines.Count);

        Assert.Equal(0.5f, asset.Lines[0].StartTime);
        Assert.Equal(2.0f, asset.Lines[0].StopTime);
        Assert.Equal(text0, asset.Lines[0].Text);

        Assert.Equal(2.5f, asset.Lines[1].StartTime);
        Assert.Equal(5.0f, asset.Lines[1].StopTime);
        Assert.Equal(text1, asset.Lines[1].Text);

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_NumLinesAndByteCountKeepDerivingAfterCollectionsAreMutated()
    {
        string text0 = "Line 1";
        byte[] pool0 = Encoding.Latin1.GetBytes(text0 + "\0");
        ushort numLines = 1;
        int naiveByteCount = numLines * 12 + pool0.Length;
        ushort byteCount = Align4(naiveByteCount);
        byte[] padding = new byte[byteCount - naiveByteCount];

        byte[] data =
        [
            .. Prefix(0),
            .. U16(numLines),
            .. U16(byteCount),
            .. LineBytes(1.0f, 3.0f, 0),
            .. pool0,
            .. padding,
        ];

        var asset = (SubtitlesAsset)Read(data);
        Assert.Equal((ushort)1, asset.Physical.NumLines);
        Assert.Equal(byteCount, asset.Physical.ByteCount);

        asset.Lines.Add(new SubtitleLine { StartTime = 3.5f, StopTime = 6.0f, Text = "Line 2" });
        Assert.Equal((ushort)2, asset.Physical.NumLines);
        Assert.Equal(Align4(24 + pool0.Length + 7), asset.Physical.ByteCount);
    }

    [Fact]
    public void Read_PreservesUnparsedTail()
    {
        byte[] unparsedTail = [0xCA, 0xFE, 0xBA, 0xBE];
        byte[] data =
        [
            .. Prefix(0),
            .. U16(0),
            .. U16(0),
            .. unparsedTail,
        ];

        var asset = (SubtitlesAsset)Read(data);
        Assert.Equal(unparsedTail, asset.GetUnparsedTail().ToArray());

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_ReadsLinksAtTheDocumentedOffset()
    {
        string text = "Incoming!";
        byte[] pool = Encoding.Latin1.GetBytes(text + "\0");
        ushort numLines = 1;
        int naiveByteCount = numLines * 12 + pool.Length;
        ushort byteCount = Align4(naiveByteCount);
        byte[] padding = new byte[byteCount - naiveByteCount];

        byte[] data =
        [
            .. Prefix(linkCount: 1),
            .. U16(numLines),
            .. U16(byteCount),
            .. LineBytes(0.0f, 1.0f, 0),
            .. pool,
            .. padding,
            .. LinkBytes(15, 25, 0x12345678),
        ];

        var asset = (SubtitlesAsset)Read(data);

        Assert.Single(asset.Links);
        Assert.Equal(15, asset.Links[0].SourceEvent);
        Assert.Equal(25, asset.Links[0].DestinationEvent);
        Assert.Equal(new AssetId(0x12345678), asset.Links[0].DestinationAssetId);

        Assert.Equal(data, Write(asset));
    }

    [Theory]
    [InlineData(GameVersion.BFBB)]
    [InlineData(GameVersion.N100F)]
    [InlineData(GameVersion.TSSM)]
    [InlineData(GameVersion.Ratatouille)]
    public void Read_UnderUnsupportedGames_DegradesToGenericBaseAsset(GameVersion game)
    {
        var profile = Serializer.DefaultProfileFor(game);
        byte[] data =
        [
            .. Prefix(0),
            .. U16(0),
            .. U16(0),
        ];

        var asset = Read(data, profile);

        Assert.IsNotType<SubtitlesAsset>(asset, exactMatch: false);
        Assert.IsType<GenericBaseAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void Read_UnderROTU_ParsesCorrectly()
    {
        var profile = ROTUSerializer.DefaultProfile;
        string text = "Destroy the robot!";
        byte[] pool = Encoding.Latin1.GetBytes(text + "\0");
        ushort numLines = 1;
        ushort byteCount = (ushort)(numLines * 12 + pool.Length);

        byte[] data =
        [
            .. Prefix(0),
            .. U16(numLines),
            .. U16(byteCount),
            .. LineBytes(0.0f, 3.0f, 0),
            .. pool,
        ];

        var asset = (SubtitlesAsset)Read(data, profile);

        Assert.Single(asset.Lines);
        Assert.Equal(text, asset.Lines[0].Text);

        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Write_GuardedNonSubtitlesAsset_DoesNotThrow()
    {
        var generic = new GenericBaseAsset(AssetType.Subtitles);
        generic.Physical.BaseId = new AssetId(0x12345678);
        generic.Physical.BaseType = 0x00;
        generic.Physical.LinkCount = 0;
        generic.BaseFlags = BaseAssetFlags.Valid;
        generic.SetUnparsedTail([0x11, 0x22, 0x33, 0x44]);

        byte[] written = Write(generic);
        Assert.Equal(
            new byte[]
            {
                0x12, 0x34, 0x56, 0x78,
                0x00, 0x00, 0x00, 0x04,
                0x11, 0x22, 0x33, 0x44,
            },
            written);
    }
}

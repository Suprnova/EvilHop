using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.IO;
using System.Text;

namespace EvilHop.Tests.Serialization;

public class TextAssetTests
{
    private readonly TextAsset _asset;

    public TextAssetTests()
    {
        _asset = new TextAsset();
    }

    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(uint id = 0x12345678)
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Text;
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

    private static byte[] MakeTextBytes(string text, bool bigEndian = true, uint? overriddenLength = null, byte[]? tail = null, bool lengthIncludesNull = false)
    {
        byte[] textBytes = Encoding.Latin1.GetBytes(text);
        uint len = overriddenLength ?? (uint)textBytes.Length + (lengthIncludesNull ? 1u : 0u);
        byte[] lenBytes = bigEndian
            ? [(byte)(len >> 24), (byte)(len >> 16), (byte)(len >> 8), (byte)len]
            : [(byte)len, (byte)(len >> 8), (byte)(len >> 16), (byte)(len >> 24)];

        int pad = (4 - ((textBytes.Length + 1) & 3)) & 3;
        byte[] padBytes = new byte[pad];

        using var ms = new MemoryStream();
        ms.Write(lenBytes);
        ms.Write(textBytes);
        ms.WriteByte(0); // Null terminator
        ms.Write(padBytes);
        if (tail is not null)
            ms.Write(tail);
        return ms.ToArray();
    }

    [Fact]
    public void NewTextAsset_HasExpectedDefaults()
    {
        Assert.Equal(AssetType.Text, _asset.Type);
        Assert.Equal(string.Empty, _asset.Text);
        Assert.Equal(0u, _asset.Physical.Length);
        Assert.Equal(0, _asset.Physical.Alignment);
        Assert.Equal(AssetFlags.None, _asset.Physical.Flags);
    }

    [Fact]
    public void Physical_Length_DerivesFromText()
    {
        _asset.Text = "Hello";
        Assert.Equal(5u, _asset.Physical.Length);

        _asset.Text = string.Empty;
        Assert.Equal(0u, _asset.Physical.Length);

        _asset.Text = "Testing 1, 2, 3!";
        Assert.Equal(16u, _asset.Physical.Length);
    }

    [Fact]
    public void Physical_Length_OverridesAndClearsCorrectly()
    {
        _asset.Text = "Test";
        Assert.Equal(4u, _asset.Physical.Length);

        // Override
        _asset.Physical.Length = 100;
        Assert.Equal(100u, _asset.Physical.Length);

        // Mutating text does not clear override when distinct
        _asset.Text = "Testing";
        Assert.Equal(100u, _asset.Physical.Length);

        // Assigning matching value clears override
        _asset.Physical.Length = 7;
        Assert.Equal(7u, _asset.Physical.Length);

        // Mutating text now updates length again
        _asset.Text = "New";
        Assert.Equal(3u, _asset.Physical.Length);
    }

    [Fact]
    public void Read_EmptyString_ParsesCorrectly()
    {
        byte[] data = MakeTextBytes(string.Empty);
        Assert.Equal(8, data.Length); // 4 len + 1 null + 3 pad

        var asset = (TextAsset)Read(data);

        Assert.Equal(string.Empty, asset.Text);
        Assert.Equal(0u, asset.Physical.Length);
        Assert.Equal(data, Write(asset));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("ABC")]
    [InlineData("ABCD")]
    [InlineData("ABCDE")]
    [InlineData("ABCDEF")]
    [InlineData("ABCDEFG")]
    [InlineData("0123456789")]
    [InlineData("This is a longer message used in dialog and HUD text!")]
    public void Read_ThenWrite_VariousLengths_RoundTripsExactly(string text)
    {
        byte[] data = MakeTextBytes(text);
        var asset = (TextAsset)Read(data);

        Assert.Equal(text, asset.Text);
        Assert.Equal((uint)Encoding.Latin1.GetByteCount(text), asset.Physical.Length);
        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_RealBFBBExemplar_RoundTripsExactly()
    {
        // From b101.HIP: AHDR id=0x00F51B8A "round_3_text", size=16
        byte[] data =
        [
            0x00, 0x00, 0x00, 0x08, // len = 8
            0x52, 0x6F, 0x75, 0x6E, 0x64, 0x20, 0x33, 0x21, // "Round 3!"
            0x00,                   // null terminator
            0x00, 0x00, 0x00,       // 3 bytes padding
        ];

        var asset = (TextAsset)Read(data);

        Assert.Equal("Round 3!", asset.Text);
        Assert.Equal(8u, asset.Physical.Length);
        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_RealN100FExemplar_RoundTripsExactly()
    {
        // From boot.HIP: AHDR id=0xE9DB1B39 "TALKTEXT", size=24. N100F's length field includes
        // the null terminator, leaving no room for separate padding.
        byte[] data =
        [
            0x00, 0x00, 0x00, 0x14,
            0x50, 0x52, 0x45, 0x53, 0x53, 0x20, 0x24, 0x44, 0x7E, 0x24, 0x43, 0x20,
            0x54, 0x4F, 0x20, 0x54, 0x41, 0x4C, 0x4B, 0x00,
        ];

        var profile = Serializer.DefaultProfileFor(GameVersion.N100F);
        var asset = (TextAsset)Read(data, profile, id: 0xE9DB1B39);

        Assert.Equal("PRESS $D~$C TO TALK", asset.Text);
        Assert.Equal(data, Write(asset, profile));
    }

    [Theory]
    [InlineData("AB")] // strlen % 4 == 2, needs 1 padding byte
    [InlineData("ABC")] // strlen % 4 == 3, no padding needed
    [InlineData("ABCD")] // strlen % 4 == 0, needs 3 padding bytes
    [InlineData("ABCDE")] // strlen % 4 == 1, needs 2 padding bytes
    public void Read_ThenWrite_N100FLengthIncludesNullTerminator_RoundTripsExactly(string text)
    {
        var profile = Serializer.DefaultProfileFor(GameVersion.N100F);
        byte[] data = MakeTextBytes(text, lengthIncludesNull: true);

        var asset = (TextAsset)Read(data, profile);

        Assert.Equal(text, asset.Text);
        Assert.Equal((uint)text.Length, asset.Physical.Length);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_FormattedTextWithTags_RoundTripsExactly()
    {
        string text = "{color=00FF00}Press {tex:button_a} to jump!{n}{wait:2.0}";
        byte[] data = MakeTextBytes(text);

        var asset = (TextAsset)Read(data);

        Assert.Equal(text, asset.Text);
        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_SpecialCharacters_PreservesLatin1RoundTrip()
    {
        string text = "\u00A9 2004 THQ / Heavy Iron Studios \u00E9\u00E0\u00FC\u00F1";
        byte[] data = MakeTextBytes(text);

        var asset = (TextAsset)Read(data);

        Assert.Equal(text, asset.Text);
        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_WithUnparsedTail_PreservesTail()
    {
        byte[] tail = [0xDE, 0xAD, 0xBE, 0xEF];
        byte[] data = MakeTextBytes("Hello", tail: tail);

        var asset = (TextAsset)Read(data);

        Assert.Equal("Hello", asset.Text);
        Assert.Equal(tail, asset.GetUnparsedTail().ToArray());
        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Write_OverriddenLength_PreservesOverriddenValue()
    {
        var asset = new TextAsset
        {
            Text = "Hello",
        };
        asset.Physical.Length = 42;

        byte[] written = Write(asset);

        Assert.Equal(0x00, written[0]);
        Assert.Equal(0x00, written[1]);
        Assert.Equal(0x00, written[2]);
        Assert.Equal(0x2A, written[3]); // 42 in big-endian
    }

    [Fact]
    public void Write_GuardedNonTextAsset_DoesNotThrow()
    {
        var generic = new GenericAsset(AssetType.Text);
        generic.SetUnparsedTail([0x01, 0x02, 0x03, 0x04]);

        byte[] written = Write(generic);
        Assert.Equal([0x01, 0x02, 0x03, 0x04], written);
    }

    [Theory]
    [InlineData(GameVersion.BFBB)]
    [InlineData(GameVersion.N100F)]
    [InlineData(GameVersion.TSSM)]
    [InlineData(GameVersion.Incredibles)]
    [InlineData(GameVersion.ROTU)]
    [InlineData(GameVersion.Ratatouille)]
    public void Read_UnderAllSupportedGames_ParsesAsTextAsset(GameVersion game)
    {
        var profile = Serializer.DefaultProfileFor(game);
        bool bigEndian = profile.Endianness == Endianness.Big;
        byte[] data = MakeTextBytes("Game Test", bigEndian: bigEndian, lengthIncludesNull: game is GameVersion.N100F);

        var asset = Read(data, profile);

        Assert.IsType<TextAsset>(asset);
        var textAsset = (TextAsset)asset;
        Assert.Equal("Game Test", textAsset.Text);
        Assert.Equal(data, Write(textAsset, profile));
    }

    [Fact]
    public void Read_LittleEndian_ParsesAndWritesCorrectly()
    {
        var profile = BFBBSerializer.DefaultProfile with { Platform = Platform.Xbox };
        byte[] data = MakeTextBytes("Xbox Text", bigEndian: false);

        var asset = (TextAsset)Read(data, profile);

        Assert.Equal("Xbox Text", asset.Text);
        Assert.Equal(9u, asset.Physical.Length);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_TruncatedLength_ThrowsEndOfStreamException()
    {
        byte[] truncated = [0x00, 0x01];
        Assert.Throws<EndOfStreamException>(() => Read(truncated));
    }

    [Fact]
    public void Read_TruncatedData_ThrowsEndOfStreamException()
    {
        byte[] truncated = [0x00, 0x00, 0x00, 0x0A, 0x41, 0x42, 0x43]; // Length 10, but only 3 bytes of chars
        Assert.Throws<EndOfStreamException>(() => Read(truncated));
    }
}

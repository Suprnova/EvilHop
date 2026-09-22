using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Text;
using static EvilHop.Assets.DiscoFloorAsset;

namespace EvilHop.Tests.Serialization;

public class DiscoFloorAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.DiscoFloor;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
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

    private static byte[] Prefix(byte linkCount = 0) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x00,                   // BaseType
        linkCount,
        0x00, 0x00,             // BaseFlags
    ];

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] PaddedPrefixString(string text)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(text);
        var padded = new byte[(bytes.Length + 1 + 3) & ~3];
        bytes.CopyTo(padded, 0);
        return padded;
    }

    private static byte EncodeTiles(TileState t0, TileState t1, TileState t2) =>
        (byte)((int)t0 | ((int)t1 << 2) | ((int)t2 << 4));

    // Two states, three tiles each - encoded to a single mask byte per state (3 tiles => ceil(6/8) = 1
    // byte), so the bitmask region (2 bytes) needs 2 bytes of trailing padding to reach a 4-byte
    // alignment.
    private static byte[] SampleData(byte linkCount = 0, byte[]? linkBytes = null, byte[]? tail = null)
    {
        byte[] offPrefix = PaddedPrefixString("OFF0");
        byte[] transitionPrefix = PaddedPrefixString("YEL0");
        byte[] onPrefix = PaddedPrefixString("RED0");

        uint offPrefixOffset = 36 + 32u * linkCount;
        uint transitionPrefixOffset = offPrefixOffset + (uint)offPrefix.Length;
        uint onPrefixOffset = transitionPrefixOffset + (uint)transitionPrefix.Length;
        uint statesOffset = onPrefixOffset + (uint)onPrefix.Length;
        uint bitmaskRegionStart = statesOffset + 2 * 4;

        byte mask0 = EncodeTiles(TileState.Off, TileState.On, TileState.Random);
        byte mask1 = EncodeTiles(TileState.On, TileState.Off, TileState.On);

        return
        [
            .. Prefix(linkCount),
            .. U32(0x00000003), // flags: Loop | Enabled
            .. F32(0.25f), .. F32(1.0f),
            .. U32(offPrefixOffset), .. U32(transitionPrefixOffset), .. U32(onPrefixOffset),
            .. U32(3), // state_mask_size (tile count)
            .. U32(statesOffset),
            .. U32(2), // states_size (state count)
            .. (linkBytes ?? []),
            .. offPrefix, .. transitionPrefix, .. onPrefix,
            .. U32(bitmaskRegionStart), .. U32(bitmaskRegionStart + 1),
            mask0, mask1, 0x00, 0x00,
            .. (tail ?? []),
        ];
    }

    [Fact]
    public void Read_DiscoFloor_ProducesDiscoFloorAsset() =>
        Assert.IsType<DiscoFloorAsset>(Read(SampleData()));

    [Fact]
    public void Read_DiscoFloor_PopulatesFields()
    {
        var asset = (DiscoFloorAsset)Read(SampleData());

        Assert.Equal(Behavior.Loop | Behavior.Enabled, asset.Flags);
        Assert.Equal(0.25f, asset.TransitionDuration);
        Assert.Equal(1.0f, asset.StateDuration);
        Assert.Equal("OFF0", asset.OffPrefix);
        Assert.Equal("YEL0", asset.TransitionPrefix);
        Assert.Equal("RED0", asset.OnPrefix);
        Assert.Equal(3u, asset.Physical.TileCount);
    }

    [Fact]
    public void Read_DiscoFloor_PopulatesStates()
    {
        var asset = (DiscoFloorAsset)Read(SampleData());

        Assert.Equal(2, asset.States.Count);

        Assert.Equal([TileState.Off, TileState.On, TileState.Random], asset.States[0].Tiles);
        Assert.Equal([TileState.On, TileState.Off, TileState.On], asset.States[1].Tiles);
    }

    [Fact]
    public void Read_DiscoFloor_ReadsLinksRightAfterTheFixedStruct()
    {
        byte[] linkBytes = LinkBytes(1, 2, 0xAABBCCDD);
        byte[] data = SampleData(linkCount: 1, linkBytes: linkBytes);

        var asset = (DiscoFloorAsset)Read(data);

        var link = Assert.Single(asset.Links);
        Assert.Equal(1, link.SourceEvent);
        Assert.Equal(2, link.DestinationEvent);
        Assert.Equal(new AssetId(0xAABBCCDD), link.DestinationAssetId);

        // The prefixes/states still parse correctly with the link array in front of them.
        Assert.Equal("OFF0", asset.OffPrefix);
        Assert.Equal(2, asset.States.Count);
    }

    [Fact]
    public void Read_ThenWrite_DiscoFloorWithNoLinks_ReproducesInputBytes()
    {
        byte[] data = SampleData();

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_DiscoFloorWithLinks_ReproducesInputBytes()
    {
        byte[] data = SampleData(linkCount: 1, linkBytes: LinkBytes(1, 2, 0xAABBCCDD));

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_DiscoFloorWithNonZeroBitmaskPadding_ReproducesInputBytes()
    {
        // Regression test: real archives leave the unused bit pairs in a mask's last byte (here,
        // tile index 3 - state_mask_size 3 only uses tiles 0-2, out of the 4 the byte has room for)
        // set to whatever the authoring tool's buffer already held, not zero. A codec that always
        // zeroes them on write can't reproduce that.
        byte[] data =
        [
            .. Prefix(),
            .. U32(0x00000002), // flags: Enabled
            .. F32(0.1f), .. F32(0.1f),
            .. U32(36), .. U32(40), .. U32(44), // prefix offsets - all three prefixes are empty
            .. U32(3), // state_mask_size
            .. U32(48), // states_offset
            .. U32(1), // states_size
            0x00, 0x00, 0x00, 0x00, // off prefix: "" + null + 3 bytes padding
            0x00, 0x00, 0x00, 0x00, // transition prefix
            0x00, 0x00, 0x00, 0x00, // on prefix
            .. U32(52), // state_offsets[0]
            0xE1, // On, Off, Random, then garbage (0b11) in the unused 4th tile slot
            0x00, 0x00, 0x00, // padding to align the bitmask region to 4 bytes
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_DiscoFloorWithNonZeroTrailingPadding_ReproducesInputBytes()
    {
        // Regression test: the state bitmask array, as a whole, is padded to a 4-byte alignment at
        // its very end (here, a single 1-byte mask needs 3 bytes of padding) - and real archives
        // leave those bytes as whatever the authoring tool's buffer already held, not zero.
        byte[] data =
        [
            .. Prefix(),
            .. U32(0x00000002), // flags: Enabled
            .. F32(0.1f), .. F32(0.1f),
            .. U32(36), .. U32(40), .. U32(44),
            .. U32(4), // state_mask_size - fills its 1-byte mask exactly, no unused tile slots
            .. U32(48), // states_offset
            .. U32(1), // states_size
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            .. U32(52), // state_offsets[0]
            0x55, // On, On, On, On
            0xCD, 0xCD, 0xCD, // trailing padding garbage
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_DiscoFloorWithNonZeroPrefixPadding_ReproducesInputBytes()
    {
        // Regression test: real archives leave a prefix string's trailing 4-byte-alignment padding
        // as whatever the authoring tool's buffer already held, not zero. A codec that always
        // zeroes it on write can't reproduce that.
        byte[] data =
        [
            .. Prefix(),
            .. U32(0x00000002), // flags: Enabled
            .. F32(0.1f), .. F32(0.1f),
            .. U32(36), .. U32(40), .. U32(44),
            .. U32(0), // state_mask_size
            .. U32(48), // states_offset
            .. U32(0), // states_size
            (byte)'A', (byte)'B', 0x00, 0xAB, // off prefix: "AB" + null + 1 byte garbage padding
            0x00, 0x00, 0x00, 0x00, // transition prefix
            0x00, 0x00, 0x00, 0x00, // on prefix
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_DiscoFloorWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = SampleData(tail: [0xDE, 0xAD, 0xBE, 0xEF]);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_DiscoFloorWithNoStates_ReproducesInputBytes()
    {
        byte[] offPrefix = PaddedPrefixString("OFF0");
        byte[] transitionPrefix = PaddedPrefixString("YEL0");
        byte[] onPrefix = PaddedPrefixString("RED0");

        uint offPrefixOffset = 36;
        uint transitionPrefixOffset = offPrefixOffset + (uint)offPrefix.Length;
        uint onPrefixOffset = transitionPrefixOffset + (uint)transitionPrefix.Length;
        uint statesOffset = onPrefixOffset + (uint)onPrefix.Length;

        byte[] data =
        [
            .. Prefix(),
            .. U32(0x00000002), // flags: Enabled
            .. F32(0.25f), .. F32(1.0f),
            .. U32(offPrefixOffset), .. U32(transitionPrefixOffset), .. U32(onPrefixOffset),
            .. U32(0), // state_mask_size
            .. U32(statesOffset),
            .. U32(0), // states_size
            .. offPrefix, .. transitionPrefix, .. onPrefix,
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_DiscoFloor_UnderIncredibles_DegradesToGenericBaseAsset()
    {
        byte[] data = SampleData();

        var asset = Read(data, IncrediblesSerializer.DefaultProfile);

        Assert.IsNotType<DiscoFloorAsset>(asset);
        Assert.IsType<BaseAsset>(asset, exactMatch: false);
        // DiscoFloor is BaseAsset-shaped, so an unsupported game still parses the 8-byte
        // BaseAssetPrefix before falling back - only the bytes after it land in the unparsed tail.
        Assert.Equal(data[8..], asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Write_AfterMutatingATile_DiscardsCapturedMaskGarbage()
    {
        byte[] data =
        [
            .. Prefix(),
            .. U32(0x00000002),
            .. F32(0.1f), .. F32(0.1f),
            .. U32(36), .. U32(40), .. U32(44),
            .. U32(3),
            .. U32(48),
            .. U32(1),
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            .. U32(52),
            0xE1, // On, Off, Random, garbage in the unused 4th slot
            0x00, 0x00, 0x00,
        ];

        var asset = (DiscoFloorAsset)Read(data);
        asset.States[0].Tiles[1] = TileState.On;

        byte[] rewritten = Write(asset);

        // The garbage bits are only preserved verbatim while Tiles matches what was read - once
        // mutated, the mask is re-encoded cleanly, and the previously-garbage bits come back zeroed.
        // The mutation doesn't change the layout, so the mask byte still lands at absolute offset 60
        // (8-byte BaseAssetPrefix + 36-byte struct + 3 empty, 4-byte-padded prefixes + one 4-byte
        // state offset).
        byte maskByte = rewritten[60];
        Assert.Equal(0, maskByte >> 6);
    }

    [Fact]
    public void Write_AfterMutatingAPrefix_DiscardsCapturedPaddingGarbage()
    {
        byte[] data =
        [
            .. Prefix(),
            .. U32(0x00000002),
            .. F32(0.1f), .. F32(0.1f),
            .. U32(36), .. U32(40), .. U32(44),
            .. U32(0),
            .. U32(48),
            .. U32(0),
            (byte)'A', (byte)'B', 0x00, 0xAB,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ];

        var asset = (DiscoFloorAsset)Read(data);
        asset.OffPrefix = "CD";

        byte[] rewritten = Write(asset);

        // Same length as "AB", so the layout is unchanged and the prefix still starts at absolute
        // offset 44 (8-byte BaseAssetPrefix + 36-byte struct).
        Assert.Equal((byte)'C', rewritten[44]);
        Assert.Equal((byte)'D', rewritten[45]);
        Assert.Equal(0x00, rewritten[46]);
        Assert.Equal(0x00, rewritten[47]);
    }

    [Fact]
    public void Write_AfterAddingAState_DiscardsCapturedTrailingPaddingGarbage()
    {
        byte[] data =
        [
            .. Prefix(),
            .. U32(0x00000002),
            .. F32(0.1f), .. F32(0.1f),
            .. U32(36), .. U32(40), .. U32(44),
            .. U32(4),
            .. U32(48),
            .. U32(1),
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            .. U32(52),
            0x55,
            0xCD, 0xCD, 0xCD, // trailing padding garbage
        ];

        var asset = (DiscoFloorAsset)Read(data);
        asset.States.Add(new State { Tiles = { TileState.On, TileState.On, TileState.On, TileState.On } });

        byte[] rewritten = Write(asset);

        // A second 1-byte mask changes the required trailing padding from 3 bytes to 2, so the
        // captured 3-byte garbage no longer applies and a clean 2-byte zero pad is written instead.
        Assert.Equal([0x00, 0x00], rewritten[^2..]);
    }

    [Fact]
    public void TileCount_DisagreeingWithStates_IsStoredIndependently()
    {
        var asset = new DiscoFloorAsset();
        asset.States.Add(new State());
        asset.States[0].Tiles.Add(TileState.Off);

        asset.Physical.TileCount = 5;

        Assert.Equal(5u, asset.Physical.TileCount);
        Assert.Single(asset.States[0].Tiles);
    }

    [Fact]
    public void TileCount_MatchingStates_DerivesFromFirstState()
    {
        var asset = new DiscoFloorAsset();
        asset.States.Add(new State());
        asset.States[0].Tiles.Add(TileState.On);
        asset.States[0].Tiles.Add(TileState.Off);

        asset.Physical.TileCount = 2;
        asset.States[0].Tiles.Add(TileState.Random);

        Assert.Equal(3u, asset.Physical.TileCount);
    }

    [Fact]
    public void StateCount_DisagreeingWithStates_IsStoredIndependently()
    {
        var asset = new DiscoFloorAsset();
        asset.States.Add(new State());

        asset.Physical.StateCount = 5;

        Assert.Equal(5u, asset.Physical.StateCount);
        Assert.Single(asset.States);
    }

    [Fact]
    public void StateCount_MatchingStates_DerivesFromStates()
    {
        var asset = new DiscoFloorAsset();
        asset.States.Add(new State());
        asset.States.Add(new State());

        asset.Physical.StateCount = 2;
        asset.States.Add(new State());

        Assert.Equal(3u, asset.Physical.StateCount);
    }
}

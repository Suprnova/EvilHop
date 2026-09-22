using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class CutsceneTableAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(Serializer serializer)
    {
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.CutsceneTable;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile profile, Serializer serializer)
    {
        var (header, debug) = HeaderFor(serializer);
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

    // N100F's Cutscene/CutsceneTable payloads are little-endian even on GameCube - see
    // ICutsceneHeader.HeaderReader/HeaderWriter - unlike every other field these fixtures build.
    private static byte[] Count(uint value) => BitConverter.GetBytes(value);

    private static byte[] Header(uint assetId, uint numData, uint numTime, uint headerSize) =>
    [
        .. BitConverter.GetBytes(0x4E535443u), // Magic "CTSN"
        .. BitConverter.GetBytes(assetId),
        .. BitConverter.GetBytes(numData),
        .. BitConverter.GetBytes(numTime),
        0x00, 0x00, 0x00, 0x00, // MaxModel
        0x00, 0x00, 0x00, 0x00, // MaxBufEven
        0x00, 0x00, 0x00, 0x00, // MaxBufOdd
        .. BitConverter.GetBytes(headerSize),
        0x00, 0x00, 0x00, 0x00, // VisCount
        0x00, 0x00, 0x00, 0x00, // VisSize
        0x00, 0x00, 0x00, 0x00, // BreakCount
        0x00, 0x00, 0x00, 0x00, // pad
    ];

    private static byte[] FixedString(string value, int length) =>
    [
        .. System.Text.Encoding.ASCII.GetBytes(value),
        .. new byte[length - value.Length],
    ];

    private static byte[] DataEntry(uint dataType, uint assetId, uint chunkSize, uint fileOffset) =>
    [
        .. BitConverter.GetBytes(dataType),
        .. BitConverter.GetBytes(assetId),
        .. BitConverter.GetBytes(chunkSize),
        .. BitConverter.GetBytes(fileOffset),
    ];

    // One data entry (0x10) plus a 4-byte unparsed remainder - standing in for the (NumTime + 1)
    // TimeChunkOffs entries a real archive would carry there.
    private static byte[] EntryWithDataAndTail(uint assetId) =>
    [
        .. Header(assetId, numData: 1, numTime: 0, headerSize: 0x64),
        .. FixedString("boss_intro", 16),
        .. FixedString(string.Empty, 16),
        .. DataEntry(1, 0xAABBCCDD, 100, 2048),
        0xDE, 0xAD, 0xBE, 0xEF,
    ];

    // No data entries and no remainder - HeaderSize exactly covers the fixed header and sound names.
    private static byte[] EntryWithoutDataOrTail(uint assetId) =>
    [
        .. Header(assetId, numData: 0, numTime: 0, headerSize: 0x50),
        .. FixedString(string.Empty, 16),
        .. FixedString(string.Empty, 16),
    ];

    private static byte[] N100FData(byte[]? tail = null) =>
    [
        .. Count(2),
        .. EntryWithDataAndTail(0x11111111),
        .. EntryWithoutDataOrTail(0x22222222),
        .. tail ?? [],
    ];

    [Fact]
    public void Read_CutsceneTable_ProducesCutsceneTableAsset() =>
        Assert.IsType<CutsceneTableAsset>(Read(N100FData(), N100FSerializer.DefaultProfile, new N100FSerializer()));

    [Fact]
    public void Read_CutsceneTable_UnderN100F_PopulatesEntries()
    {
        var asset = (CutsceneTableAsset)Read(N100FData(), N100FSerializer.DefaultProfile, new N100FSerializer());

        Assert.Equal(2u, asset.Physical.Count);
        Assert.Equal(2, asset.Cutscenes.Count);

        var first = asset.Cutscenes[0];
        Assert.Equal(new AssetId(0x11111111), first.Physical.AssetId);
        Assert.Equal("boss_intro", first.SoundLeft);
        var entry = Assert.Single(first.Data);
        Assert.Equal(CutsceneAsset.ModelKind.RWModel, entry.ModelKind);
        Assert.Equal(new AssetId(0xAABBCCDD), entry.AssetId);

        var second = asset.Cutscenes[1];
        Assert.Equal(new AssetId(0x22222222), second.Physical.AssetId);
        Assert.Empty(second.Data);
    }

    [Fact]
    public void Read_CutsceneTable_KeepsEachEntrysHeaderRemainderSeparate()
    {
        var asset = (CutsceneTableAsset)Read(N100FData(), N100FSerializer.DefaultProfile, new N100FSerializer());

        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, asset.Cutscenes[0].GetUnparsedTail().ToArray());
        Assert.Empty(asset.Cutscenes[1].GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Read_ThenWrite_CutsceneTable_UnderN100F_ReproducesInputBytes()
    {
        byte[] data = N100FData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile, new N100FSerializer()), profile));
    }

    [Fact]
    public void Read_ThenWrite_CutsceneTableWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = N100FData([0xCA, 0xFE]);
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile, new N100FSerializer()), profile));
    }

    [Fact]
    public void Read_ThenWrite_EmptyCutsceneTable_ReproducesInputBytes()
    {
        byte[] data = Count(0);
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile, new N100FSerializer()), profile));
    }

    [Fact]
    public void Read_CutsceneTable_UnderUnsupportedGame_DegradesToGenericAsset()
    {
        byte[] data = N100FData();

        var asset = Read(data, RatatouilleSerializer.DefaultProfile, new RatatouilleSerializer());

        Assert.IsNotType<CutsceneTableAsset>(asset);
    }

    [Fact]
    public void Count_WhenNotOverridden_DerivesFromCutscenesCount()
    {
        var asset = new CutsceneTableAsset();
        asset.Cutscenes.Add(new CutsceneTableEntry());
        asset.Cutscenes.Add(new CutsceneTableEntry());

        Assert.Equal(2u, asset.Physical.Count);
    }

    [Fact]
    public void Count_WhenOverridden_DivergesFromCutscenesCount()
    {
        var asset = new CutsceneTableAsset();
        asset.Cutscenes.Add(new CutsceneTableEntry());

        asset.Physical.Count = 5;

        Assert.Equal(5u, asset.Physical.Count);
        Assert.Single(asset.Cutscenes);
    }
}

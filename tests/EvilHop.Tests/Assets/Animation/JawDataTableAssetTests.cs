using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class JawDataTableAssetTests
{
    private static readonly byte[] JawData1 = [10, 20, 30];
    private static readonly byte[] JawData2 = [5, 15];

    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.JawDataTable;
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

    private static byte[] Count(int count) => [.. BitConverter.GetBytes(count).Reverse()];

    private static int Align4(int value) => (value + 3) & ~3;

    private static int Padding(int size) => (4 - (size % 4)) % 4;

    private static byte[] DirectoryEntry(uint soundId, int dataStart, int dataLength) =>
    [
        .. BitConverter.GetBytes(soundId).Reverse(),
        .. BitConverter.GetBytes(dataStart).Reverse(),
        .. BitConverter.GetBytes(dataLength).Reverse(),
    ];

    // BFBB/TSSM: leading Length is little-endian on every platform, no other fields.
    private static byte[] ClassicJawEntry(byte[] jawData) =>
    [
        .. BitConverter.GetBytes(jawData.Length),
        .. jawData,
        .. new byte[Padding(4 + jawData.Length)],
    ];

    // ROTU: leading Length is platform-endian, followed by an extra unknown field.
    private static byte[] RotuJawEntry(byte[] jawData, uint unknown) =>
    [
        .. BitConverter.GetBytes(jawData.Length).Reverse(),
        .. BitConverter.GetBytes(unknown).Reverse(),
        .. jawData,
        .. new byte[Padding(8 + jawData.Length)],
    ];

    private static byte[] ClassicData() =>
    [
        .. Count(2),
        .. DirectoryEntry(0x11111111, 0, 4 + JawData1.Length),
        .. DirectoryEntry(0x22222222, Align4(4 + JawData1.Length), 4 + JawData2.Length),
        .. ClassicJawEntry(JawData1),
        .. ClassicJawEntry(JawData2),
    ];

    private static byte[] RotuData() =>
    [
        .. Count(2),
        .. DirectoryEntry(0x11111111, 0, 8 + JawData1.Length),
        .. DirectoryEntry(0x22222222, Align4(8 + JawData1.Length), 8 + JawData2.Length),
        .. RotuJawEntry(JawData1, 1),
        .. RotuJawEntry(JawData2, 2),
    ];

    [Fact]
    public void Read_JawDataTable_UnderBfbb_ProducesJawDataTableAsset() =>
        Assert.IsType<JawDataTableAsset>(Read(ClassicData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_JawDataTable_UnderBfbb_PopulatesEveryField()
    {
        var asset = (JawDataTableAsset)Read(ClassicData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(2, asset.Entries.Count);

        var first = asset.Entries[0];
        Assert.Equal(new AssetId(0x11111111), first.SoundId);
        Assert.Equal(JawData1, (byte[])[.. first.JawData]);
        Assert.Equal(0u, first.Unknown);

        var second = asset.Entries[1];
        Assert.Equal(new AssetId(0x22222222), second.SoundId);
        Assert.Equal(JawData2, (byte[])[.. second.JawData]);
        Assert.Equal(0u, second.Unknown);
    }

    [Fact]
    public void Read_JawDataTable_UnderROTU_PopulatesUnknownField()
    {
        var asset = (JawDataTableAsset)Read(RotuData(), ROTUSerializer.DefaultProfile);

        var first = asset.Entries[0];
        Assert.Equal(JawData1, (byte[])[.. first.JawData]);
        Assert.Equal(1u, first.Unknown);

        var second = asset.Entries[1];
        Assert.Equal(JawData2, (byte[])[.. second.JawData]);
        Assert.Equal(2u, second.Unknown);
    }

    [Fact]
    public void Read_ThenWrite_JawDataTableUnderBfbb_ReproducesInputBytes()
    {
        byte[] data = ClassicData();
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_JawDataTableUnderTSSM_ReproducesInputBytes()
    {
        byte[] data = ClassicData();
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_JawDataTableUnderROTU_ReproducesInputBytes()
    {
        byte[] data = RotuData();
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_JawDataTableWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = Count(0);
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_JawDataTableWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. ClassicData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_JawDataTable_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = ClassicData();

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<JawDataTableAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Count_DisagreeingWithEntries_IsStoredIndependently()
    {
        var asset = new JawDataTableAsset();
        asset.Entries.Add(new JawDataTableEntry());

        asset.Physical.Count = 5;

        Assert.Equal(5, asset.Physical.Count);
        Assert.Single(asset.Entries);
    }

    [Fact]
    public void Count_MatchingEntries_DerivesFromEntries()
    {
        var asset = new JawDataTableAsset();
        asset.Entries.Add(new JawDataTableEntry());
        asset.Entries.Add(new JawDataTableEntry());

        asset.Physical.Count = 2;
        asset.Entries.Add(new JawDataTableEntry());

        Assert.Equal(3, asset.Physical.Count);
    }
}

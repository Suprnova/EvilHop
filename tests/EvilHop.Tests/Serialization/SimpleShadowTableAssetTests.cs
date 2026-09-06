using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class SimpleShadowTableAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.SimpleShadowTable;
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

    private static byte[] EntryBytes(uint modelId, uint shadowModelId, uint unknown) =>
    [
        .. BitConverter.GetBytes(modelId).Reverse(),
        .. BitConverter.GetBytes(shadowModelId).Reverse(),
        .. BitConverter.GetBytes(unknown).Reverse(),
    ];

    private static byte[] TableBytes(params byte[][] entries) =>
    [
        .. BitConverter.GetBytes((uint)entries.Length).Reverse(),
        .. entries.SelectMany(entry => entry),
    ];

    [Fact]
    public void Read_SimpleShadowTable_ProducesSimpleShadowTableAsset() =>
        Assert.IsType<SimpleShadowTableAsset>(Read(TableBytes()));

    [Fact]
    public void Read_SimpleShadowTable_PopulatesEntries()
    {
        byte[] data = TableBytes(EntryBytes(0xAABBCCDD, 0x11223344, 1));

        var asset = (SimpleShadowTableAsset)Read(data);

        var entry = Assert.Single(asset.Entries);
        Assert.Equal(new AssetId(0xAABBCCDD), entry.ModelId);
        Assert.Equal(new AssetId(0x11223344), entry.ShadowModelId);
        Assert.Equal(1u, entry.Unknown);
    }

    [Fact]
    public void Read_ThenWrite_SimpleShadowTableWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = TableBytes();

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SimpleShadowTableWithMultipleEntries_ReproducesInputBytes()
    {
        byte[] data = TableBytes(
            EntryBytes(0xAABBCCDD, 0x11223344, 0),
            EntryBytes(0x01020304, 0x05060708, 1));

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SimpleShadowTableWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. TableBytes(EntryBytes(1, 2, 0)), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_SimpleShadowTable_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = TableBytes(EntryBytes(1, 2, 0));

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<SimpleShadowTableAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Write_SimpleShadowTableAsset_UnderN100F_StillWritesItsOwnFields()
    {
        var asset = new SimpleShadowTableAsset { Type = AssetType.SimpleShadowTable };
        asset.Entries.Add(new SimpleShadowTableEntry { ModelId = new AssetId(1), ShadowModelId = new AssetId(2), Unknown = 1 });

        Assert.Equal(TableBytes(EntryBytes(1, 2, 1)), Write(asset, N100FSerializer.DefaultProfile));
    }
}

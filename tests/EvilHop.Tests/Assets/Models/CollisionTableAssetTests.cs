using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class CollisionTableAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.CollisionTable;
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

    private static byte[] EntryBytes(uint modelId, uint collisionModelId, uint cameraCollisionModelId) =>
    [
        .. BitConverter.GetBytes(modelId).Reverse(),
        .. BitConverter.GetBytes(collisionModelId).Reverse(),
        .. BitConverter.GetBytes(cameraCollisionModelId).Reverse(),
    ];

    private static byte[] TableBytes(params byte[][] entries) =>
    [
        .. BitConverter.GetBytes((uint)entries.Length).Reverse(),
        .. entries.SelectMany(entry => entry),
    ];

    [Fact]
    public void Read_CollisionTable_ProducesCollisionTableAsset() =>
        Assert.IsType<CollisionTableAsset>(Read(TableBytes()));

    [Fact]
    public void Read_CollisionTable_PopulatesEntries()
    {
        byte[] data = TableBytes(EntryBytes(0xAABBCCDD, 0, 0x11223344));

        var asset = (CollisionTableAsset)Read(data);

        var entry = Assert.Single(asset.Entries);
        Assert.Equal(new AssetId(0xAABBCCDD), entry.ModelId);
        Assert.Equal(AssetId.None, entry.CollisionModelId);
        Assert.Equal(new AssetId(0x11223344), entry.CameraCollisionModelId);
    }

    [Fact]
    public void Read_ThenWrite_CollisionTableWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = TableBytes();

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_CollisionTableWithMultipleEntries_ReproducesInputBytes()
    {
        byte[] data = TableBytes(
            EntryBytes(0xAABBCCDD, 0, 0x11223344),
            EntryBytes(0x01020304, 0x05060708, 0));

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_CollisionTableWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. TableBytes(EntryBytes(1, 2, 3)), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_CollisionTable_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = TableBytes(EntryBytes(1, 2, 3));

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<CollisionTableAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Write_CollisionTableAsset_UnderN100F_StillWritesItsOwnFields()
    {
        var asset = new CollisionTableAsset { Type = AssetType.CollisionTable };
        asset.Entries.Add(new CollisionTableEntry { ModelId = new AssetId(1), CameraCollisionModelId = new AssetId(3) });

        Assert.Equal(TableBytes(EntryBytes(1, 0, 3)), Write(asset, N100FSerializer.DefaultProfile));
    }
}

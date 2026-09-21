using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using static EvilHop.Assets.GroupAsset;

namespace EvilHop.Tests.Serialization;

public class GroupAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Group;
        header.Debug = debug;

        return (header, debug);
    }

    private static GroupAsset Read(byte[] data)
    {
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), Endianness.Big);
        return (GroupAsset)AssetCodecs.Read(reader, header, debug, N100FSerializer.DefaultProfile);
    }

    private static byte[] Write(GroupAsset asset)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true))
            AssetCodecs.Write(asset, writer, N100FSerializer.DefaultProfile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x11,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] Header(short itemCount, short groupFlags) =>
    [
        (byte)(itemCount >> 8), (byte)itemCount,
        (byte)(groupFlags >> 8), (byte)groupFlags,
    ];

    private static byte[] AssetIdBytes(uint value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        .. AssetIdBytes(destinationAssetId),
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    [Fact]
    public void Read_Group_ProducesGroupAsset() =>
        Assert.IsType<GroupAsset>(Read([.. Prefix(0), .. Header(0, 0)]));

    [Fact]
    public void Read_Group_PopulatesItemsAndGroupFlags()
    {
        byte[] data =
        [
            .. Prefix(0),
            .. Header(3, 1),
            .. AssetIdBytes(0xAABBCCDD),
            .. AssetIdBytes(0x11223344),
            .. AssetIdBytes(0x55667788),
        ];

        var asset = Read(data);

        Assert.Equal(GroupEventMode.Random, asset.GroupFlags);
        Assert.Equal(3, asset.Items.Count);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Items[0]);
        Assert.Equal(new AssetId(0x55667788), asset.Items[2]);
    }

    [Fact]
    public void Read_Group_ItemCountKeepsDerivingAfterItemsAreMutated()
    {
        byte[] data = [.. Prefix(0), .. Header(1, 0), .. AssetIdBytes(0xAABBCCDD)];

        var asset = Read(data);
        Assert.Equal(1, asset.Physical.ItemCount);

        asset.Items.Add(AssetId.None);

        Assert.Equal(2, asset.Physical.ItemCount);
    }

    [Fact]
    public void Read_Group_LinkCountKeepsDerivingAfterLinksAreMutated()
    {
        byte[] data = [.. Prefix(1), .. Header(0, 0), .. LinkBytes(0, 0, 0)];

        var asset = Read(data);
        Assert.Equal(1, asset.Physical.LinkCount);

        asset.Links.Add(new Link());

        Assert.Equal(2, asset.Physical.LinkCount);
    }

    [Fact]
    public void Read_ThenWrite_GroupWithNoItemsOrLinks_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(0), .. Header(0, 0)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_GroupWithItemsAndLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(1),
            .. Header(2, 2),
            .. AssetIdBytes(0xAABBCCDD),
            .. AssetIdBytes(0x11223344),
            .. LinkBytes(1, 2, 0x55667788),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_GroupWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(0), .. Header(0, 0), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }
}

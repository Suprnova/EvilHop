using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class PickupTableAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.PickupTable;
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

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] EntryBytes(uint pickupHash, byte pickupType, byte pickupIndex, ushort flags, uint quantity, uint modelId, uint animId) =>
    [
        .. U32(pickupHash),
        pickupType, pickupIndex,
        (byte)(flags >> 8), (byte)flags,
        .. U32(quantity),
        .. U32(modelId),
        .. U32(animId),
    ];

    private static byte[] Data(uint magic = 0x4B434950, params byte[][] entries) =>
    [
        .. U32(magic),
        .. U32((uint)entries.Length),
        .. entries.SelectMany(e => e),
    ];

    [Fact]
    public void Read_PickupTable_ProducesPickupTableAsset() =>
        Assert.IsType<PickupTableAsset>(Read(Data()));

    [Fact]
    public void Read_PickupTable_PopulatesEntries()
    {
        byte[] data = Data(entries:
        [
            EntryBytes(0xFA607BCB, 0xCD, 0xCD, 0x0000, 1, 0x567CD99A, 0),
        ]);

        var asset = (PickupTableAsset)Read(data);

        Assert.Single(asset.Entries);
        Assert.Equal(0xFA607BCBu, asset.Entries[0].PickupHash);
        Assert.Equal(0xCD, asset.Entries[0].PickupType);
        Assert.Equal(0xCD, asset.Entries[0].PickupIndex);
        Assert.Equal(0, asset.Entries[0].Flags);
        Assert.Equal(1u, asset.Entries[0].Quantity);
        Assert.Equal(new AssetId(0x567CD99A), asset.Entries[0].ModelId);
        Assert.Equal(AssetId.None, asset.Entries[0].AnimId);
    }

    [Fact]
    public void Read_ThenWrite_PickupTableWithMultipleEntries_ReproducesInputBytes()
    {
        byte[] data = Data(entries:
        [
            EntryBytes(0x11111111, 0, 1, 0x1234, 5, 0x22222222, 0x33333333),
            EntryBytes(0x44444444, 0xCD, 0xCD, 0, 1, 0x55555555, 0),
        ]);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_PickupTableWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = Data();

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_PickupTableWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void EntryCount_DisagreeingWithEntries_IsStoredIndependently()
    {
        var asset = new PickupTableAsset();
        asset.Entries.Add(new PickupTableEntry());

        asset.Physical.EntryCount = 5;

        Assert.Equal(5u, asset.Physical.EntryCount);
        Assert.Single(asset.Entries);
    }

    [Fact]
    public void EntryCount_MatchingEntries_DerivesFromEntries()
    {
        var asset = new PickupTableAsset();
        asset.Entries.Add(new PickupTableEntry());
        asset.Entries.Add(new PickupTableEntry());

        asset.Physical.EntryCount = 2;
        asset.Entries.Add(new PickupTableEntry());

        Assert.Equal(3u, asset.Physical.EntryCount);
    }

    [Fact]
    public void Read_ThenWrite_RealBFBBExemplar_ReproducesInputBytes()
    {
        // bfbb/prototype_2003-10-01/GC/NTSC-U/US/boot.HIP, AHDR id=0x78E38FCE
        byte[] data =
        [
            .. Data(entries:
            [
                EntryBytes(0xFA607BCB, 0xCD, 0xCD, 0x0000, 1, 0x567CD99A, 0),
                EntryBytes(0x6D4A4181, 0xCD, 0xCD, 0x0000, 1, 0x644E22AC, 0),
                EntryBytes(0x079A0734, 0xCD, 0xCD, 0x0000, 1, 0x814E75DD, 0),
            ]),
        ];

        var asset = (PickupTableAsset)Read(data);

        Assert.Equal(0x4B434950u, ((Physical.IPickupTableAsset)asset).Magic);
        Assert.Equal(3, asset.Entries.Count);
        Assert.Equal(0xFA607BCBu, asset.Entries[0].PickupHash);
        Assert.Equal(0xCD, asset.Entries[0].PickupType);
        Assert.Equal(1u, asset.Entries[0].Quantity);
        Assert.Equal(new AssetId(0x567CD99A), asset.Entries[0].ModelId);

        Assert.Equal(data, Write(asset));
    }
}

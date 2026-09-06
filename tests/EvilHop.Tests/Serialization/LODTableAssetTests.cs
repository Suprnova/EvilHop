using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class LODTableAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.LODTable;
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

    private static byte[] BfbbEntry(uint baseModelId, float noRenderDist, uint lod1, uint lod2, uint lod3, float dist1, float dist2, float dist3) =>
    [
        .. BitConverter.GetBytes(baseModelId).Reverse(),
        .. BitConverter.GetBytes(noRenderDist).Reverse(),
        .. BitConverter.GetBytes(lod1).Reverse(),
        .. BitConverter.GetBytes(lod2).Reverse(),
        .. BitConverter.GetBytes(lod3).Reverse(),
        .. BitConverter.GetBytes(dist1).Reverse(),
        .. BitConverter.GetBytes(dist2).Reverse(),
        .. BitConverter.GetBytes(dist3).Reverse(),
    ];

    private static byte[] EntryWithFlags(uint baseModelId, float noRenderDist, uint flags, uint lod1, uint lod2, uint lod3, float dist1, float dist2, float dist3) =>
    [
        .. BitConverter.GetBytes(baseModelId).Reverse(),
        .. BitConverter.GetBytes(noRenderDist).Reverse(),
        .. BitConverter.GetBytes(flags).Reverse(),
        .. BitConverter.GetBytes(lod1).Reverse(),
        .. BitConverter.GetBytes(lod2).Reverse(),
        .. BitConverter.GetBytes(lod3).Reverse(),
        .. BitConverter.GetBytes(dist1).Reverse(),
        .. BitConverter.GetBytes(dist2).Reverse(),
        .. BitConverter.GetBytes(dist3).Reverse(),
    ];

    private static byte[] Count(int count) => [.. BitConverter.GetBytes(count).Reverse()];

    private static byte[] BfbbData() =>
    [
        .. Count(2),
        .. BfbbEntry(0x11111111, 35.0f, 0, 0, 0, 0, 0, 0),
        .. BfbbEntry(0x22222222, 40.0f, 0x33333333, 0x44444444, 0x55555555, 10.0f, 20.0f, 30.0f),
    ];

    private static byte[] TssmData() =>
    [
        .. Count(2),
        .. EntryWithFlags(0x11111111, 35.0f, 0, 0, 0, 0, 0, 0, 0),
        .. EntryWithFlags(0x22222222, 40.0f, 0, 0x33333333, 0x44444444, 0x55555555, 10.0f, 20.0f, 30.0f),
    ];

    [Fact]
    public void Read_LODTable_UnderBfbb_ProducesLODTableAsset() =>
        Assert.IsType<LODTableAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_LODTable_UnderBfbb_PopulatesEveryField()
    {
        var asset = (LODTableAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(2, asset.Entries.Count);

        var first = asset.Entries[0];
        Assert.Equal(new AssetId(0x11111111), first.BaseModelId);
        Assert.Equal(35.0f, first.NoRenderDistance);
        Assert.Equal(0u, first.Flags);
        Assert.Equal(default, first.Lod1ModelId);

        var second = asset.Entries[1];
        Assert.Equal(new AssetId(0x22222222), second.BaseModelId);
        Assert.Equal(40.0f, second.NoRenderDistance);
        Assert.Equal(new AssetId(0x33333333), second.Lod1ModelId);
        Assert.Equal(new AssetId(0x44444444), second.Lod2ModelId);
        Assert.Equal(new AssetId(0x55555555), second.Lod3ModelId);
        Assert.Equal(10.0f, second.Lod1Distance);
        Assert.Equal(20.0f, second.Lod2Distance);
        Assert.Equal(30.0f, second.Lod3Distance);
    }

    [Fact]
    public void Read_LODTable_UnderTSSM_PopulatesFlags()
    {
        byte[] data =
        [
            .. Count(1),
            .. EntryWithFlags(0x11111111, 35.0f, 0xCAFEF00D, 0, 0, 0, 0, 0, 0),
        ];

        var asset = (LODTableAsset)Read(data, TSSMSerializer.DefaultProfile);

        Assert.Equal(0xCAFEF00Du, asset.Entries[0].Flags);
    }

    [Fact]
    public void Read_ThenWrite_LODTableUnderBfbb_ReproducesInputBytes()
    {
        byte[] data = BfbbData();
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LODTableUnderTSSM_ReproducesInputBytes()
    {
        byte[] data = TssmData();
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LODTableUnderIncredibles_ReproducesInputBytes()
    {
        byte[] data = TssmData();
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LODTableUnderROTU_ReproducesInputBytes()
    {
        byte[] data = TssmData();
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LODTableWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = Count(0);
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LODTableWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_LODTable_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = BfbbData();

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<LODTableAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Count_DisagreeingWithEntries_IsStoredIndependently()
    {
        var asset = new LODTableAsset();
        asset.Entries.Add(new LODTableEntry());

        asset.Physical.Count = 5;

        Assert.Equal(5, asset.Physical.Count);
        Assert.Single(asset.Entries);
    }

    [Fact]
    public void Count_MatchingEntries_DerivesFromEntries()
    {
        var asset = new LODTableAsset();
        asset.Entries.Add(new LODTableEntry());
        asset.Entries.Add(new LODTableEntry());

        asset.Physical.Count = 2;
        asset.Entries.Add(new LODTableEntry());

        Assert.Equal(3, asset.Physical.Count);
    }
}

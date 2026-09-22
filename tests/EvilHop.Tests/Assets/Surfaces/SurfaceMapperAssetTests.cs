using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class SurfaceMapperAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(uint id = 0x45DF6453)
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Id = id;
        header.Type = AssetType.SurfaceMapper;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile profile, uint id = 0x45DF6453)
    {
        var (header, debug) = HeaderFor(id);
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

    private static byte[] AssetIdBytes(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] UInt32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Entry(uint surfaceId, uint materialIndex) => [.. AssetIdBytes(surfaceId), .. UInt32(materialIndex)];

    private static byte[] Header(uint selfId, uint count) => [.. AssetIdBytes(selfId), .. UInt32(count)];

    private static byte[] SampleData() =>
    [
        .. Header(0x45DF6453, 2),
        .. Entry(0xAABBCCDD, 5),
        .. Entry(0x11223344, 0xCA3E2C38),
    ];

    [Fact]
    public void Read_SurfaceMapper_ProducesSurfaceMapperAsset() =>
        Assert.IsType<SurfaceMapperAsset>(Read(SampleData(), N100FSerializer.DefaultProfile));

    [Fact]
    public void Read_SurfaceMapper_PopulatesEveryField()
    {
        var asset = (SurfaceMapperAsset)Read(SampleData(), N100FSerializer.DefaultProfile);

        Assert.Equal(2, asset.Entries.Count);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Entries[0].SurfaceId);
        Assert.Equal(5u, asset.Entries[0].MaterialIndex);
        Assert.Equal(new AssetId(0x11223344), asset.Entries[1].SurfaceId);
        Assert.Equal(0xCA3E2C38u, asset.Entries[1].MaterialIndex);
    }

    [Fact]
    public void Read_SurfaceMapper_SelfIdDerivesFromAssetIdWhenMatching()
    {
        var asset = (SurfaceMapperAsset)Read(SampleData(), N100FSerializer.DefaultProfile);

        Assert.Equal(asset.Id, asset.Physical.SelfId);
    }

    [Fact]
    public void Read_SurfaceMapper_SelfIdDisagreeingWithAssetId_IsPreserved()
    {
        byte[] data = [.. Header(0x99999999, 0)];

        var asset = (SurfaceMapperAsset)Read(data, N100FSerializer.DefaultProfile, id: 0x45DF6453);

        Assert.Equal(new AssetId(0x99999999), asset.Physical.SelfId);
        Assert.NotEqual(asset.Id, asset.Physical.SelfId);
    }

    [Fact]
    public void Read_SurfaceMapper_CountKeepsDerivingAfterEntriesAreMutated()
    {
        var asset = (SurfaceMapperAsset)Read(SampleData(), N100FSerializer.DefaultProfile);
        Assert.Equal(2u, asset.Physical.Count);

        asset.Entries.Add(new SurfaceMapperAsset.Entry());

        Assert.Equal(3u, asset.Physical.Count);
    }

    [Fact]
    public void Read_ThenWrite_SurfaceMapper_ReproducesInputBytes()
    {
        byte[] data = SampleData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SurfaceMapperWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = [.. Header(0x45DF6453, 0)];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SurfaceMapperWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. SampleData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Theory]
    [InlineData(0)] // N100F
    [InlineData(1)] // BFBB
    [InlineData(2)] // TSSM
    [InlineData(3)] // Incredibles
    [InlineData(4)] // ROTU
    [InlineData(5)] // Ratatouille
    public void Read_ThenWrite_SurfaceMapper_ReproducesInputBytesAcrossEveryGame(int gameIndex)
    {
        FormatProfile profile = gameIndex switch
        {
            0 => N100FSerializer.DefaultProfile,
            1 => BFBBSerializer.DefaultProfile,
            2 => TSSMSerializer.DefaultProfile,
            3 => IncrediblesSerializer.DefaultProfile,
            4 => ROTUSerializer.DefaultProfile,
            _ => RatatouilleSerializer.DefaultProfile,
        };
        byte[] data = SampleData();

        Assert.Equal(data, Write(Read(data, profile), profile));
    }
}

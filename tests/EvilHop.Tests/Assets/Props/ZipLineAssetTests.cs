using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class ZipLineAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.ZipLine;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    // incredibles/prototype_2004-07-19/GC/NTSC-U/US/NJ/nj01.HIP, AHDR id=0x735DE1CE
    private static byte[] Exemplar(byte linkCount = 0) =>
    [
        0x73, 0x5D, 0xE1, 0xCE, 0x00, linkCount, 0x00, 0x1D, 0x00, 0x00, 0x00, 0x00, 0xBE, 0xBB, 0xBA, 0xF9,
        0x41, 0x55, 0xD6, 0x49, 0xC3, 0x17, 0xA5, 0xD3, 0x00, 0x00, 0x00, 0x00, 0xF1, 0x26, 0x09, 0x69,
        0x00, 0x00, 0x00, 0x00, 0x41, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    ];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    [Fact]
    public void Read_ZipLine_ProducesZipLineAsset() =>
        Assert.IsType<ZipLineAsset>(Read(Exemplar()));

    [Fact]
    public void Read_ZipLine_PopulatesFields()
    {
        var asset = (ZipLineAsset)Read(Exemplar());

        Assert.Equal(13.364816f, asset.Position.Y);
        Assert.Equal(new AssetId(0xF1260969), asset.SplineId);
        Assert.Equal(AssetId.None, asset.BoundEntityId);
        Assert.Equal(10f, asset.Speed);
        Assert.Equal(0, asset.Physical.DismountType);
        Assert.Equal(0u, asset.Physical.ZipLineFlags);
    }

    [Fact]
    public void Read_ThenWrite_RealIncrediblesExemplar_ReproducesInputBytes() =>
        Assert.Equal(Exemplar(), Write(Read(Exemplar())));

    [Fact]
    public void Read_ThenWrite_ZipLineWithLinks_ReproducesInputBytes()
    {
        byte[] data = [.. Exemplar(linkCount: 1), .. LinkBytes(0x0A, 0x0B, 0x11223344)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ZipLineWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Exemplar(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ZipLine_UnderTSSM_DegradesToGenericAsset() =>
        Assert.IsNotType<ZipLineAsset>(Read(Exemplar(), TSSMSerializer.DefaultProfile), exactMatch: false);
}

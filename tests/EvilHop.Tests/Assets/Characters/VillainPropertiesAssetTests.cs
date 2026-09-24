using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class VillainPropertiesAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.VillainProperties;
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

    // bfbb/prototype_2003-10-01/GC/NTSC-U/US/bc/bc01.HIP, AHDR id=0x018461D8 - every VILP in the corpus is identical
    private static readonly byte[] Exemplar =
    [
        0xCD, 0xCD, 0xCD, 0xCD, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0xA0, 0x00, 0x00,
        0xFF, 0xFF, 0xFF, 0xFF,
    ];

    [Fact]
    public void Read_VillainProperties_ProducesVillainPropertiesAsset() =>
        Assert.IsType<VillainPropertiesAsset>(Read(Exemplar));

    [Fact]
    public void Read_VillainProperties_PopulatesFields()
    {
        var physical = ((VillainPropertiesAsset)Read(Exemplar)).Physical;

        Assert.Equal(0xCDCDCDCDu, physical.Unknown1);
        Assert.Equal(31, physical.Unknown5);
        Assert.Equal(5f, physical.Unknown8);
        Assert.Equal(-1, physical.Unknown9);
    }

    [Fact]
    public void Read_ThenWrite_RealBFBBExemplar_ReproducesInputBytes() =>
        Assert.Equal(Exemplar, Write(Read(Exemplar)));

    [Fact]
    public void Read_ThenWrite_VillainPropertiesWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Exemplar, 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Write_NewVillainProperties_WritesTheShippedValues() =>
        Assert.Equal(Exemplar, Write(new VillainPropertiesAsset()));

    [Fact]
    public void Read_VillainProperties_UnderTSSM_DegradesToGenericAsset() =>
        Assert.IsNotType<VillainPropertiesAsset>(Read(Exemplar, TSSMSerializer.DefaultProfile), exactMatch: false);
}

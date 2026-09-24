using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class SlidePropertyAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.SlideProperty;
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

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Fields(params float[] values) => [.. values.SelectMany(F32)];

    // incredibles/prototype_2004-07-19/GC/NTSC-U/US/LD/ld04.HIP, AHDR id=0xC3A06AE1
    private static readonly byte[] Exemplar =
    [
        0xC3, 0xA0, 0x6A, 0xE1, 0x46, 0x00, 0x00, 0x1D, 0x41, 0x50, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00,
        0x40, 0x40, 0x00, 0x00, 0x40, 0xA0, 0x00, 0x00, 0x41, 0xF0, 0x00, 0x00, 0x42, 0x34, 0x00, 0x00,
        0x3B, 0x44, 0x9B, 0xA6, 0x40, 0x40, 0x00, 0x00, 0x42, 0x8C, 0x00, 0x00, 0x42, 0x8C, 0x00, 0x00,
        0x41, 0x20, 0x00, 0x00,
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
    public void Read_SlideProperty_ProducesSlidePropertyAsset() =>
        Assert.IsType<SlidePropertyAsset>(Read(Exemplar));

    [Fact]
    public void Read_SlideProperty_PopulatesFields()
    {
        var physical = ((SlidePropertyAsset)Read(Exemplar)).Physical;

        Assert.Equal(
            [13f, 2f, 3f, 5f, 30f, 45f, 0.003f, 3f, 70f, 70f, 10f],
            [physical.Unknown1, physical.Unknown2, physical.Unknown3, physical.Unknown4, physical.Unknown5, physical.Unknown6,
             physical.Unknown7, physical.Unknown8, physical.Unknown9, physical.Unknown10, physical.Unknown11]);
    }

    [Fact]
    public void Read_ThenWrite_RealIncrediblesExemplar_ReproducesInputBytes() =>
        Assert.Equal(Exemplar, Write(Read(Exemplar)));

    [Fact]
    public void Read_ThenWrite_SlidePropertyWithLinks_ReproducesInputBytes()
    {
        byte[] data = [.. Exemplar[..5], 0x01, .. Exemplar[6..], .. LinkBytes(0x0A, 0x0B, 0x11223344)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SlidePropertyWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Exemplar, 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_SlideProperty_UnderTSSM_DegradesToGenericAsset() =>
        Assert.IsNotType<SlidePropertyAsset>(Read(Exemplar, TSSMSerializer.DefaultProfile), exactMatch: false);

    [Fact]
    public void Write_NewSlideProperty_WritesTheConstantValues()
    {
        var asset = new SlidePropertyAsset();

        byte[] data = Write(asset);

        Assert.Equal(Fields(0f, 0f, 3f, 0f, 0f, 45f, 0.003f, 3f, 70f, 70f, 10f), data[8..]);
    }
}

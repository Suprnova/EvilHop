using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Text;

namespace EvilHop.Tests.Serialization;

public class PortalAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Portal;
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

    private static byte[] Data(string storedScene, byte linkCount = 0) =>
    [
        0x58, 0xB3, 0xA7, 0x7B, 0x10, linkCount, 0x00, 0x1D, // BaseId, BaseType, LinkCount, BaseFlags
        0x4C, 0x70, 0x9E, 0x92,                           // CameraId
        0x1E, 0xEF, 0x39, 0xDE,                           // MarkerId
        0x43, 0x87, 0x00, 0x00,                           // Angle = 270
        .. Encoding.Latin1.GetBytes(storedScene),
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
    public void Read_Portal_ProducesPortalAsset() =>
        Assert.IsType<PortalAsset>(Read(Data("10BH")));

    [Fact]
    public void Read_Portal_PopulatesFields()
    {
        var asset = (PortalAsset)Read(Data("10BH"));

        Assert.Equal(new AssetId(0x4C709E92), asset.CameraId);
        Assert.Equal(new AssetId(0x1EEF39DE), asset.MarkerId);
        Assert.Equal(270f, asset.Angle);
    }

    [Fact]
    public void Read_PortalUnderBFBB_UnreversesSceneId() =>
        Assert.Equal("HB01", ((PortalAsset)Read(Data("10BH"))).SceneId);

    [Fact]
    public void Read_PortalUnderTSSM_KeepsSceneIdInOrder() =>
        Assert.Equal("BB03", ((PortalAsset)Read(Data("BB03"), TSSMSerializer.DefaultProfile)).SceneId);

    [Fact]
    public void Read_PortalUnderLittleEndianN100F_UnreversesSceneId()
    {
        var profile = N100FSerializer.DefaultProfile with { Platform = Platform.Xbox };
        // n100f/prototype_2003-07-08/XBOX/NTSC-U/US/b0/b001.HIP, AHDR id=0xFB08B123
        byte[] data =
        [
            0x23, 0xB1, 0x08, 0xFB, 0x10, 0x00, 0x1D, 0x00, 0xB2, 0xC2, 0x44, 0xBC, 0xC5, 0x39, 0x60, 0x75,
            0x00, 0x00, 0x00, 0x00, 0x42, 0x30, 0x30, 0x32,
        ];

        var asset = (PortalAsset)Read(data, profile);

        Assert.Equal("B002", asset.SceneId);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Write_PortalUnderBFBB_StoresSceneIdReversed()
    {
        var asset = (PortalAsset)Read(Data("10BH"));

        asset.SceneId = "JF01";

        Assert.Equal(Data("10FJ"), Write(asset));
    }

    [Theory]
    [InlineData("HB01")]
    [InlineData("hb01")]
    [InlineData(" nti")]
    public void Read_ThenWrite_PortalUnderBFBBAndTSSM_ReproducesInputBytes(string storedScene)
    {
        byte[] data = Data(storedScene);

        Assert.Equal(data, Write(Read(data)));
        Assert.Equal(data, Write(Read(data, TSSMSerializer.DefaultProfile), TSSMSerializer.DefaultProfile));
    }

    [Fact]
    public void Read_ThenWrite_PortalWithLinks_ReproducesInputBytes()
    {
        byte[] data = [.. Data("10BH", linkCount: 2), .. LinkBytes(0x1C, 0x0A, 0x11223344), .. LinkBytes(0x1D, 0x0B, 0x55667788)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_PortalWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data("10BH"), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Theory]
    [InlineData("HB1")]
    [InlineData("HB011")]
    public void Write_SceneIdNotFourCharacters_ThrowsArgumentException(string sceneId)
    {
        var asset = new PortalAsset { SceneId = sceneId };

        Assert.Throws<ArgumentException>(() => Write(asset));
    }

    [Fact]
    public void Read_ThenWrite_RealROTUExemplar_ReproducesInputBytes()
    {
        // rotu/prototype_2005-09-15/GC/NTSC-J/JP/A1/a101.HIP, AHDR id=0x3BFD9466
        byte[] data =
        [
            0x3B, 0xFD, 0x94, 0x66, 0x10, 0x00, 0x00, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x64, 0x3F, 0xC2, 0x16,
            0xCD, 0xCD, 0xCD, 0xCA, 0x41, 0x31, 0x30, 0x31,
        ];
        var profile = ROTUSerializer.DefaultProfile;

        var asset = (PortalAsset)Read(data, profile);

        Assert.Equal("A101", asset.SceneId);
        Assert.Equal(AssetId.None, asset.CameraId);
        Assert.Equal(data, Write(asset, profile));
    }
}

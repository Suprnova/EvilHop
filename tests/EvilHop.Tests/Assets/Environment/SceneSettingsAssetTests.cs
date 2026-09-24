using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class SceneSettingsAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.SceneSettings;
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

    private static byte[] Data(byte linkCount = 0) =>
    [
        0x46, 0x13, 0xC7, 0x67, 0x54, linkCount, 0x00, 0x1D, // BaseId, BaseType, LinkCount, BaseFlags
        0x00, 0x14, 0x00, 0x14, 0x00, 0x00, 0x40, 0xB9,     // Unknown1-4
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
    public void Read_SceneSettings_ProducesSceneSettingsAsset() =>
        Assert.IsType<SceneSettingsAsset>(Read(Data()));

    [Fact]
    public void Read_SceneSettings_PopulatesFields()
    {
        var asset = (SceneSettingsAsset)Read(Data());

        Assert.Equal(20, asset.Physical.Unknown1);
        Assert.Equal(20, asset.Physical.Unknown2);
        Assert.Equal(0, asset.Physical.Unknown3);
        Assert.Equal(0x40B9, asset.Physical.Unknown4);
    }

    [Fact]
    public void Read_ThenWrite_SceneSettingsWithLinks_ReproducesInputBytes()
    {
        byte[] data = [.. Data(linkCount: 1), .. LinkBytes(0x0A, 0x0B, 0x11223344)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SceneSettingsWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_SceneSettings_UnderTSSM_DegradesToGenericAsset() =>
        Assert.IsNotType<SceneSettingsAsset>(Read(Data(), TSSMSerializer.DefaultProfile), exactMatch: false);

    [Fact]
    public void Write_NewSceneSettings_WritesTheCommonValues()
    {
        var asset = new SceneSettingsAsset();

        byte[] data = Write(asset);

        Assert.Equal([0x00, 0x14, 0x00, 0x14, 0x00, 0x00, 0x00, 0x00], data[8..]);
    }

    [Fact]
    public void Read_ThenWrite_RealIncrediblesExemplar_ReproducesInputBytes()
    {
        // incredibles/prototype_2004-07-19/GC/NTSC-U/US/BM/bm01.HIP, AHDR id=0x4613C767
        byte[] data = [0x46, 0x13, 0xC7, 0x67, 0x54, 0x00, 0x00, 0x1D, 0x00, 0x14, 0x00, 0x14, 0x00, 0x00, 0x00, 0x00];

        var asset = (SceneSettingsAsset)Read(data);

        Assert.Equal(0x54, asset.Physical.BaseType);
        Assert.Equal(data, Write(asset));
    }
}

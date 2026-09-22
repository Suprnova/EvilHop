using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using static EvilHop.Assets.ParticleSystemAsset;

namespace EvilHop.Tests.Serialization;

public class ParticleSystemAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.ParticleSystem;
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

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x27,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] I32(int value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] Data(int systemType, uint parentId, uint textureId, byte flags, byte priority,
        short maxParticles, byte renderFunc, byte srcBlend, byte dstBlend, byte commandCount,
        byte[] commandData, byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. I32(systemType),
        .. U32(parentId),
        .. U32(textureId),
        flags, priority,
        (byte)(maxParticles >> 8), (byte)maxParticles,
        renderFunc, srcBlend, dstBlend, commandCount,
        .. I32(commandData.Length),
        .. commandData,
    ];

    [Fact]
    public void Read_ParticleSystem_ProducesParticleSystemAsset() =>
        Assert.IsType<ParticleSystemAsset>(Read(Data(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, [])));

    [Fact]
    public void Read_ParticleSystem_PopulatesFields()
    {
        byte[] data = Data(
            systemType: 0, parentId: 0xAABBCCDD, textureId: 0x11223344,
            flags: (byte)(Behavior.Visible | Behavior.UsePTankRender),
            priority: 5, maxParticles: 200, renderFunc: (byte)RenderingFunction.QuadStreak,
            srcBlend: 4, dstBlend: 0, commandCount: 2, commandData: [0x01, 0x02, 0x03, 0x04]);

        var asset = (ParticleSystemAsset)Read(data);

        Assert.Equal(new AssetId(0xAABBCCDD), asset.ParentId);
        Assert.Equal(new AssetId(0x11223344), asset.TextureId);
        Assert.Equal(Behavior.Visible | Behavior.UsePTankRender, asset.Flags);
        Assert.Equal(5, asset.Priority);
        Assert.Equal((ushort)200, asset.MaxParticles);
        Assert.Equal(RenderingFunction.QuadStreak, asset.RenderFunction);
        Assert.Equal(RwBlendFunction.SourceAlpha, asset.SourceBlend);
        Assert.Equal(RwBlendFunction.Zero, asset.DestinationBlend);
        Assert.Equal(2, ((Physical.IParticleSystemAsset)asset).CommandCount);
        Assert.Equal([0x01, 0x02, 0x03, 0x04], ((Physical.IParticleSystemAsset)asset).CommandData);
    }

    [Fact]
    public void Read_ThenWrite_ParticleSystemWithNoCommandsOrLinks_ReproducesInputBytes()
    {
        byte[] data = Data(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, []);
        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ParticleSystemWithCommandDataAndLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(0, 0xAABBCCDD, 0x11223344, (byte)Behavior.Visible, 3, 100,
                (byte)RenderingFunction.Sprite, 4, 1, 2, [0x00, 0x00, 0x00, 0x0C, 0x01, 0x00, 0x00, 0x00], linkCount: 1),
            .. LinkBytes(1, 2, 0x55667788),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ParticleSystemWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, []), 0xDE, 0xAD, 0xBE, 0xEF];
        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ParticleSystem_UnderIncredibles_DegradesToGenericBaseAsset()
    {
        byte[] data = Data(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, []);
        var asset = Read(data, IncrediblesSerializer.DefaultProfile);
        Assert.IsNotType<ParticleSystemAsset>(asset);
    }

    [Fact]
    public void Read_ThenWrite_RealBFBBExemplar_ReproducesInputBytes()
    {
        // bfbb/prototype_2003-10-01/GC/NTSC-U/US/b1/b101.HIP, AHDR id=0x63C124FD
        byte[] data =
        [
            0x63, 0xC1, 0x24, 0xFD, 0x27, 0x00, 0x00, 0x1D,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0xAB, 0x1D, 0x26, 0xF6,
            0x01, 0x00, 0x00, 0x00,
            0x00, 0x04, 0x01, 0x04,
            0x00, 0x00, 0x00, 0x74,
            .. new byte[0x74],
        ];

        var asset = (ParticleSystemAsset)Read(data);

        Assert.Equal(new AssetId(0x63C124FD), asset.Physical.BaseId);
        Assert.Equal(0x27, asset.Physical.BaseType);
        Assert.Equal(new AssetId(0xAB1D26F6), asset.TextureId);
        Assert.Equal(Behavior.Visible, asset.Flags);
        Assert.Equal(0, asset.Priority);
        Assert.Equal((ushort)0, asset.MaxParticles);
        Assert.Equal(RenderingFunction.Sprite, asset.RenderFunction);
        Assert.Equal(RwBlendFunction.SourceAlpha, asset.SourceBlend);
        Assert.Equal(RwBlendFunction.One, asset.DestinationBlend);
        Assert.Equal(4, ((Physical.IParticleSystemAsset)asset).CommandCount);
        Assert.Equal(0x74, ((Physical.IParticleSystemAsset)asset).CommandData.Length);

        Assert.Equal(data, Write(asset));
    }
}

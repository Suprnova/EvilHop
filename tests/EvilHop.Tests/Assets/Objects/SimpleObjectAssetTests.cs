using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class SimpleObjectAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.SimpleObject;
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

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x0B,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix(bool hasPadding, uint modelId, uint animListId) =>
    [
        0x01, 0x00, 0x00, 0x02,   // EntityFlags, Subtype, PFlags, CollisionFlags
        .. hasPadding ? new byte[4] : [],
        0x00, 0x00, 0x00, 0x00,   // SurfaceId
        .. new byte[36],          // Angle/Position/Scale
        .. new byte[16],          // ColorMultiplier
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        (byte)(modelId >> 24), (byte)(modelId >> 16), (byte)(modelId >> 8), (byte)modelId,
        (byte)(animListId >> 24), (byte)(animListId >> 16), (byte)(animListId >> 8), (byte)animListId,
    ];

    private static byte[] SimpFields(float animSpeed, uint initAnimState, byte collType, byte flags) =>
    [
        .. BitConverter.GetBytes(animSpeed).Reverse(),
        .. BitConverter.GetBytes(initAnimState).Reverse(),
        collType,
        flags,
        0x00, 0x00, // padding
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

    private static byte[] BfbbData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: true, 0xAABBCCDD, 0x11223344),
        .. SimpFields(1.5f, 2, collType: 2, flags: 0),
    ];

    private static byte[] TssmData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: false, 0xAABBCCDD, 0x11223344),
        .. SimpFields(1.5f, 2, collType: 0, flags: 8),
    ];

    [Fact]
    public void Read_SimpleObject_ProducesSimpleObjectAsset() =>
        Assert.IsType<SimpleObjectAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_SimpleObject_UnderBfbb_PopulatesEveryField()
    {
        var asset = (SimpleObjectAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(1.5f, asset.AnimationSpeed);
        Assert.Equal(2u, asset.InitialAnimationState);
        Assert.Equal(SimpleObjectCollisionType.Static, asset.CollisionType);
        Assert.Equal(0, asset.Physical.SimpleFlags);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Physical.ModelId);
        Assert.Equal(new AssetId(0x11223344), asset.Physical.AnimListId);
    }

    [Fact]
    public void Read_SimpleObject_UnderTSSM_PopulatesNonzeroFlags()
    {
        var asset = (SimpleObjectAsset)Read(TssmData(), TSSMSerializer.DefaultProfile);

        Assert.Equal(SimpleObjectCollisionType.None, asset.CollisionType);
        Assert.Equal(8, asset.Physical.SimpleFlags);
    }

    [Fact]
    public void Read_ThenWrite_SimpleObjectUnderBfbb_ReproducesInputBytes()
    {
        byte[] data = BfbbData();
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SimpleObjectUnderTSSM_ReproducesInputBytes()
    {
        byte[] data = TssmData();
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SimpleObjectWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_SimpleObjectWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void ModelId_SetThroughIHasModel_ProjectsOntoPhysicalModelId()
    {
        var asset = new SimpleObjectAsset();

        ((IHasModel)asset).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.ModelId);
    }

    [Fact]
    public void AnimListId_SetThroughIHasAnimList_ProjectsOntoPhysicalAnimListId()
    {
        var asset = new SimpleObjectAsset();

        ((IHasAnimList)asset).AnimListId = new AssetId(0xCAFEF00D);

        Assert.Equal(new AssetId(0xCAFEF00D), asset.Physical.AnimListId);
    }

    [Fact]
    public void SurfaceId_SetThroughIHasSurface_ProjectsOntoPhysicalSurfaceId()
    {
        var asset = new SimpleObjectAsset();

        ((IHasSurface)asset).SurfaceId = new AssetId(0x0BADF00D);

        Assert.Equal(new AssetId(0x0BADF00D), asset.Physical.SurfaceId);
    }
}

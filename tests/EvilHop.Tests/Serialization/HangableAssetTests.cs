using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class HangableAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Hangable;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x17,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix() =>
    [
        0x01, 0x00, 0x00, 0x02,   // EntityFlags, Subtype, PFlags, CollisionFlags
        0x00, 0x00, 0x00, 0x00,   // SurfaceId
        .. new byte[36],          // Angle/Position/Scale
        .. new byte[16],          // ColorMultiplier
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        0x00, 0x00, 0x00, 0x00,   // ModelId
        0x00, 0x00, 0x00, 0x00,   // AnimListId
    ];

    private static byte[] HangFields(uint flags, float pivotOffset, float leverArm, float gravity, float accel, float decay, float grabDelay, float stopDecel) =>
    [
        .. BitConverter.GetBytes(flags).Reverse(),
        .. BitConverter.GetBytes(pivotOffset).Reverse(),
        .. BitConverter.GetBytes(leverArm).Reverse(),
        .. BitConverter.GetBytes(gravity).Reverse(),
        .. BitConverter.GetBytes(accel).Reverse(),
        .. BitConverter.GetBytes(decay).Reverse(),
        .. BitConverter.GetBytes(grabDelay).Reverse(),
        .. BitConverter.GetBytes(stopDecel).Reverse(),
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

    private static byte[] Data(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(),
        .. HangFields(0x80000000, 3.0f, 5.0f, 50.0f, 6.0f, 0.01f, 0.5f, 4.0f),
    ];

    [Fact]
    public void Read_Hangable_ProducesHangableAsset() =>
        Assert.IsType<HangableAsset>(Read(Data()));

    [Fact]
    public void Read_Hangable_PopulatesEveryField()
    {
        var asset = (HangableAsset)Read(Data());

        Assert.Equal(0x80000000u, asset.Physical.HangFlags);
        Assert.Equal(3.0f, asset.PivotOffset);
        Assert.Equal(5.0f, asset.LeverArm);
        Assert.Equal(50.0f, asset.Gravity);
        Assert.Equal(6.0f, asset.Accel);
        Assert.Equal(0.01f, asset.Decay);
        Assert.Equal(0.5f, asset.GrabDelay);
        Assert.Equal(4.0f, asset.StopDecel);
    }

    [Fact]
    public void Read_ThenWrite_Hangable_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_HangableWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_Hangable_UnderROTU_PopulatesEveryField()
    {
        var asset = (HangableAsset)Read(Data(), ROTUSerializer.DefaultProfile);

        Assert.Equal(3.0f, asset.PivotOffset);
        Assert.Equal(50.0f, asset.Gravity);
    }

    [Fact]
    public void Read_Hangable_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = Data();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<HangableAsset>(asset);
    }

    [Fact]
    public void ModelId_SetThroughIHasModel_ProjectsOntoPhysicalModelId()
    {
        var asset = new HangableAsset();

        ((IHasModel)asset).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.ModelId);
    }

    [Fact]
    public void Flags_SetThroughPhysical_IsStoredIndependently()
    {
        var asset = new HangableAsset();

        asset.Physical.HangFlags = 0x80000000;

        Assert.Equal(0x80000000u, asset.Physical.HangFlags);
    }
}

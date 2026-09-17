using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class PendulumAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Pendulum;
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
        0x12,                   // BaseType
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
        0xAA, 0xBB, 0xCC, 0xDD,   // ModelId
        0x00, 0x00, 0x00, 0x00,   // AnimListId
    ];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] MotionBytes(byte flags, byte plane, float length, float range, float period, float phase, int blockSize)
    {
        byte[] fields =
        [
            0x05, 0x00, 0x00, 0x00,  // type = Pendulum, use_banking, flags
            flags, plane, 0x00, 0x00,
            .. F32(length), .. F32(range), .. F32(period), .. F32(phase),
        ];
        return [.. fields, .. new byte[blockSize - fields.Length]];
    }

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] Data(byte linkCount = 0, int blockSize = 0x30) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(),
        .. MotionBytes(0x00, 0x01, 8.0f, 0.5236f, 3.0f, 0.0f, blockSize),
    ];

    [Fact]
    public void Read_Pendulum_ProducesPendulumAsset() =>
        Assert.IsType<PendulumAsset>(Read(Data()));

    [Fact]
    public void Read_Pendulum_PopulatesMotionFields()
    {
        var asset = (PendulumAsset)Read(Data());

        Assert.Equal(0x00, asset.Motion.PendulumFlags);
        Assert.Equal(0x01, asset.Motion.Plane);
        Assert.Equal(8.0f, asset.Motion.Length);
        Assert.Equal(0.5236f, asset.Motion.Range);
        Assert.Equal(3.0f, asset.Motion.Period);
        Assert.Equal(0.0f, asset.Motion.Phase);
    }

    [Fact]
    public void Read_ThenWrite_Pendulum_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(linkCount: 1),
            .. LinkBytes(1, 2, 0x55667788),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_PendulumWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_PendulumUnderTSSM_UsesLargerMotionBlock_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(linkCount: 1, blockSize: 0x3C),
            .. LinkBytes(1, 2, 0x55667788),
        ];
        var profile = TSSMSerializer.DefaultProfile;

        var asset = (PendulumAsset)Read(data, profile);
        Assert.Equal(8.0f, asset.Motion.Length);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void ModelId_SetThroughIHasModel_ProjectsOntoPhysicalModelId()
    {
        var asset = new PendulumAsset();

        ((IHasModel)asset).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.ModelId);
    }

    [Fact]
    public void Read_ThenWrite_RealN100FExemplar_ReproducesInputBytes()
    {
        // n100f/prototype_2003-07-08/XBOX/NTSC-U/US/e0/e007.HIP, AHDR id=0xDA790AD7
        byte[] data =
        [
            0xD7, 0x0A, 0x79, 0xDA, 0x12, 0x01, 0x1D, 0x00,
            0x01, 0x00, 0x00, 0x02, 0x58, 0x9B, 0xFD, 0xB9,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x52, 0x42, 0x00, 0x00, 0x78, 0x41, 0x00, 0x00, 0x0B, 0x43,
            0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F,
            0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F,
            0x00, 0x00, 0x7F, 0x43,
            0x46, 0xA4, 0xCC, 0x49,
            0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x41, 0x92, 0x0A, 0x06, 0x3F, 0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x00, 0x00,
            .. new byte[24], // unused union padding, up to the 0x30 block size
            0xE4, 0x00, 0x18, 0x00, 0xDF, 0xC6, 0x71, 0xB8,
            .. new byte[16], // Params
            .. new byte[4],  // ParamWidgetAssetId
            .. new byte[4],  // CheckAssetId
        ];
        var profile = N100FSerializer.DefaultProfile with { Platform = Platform.Xbox };

        var asset = (PendulumAsset)Read(data, profile);

        Assert.Equal(new AssetId(0xDA790AD7), asset.Physical.BaseId);
        Assert.Equal(0x12, asset.Physical.BaseType);
        Assert.Equal(0, asset.Motion.PendulumFlags);
        Assert.Equal(0, asset.Motion.Plane);
        Assert.Equal(8.0f, asset.Motion.Length);
        Assert.Single(asset.Links);
        Assert.Equal(228, asset.Links[0].SourceEvent);
        Assert.Equal(24, asset.Links[0].DestinationEvent);
        Assert.Equal(new AssetId(0xB871C6DF), asset.Links[0].DestinationAssetId);

        Assert.Equal(data, Write(asset, profile));
    }
}

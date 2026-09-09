using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class CameraAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Camera;
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
        0x07,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] Vec3(float x, float y, float z) =>
    [
        .. BitConverter.GetBytes(x).Reverse(),
        .. BitConverter.GetBytes(y).Reverse(),
        .. BitConverter.GetBytes(z).Reverse(),
    ];

    private static byte[] F(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] SharedFields(
        short offsetStartFrames, short offsetEndFrames, float fov, float transitionTime,
        int transitionType, uint cameraFlags, float fadeUp, float fadeDown) =>
    [
        .. Vec3(1, 2, 3),   // Position
        .. Vec3(0, 0, 1),   // Forward
        .. Vec3(0, 1, 0),   // Up
        .. Vec3(1, 0, 0),   // Left
        .. Vec3(0, 0, 0),   // ViewOffset
        (byte)(offsetStartFrames >> 8), (byte)offsetStartFrames,
        (byte)(offsetEndFrames >> 8), (byte)offsetEndFrames,
        .. F(fov),
        .. F(transitionTime),
        .. BitConverter.GetBytes(transitionType).Reverse(),
        .. BitConverter.GetBytes(cameraFlags).Reverse(),
        .. F(fadeUp),
        .. F(fadeDown),
    ];

    private static byte[] Trailer(uint validFlags, uint markerId1, uint markerId2, byte camType) =>
    [
        .. BitConverter.GetBytes(validFlags).Reverse(),
        .. BitConverter.GetBytes(markerId1).Reverse(),
        .. BitConverter.GetBytes(markerId2).Reverse(),
        camType,
        0x00, 0x00, 0x00, // padding
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

    private static byte[] FollowData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. SharedFields(30, 45, 85f, 0f, 0, 0u, 0f, 0f),
        .. F(0f), .. F(-2f), .. F(1f), .. F(1f), .. F(0f), .. F(0f), // Rotation, Distance, Height, RubberBand, StartSpeed, EndSpeed
        .. Trailer(0x0001018F, 0, 0, camType: 0),
    ];

    private static byte[] ShoulderData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. SharedFields(30, 45, 60f, 0.5f, 5, 0u, 0f, 0f),
        .. F(4f), .. F(2f), .. F(3f), .. F(0.1f), .. new byte[8], // Distance, Height, RealignSpeed, RealignDelay, Reserved
        .. Trailer(0x0001018F, 0, 0, camType: 1),
    ];

    private static byte[] StaticData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. SharedFields(30, 45, 70f, 1f, 1, 0u, 0f, 0f),
        .. BitConverter.GetBytes(0xAABBCCDDu).Reverse(), .. new byte[20], // Unused, Reserved
        .. Trailer(0x0001018F, 0, 0, camType: 2),
    ];

    private static byte[] PathData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. SharedFields(30, 45, 65f, 2f, 6, 0u, 0f, 0f),
        .. BitConverter.GetBytes(0x11223344u).Reverse(), .. F(10f), .. F(1.5f), .. new byte[12], // PathId, TimeEnd, TimeDelay, Reserved
        .. Trailer(0x0001018F, 0, 0, camType: 3),
    ];

    private static byte[] StaticFollowData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. SharedFields(30, 45, 75f, 0.25f, 9, 0u, 0f, 0f),
        .. F(0.8f), .. new byte[20], // RubberBand, Reserved
        .. Trailer(0x0001018F, 0, 0, camType: 4),
    ];

    [Fact]
    public void Read_FollowCamera_ProducesFollowCameraAsset() =>
        Assert.IsType<FollowCameraAsset>(Read(FollowData()));

    [Fact]
    public void Read_FollowCamera_PopulatesSharedFields()
    {
        var asset = (FollowCameraAsset)Read(FollowData());

        Assert.Equal(new Vector3(1, 2, 3), asset.Position);
        Assert.Equal(new Vector3(0, 0, 1), asset.Forward);
        Assert.Equal(new Vector3(0, 1, 0), asset.Up);
        Assert.Equal(new Vector3(1, 0, 0), asset.Left);
        Assert.Equal(new Vector3(0, 0, 0), asset.ViewOffset);
        Assert.Equal(30, asset.OffsetStartFrames);
        Assert.Equal(45, asset.OffsetEndFrames);
        Assert.Equal(85f, asset.Fov);
        Assert.Equal(CameraTransitionType.None, asset.TransitionType);
        Assert.Equal(0x0001018Fu, asset.Physical.ValidFlags);
        Assert.Equal(CameraKind.Follow, asset.Kind);
    }

    [Fact]
    public void Read_FollowCamera_PopulatesTypeFields()
    {
        var asset = (FollowCameraAsset)Read(FollowData());

        Assert.Equal(0f, asset.Rotation);
        Assert.Equal(-2f, asset.Distance);
        Assert.Equal(1f, asset.Height);
        Assert.Equal(1f, asset.RubberBand);
        Assert.Equal(0f, asset.StartSpeed);
        Assert.Equal(0f, asset.EndSpeed);
    }

    [Fact]
    public void Read_ShoulderCamera_ProducesShoulderCameraAsset() =>
        Assert.IsType<ShoulderCameraAsset>(Read(ShoulderData()));

    [Fact]
    public void Read_ShoulderCamera_PopulatesTypeFields()
    {
        var asset = (ShoulderCameraAsset)Read(ShoulderData());

        Assert.Equal(4f, asset.Distance);
        Assert.Equal(2f, asset.Height);
        Assert.Equal(3f, asset.RealignSpeed);
        Assert.Equal(0.1f, asset.RealignDelay);
        Assert.Equal(CameraTransitionType.Linear, asset.TransitionType);
    }

    [Fact]
    public void Read_StaticCamera_ProducesStaticCameraAsset() =>
        Assert.IsType<StaticCameraAsset>(Read(StaticData()));

    [Fact]
    public void Read_StaticCamera_PopulatesTypeFields()
    {
        var asset = (StaticCameraAsset)Read(StaticData());

        Assert.Equal(0xAABBCCDDu, asset.Physical.Unused);
    }

    [Fact]
    public void Read_PathCamera_ProducesPathCameraAsset() =>
        Assert.IsType<PathCameraAsset>(Read(PathData()));

    [Fact]
    public void Read_PathCamera_PopulatesTypeFields()
    {
        var asset = (PathCameraAsset)Read(PathData());

        Assert.Equal(new AssetId(0x11223344), asset.PathId);
        Assert.Equal(10f, asset.TimeEnd);
        Assert.Equal(1.5f, asset.TimeDelay);
    }

    [Fact]
    public void Read_StaticFollowCamera_ProducesStaticFollowCameraAsset() =>
        Assert.IsType<StaticFollowCameraAsset>(Read(StaticFollowData()));

    [Fact]
    public void Read_StaticFollowCamera_PopulatesTypeFields()
    {
        var asset = (StaticFollowCameraAsset)Read(StaticFollowData());

        Assert.Equal(0.8f, asset.RubberBand);
    }

    [Fact]
    public void Read_Camera_UnknownCamType_ThrowsInvalidDataException()
    {
        byte[] data =
        [
            .. Prefix(0),
            .. SharedFields(30, 45, 60f, 0f, 0, 0u, 0f, 0f),
            .. new byte[24],
            .. Trailer(0, 0, 0, camType: 0xFF),
        ];

        Assert.Throws<InvalidDataException>(() => Read(data));
    }

    [Theory]
    [MemberData(nameof(EveryCameraKind))]
    public void Read_ThenWrite_Camera_ReproducesInputBytes(Func<byte, byte[]> data)
    {
        byte[] bytes =
        [
            .. data(2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];

        Assert.Equal(bytes, Write(Read(bytes)));
    }

    [Theory]
    [MemberData(nameof(EveryCameraKind))]
    public void Read_ThenWrite_CameraWithUnparsedTail_ReproducesInputBytes(Func<byte, byte[]> data)
    {
        byte[] bytes = [.. data(0), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(bytes, Write(Read(bytes)));
    }

    public static IEnumerable<object[]> EveryCameraKind()
    {
        yield return [(Func<byte, byte[]>)FollowData];
        yield return [(Func<byte, byte[]>)ShoulderData];
        yield return [(Func<byte, byte[]>)StaticData];
        yield return [(Func<byte, byte[]>)PathData];
        yield return [(Func<byte, byte[]>)StaticFollowData];
    }

    [Fact]
    public void Read_Camera_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = FollowData();

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<CameraAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void MarkerId1_SetDirectly_IsStoredIndependently()
    {
        var asset = new FollowCameraAsset { MarkerId1 = new AssetId(0xDEADBEEF) };

        Assert.Equal(new AssetId(0xDEADBEEF), asset.MarkerId1);
    }

    [Fact]
    public void CameraFlags_SetThroughPhysical_IsStoredIndependently()
    {
        var asset = new FollowCameraAsset();

        asset.Physical.CameraFlags = 0x80000000;

        Assert.Equal(0x80000000u, asset.Physical.CameraFlags);
    }
}

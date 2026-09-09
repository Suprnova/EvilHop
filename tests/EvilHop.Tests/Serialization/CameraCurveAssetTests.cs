using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class CameraCurveAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.CameraCurve;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= ROTUSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= ROTUSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x8D,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] F(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Fields(byte version, int cameraType, uint flags, int transitionType, float transitionTime, uint curveId1, uint curveId2, int numBeads) =>
    [
        version,
        0x00, 0x00, 0x00, // padding
        .. BitConverter.GetBytes(cameraType).Reverse(),
        .. BitConverter.GetBytes(flags).Reverse(),
        .. BitConverter.GetBytes(transitionType).Reverse(),
        .. F(transitionTime),
        .. BitConverter.GetBytes(curveId1).Reverse(),
        .. BitConverter.GetBytes(curveId2).Reverse(),
        .. BitConverter.GetBytes(numBeads).Reverse(),
    ];

    private static byte[] BeadBytes(float curveU1, float curveU2, float distanceAdjust, float pitchOffset, float targetRadius, float targetMarginAngle, float leadOffset, float yOffset, float nearWallAdjust, float farWallAdjust) =>
    [
        .. F(curveU1), .. F(curveU2), .. F(distanceAdjust), .. F(pitchOffset), .. F(targetRadius),
        .. F(targetMarginAngle), .. F(leadOffset), .. F(yOffset), .. F(nearWallAdjust), .. F(farWallAdjust),
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

    private static byte[] Data(byte linkCount = 0, int numBeads = 2) =>
    [
        .. Prefix(linkCount),
        .. Fields(version: 4, cameraType: 0, flags: 0, transitionType: 5, transitionTime: 0.5f, curveId1: 0x11111111, curveId2: 0x22222222, numBeads),
        .. Enumerable.Range(0, numBeads).SelectMany(i => BeadBytes(0.1f * i, 0.2f * i, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f)),
    ];

    [Fact]
    public void Read_CameraCurve_ProducesCameraCurveAsset() =>
        Assert.IsType<CameraCurveAsset>(Read(Data()));

    [Fact]
    public void Read_CameraCurve_PopulatesEveryField()
    {
        var asset = (CameraCurveAsset)Read(Data());

        Assert.Equal(4, asset.Physical.Version);
        Assert.Equal(CameraKind.Follow, asset.CameraType);
        Assert.Equal(CameraTransitionType.Linear, asset.TransitionType);
        Assert.Equal(0.5f, asset.TransitionTime);
        Assert.Equal(new AssetId(0x11111111), asset.CurveId1);
        Assert.Equal(new AssetId(0x22222222), asset.CurveId2);
        Assert.Equal(2, asset.Beads.Count);
    }

    [Fact]
    public void Read_CameraCurve_PopulatesEachBead()
    {
        var asset = (CameraCurveAsset)Read(Data(numBeads: 2));

        var bead = asset.Beads[1];
        Assert.Equal(0.1f, bead.CurveU1);
        Assert.Equal(0.2f, bead.CurveU2);
        Assert.Equal(1f, bead.DistanceAdjust);
        Assert.Equal(2f, bead.PitchOffset);
        Assert.Equal(3f, bead.TargetRadius);
        Assert.Equal(4f, bead.TargetMarginAngle);
        Assert.Equal(5f, bead.LeadOffset);
        Assert.Equal(6f, bead.YOffset);
        Assert.Equal(7f, bead.NearWallAdjust);
        Assert.Equal(8f, bead.FarWallAdjust);
    }

    [Fact]
    public void Read_ThenWrite_CameraCurve_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(linkCount: 2, numBeads: 3),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_CameraCurveWithNoBeads_ReproducesInputBytes()
    {
        byte[] data = Data(numBeads: 0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_CameraCurveWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_CameraCurve_UnderRatatouille_ReproducesInputBytes()
    {
        byte[] data = Data(numBeads: 1);
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_CameraCurve_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = Data();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<CameraCurveAsset>(asset);
    }

    [Fact]
    public void NumBeads_SetToMismatchedValue_IsStoredIndependently()
    {
        var asset = new CameraCurveAsset();

        asset.Physical.NumBeads = 7;

        Assert.Equal(7, asset.Physical.NumBeads);
    }

    [Fact]
    public void NumBeads_SetToMatchingValue_ClearsOverride()
    {
        var asset = new CameraCurveAsset();
        asset.Beads.Add(new CameraCurveBead());
        asset.Physical.NumBeads = 5;

        asset.Physical.NumBeads = 1;

        Assert.Equal(1, asset.Physical.NumBeads);
    }
}

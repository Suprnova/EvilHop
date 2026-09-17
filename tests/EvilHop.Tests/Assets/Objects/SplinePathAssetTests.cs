using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class SplinePathAssetTests
{
    private readonly SplinePathAsset _asset;

    public SplinePathAssetTests()
    {
        _asset = new SplinePathAsset();
    }

    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.SplinePath;
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

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0xE5,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
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

    private static byte[] U16(ushort value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Data(
        bool isExclusive = false, byte used = 0x4D, bool hasHover = false, byte pad0 = 0x53,
        ushort forwardCount = 0, ushort backwardCount = 0,
        float speed = 0f, float hoverTime = 0f, Vector3 hoverPoint = default,
        uint splineId = 0, uint forwardIdsPointer = 0, uint backwardIdsPointer = 0,
        byte linkCount = 0, uint[]? forwardIds = null, uint[]? backwardIds = null) =>
    [
        .. Prefix(linkCount),
        (byte)(isExclusive ? 1 : 0),
        used,
        (byte)(hasHover ? 1 : 0),
        pad0,
        .. U16(forwardCount),
        .. U16(backwardCount),
        .. F32(speed),
        .. F32(hoverTime),
        .. F32(hoverPoint.X),
        .. F32(hoverPoint.Y),
        .. F32(hoverPoint.Z),
        .. U32(splineId),
        .. U32(forwardIdsPointer),
        .. U32(backwardIdsPointer),
        .. (forwardIds is null ? [] : forwardIds.SelectMany(U32)),
        .. (backwardIds is null ? [] : backwardIds.SelectMany(U32)),
    ];

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-10.0f)]
    [InlineData(26.5f)]
    [InlineData(500.0f)]
    public void Speed_WhenAssigned_SetsValue(float value)
    {
        _asset.Speed = value;

        Assert.Equal(value, _asset.Speed);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    [InlineData(5.75f)]
    [InlineData(120.0f)]
    public void HoverTime_WhenAssigned_SetsValue(float value)
    {
        _asset.HoverTime = value;

        Assert.Equal(value, _asset.HoverTime);
    }

    [Fact]
    public void HoverPoint_WhenAssigned_SetsValue()
    {
        var point = new Vector3(10.5f, -20.25f, 30.75f);
        _asset.HoverPoint = point;

        Assert.Equal(point, _asset.HoverPoint);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(0x1B541CBCu)]
    [InlineData(0xFFFFFFFFu)]
    public void SplineId_WhenAssigned_SetsValue(uint value)
    {
        var id = new AssetId(value);
        _asset.SplineId = id;

        Assert.Equal(id, _asset.SplineId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsExclusive_WhenAssigned_SetsValue(bool value)
    {
        _asset.IsExclusive = value;

        Assert.Equal(value, _asset.IsExclusive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HasHover_WhenAssigned_SetsValue(bool value)
    {
        _asset.HasHover = value;

        Assert.Equal(value, _asset.HasHover);
    }

    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData((byte)0x4D)]
    [InlineData(byte.MaxValue)]
    public void Used_WhenAssigned_SetsValue(byte value)
    {
        _asset.Physical.Used = value;

        Assert.Equal(value, _asset.Physical.Used);
    }

    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData((byte)0x53)]
    [InlineData(byte.MaxValue)]
    public void Pad0_WhenAssigned_SetsValue(byte value)
    {
        _asset.Physical.Pad0 = value;

        Assert.Equal(value, _asset.Physical.Pad0);
    }

    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(0x80BB5B24u)]
    [InlineData(uint.MaxValue)]
    public void ForwardIdsPointer_WhenAssigned_SetsValue(uint value)
    {
        _asset.Physical.ForwardIdsPointer = value;

        Assert.Equal(value, _asset.Physical.ForwardIdsPointer);
    }

    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(0x88BB5B24u)]
    [InlineData(uint.MaxValue)]
    public void BackwardIdsPointer_WhenAssigned_SetsValue(uint value)
    {
        _asset.Physical.BackwardIdsPointer = value;

        Assert.Equal(value, _asset.Physical.BackwardIdsPointer);
    }

    [Fact]
    public void ForwardCount_WhenNotOverridden_DerivesFromForwardAssetIdsCount()
    {
        _asset.ForwardAssetIds.Add(new AssetId(1));
        _asset.ForwardAssetIds.Add(new AssetId(2));

        Assert.Equal((ushort)2, _asset.Physical.ForwardCount);
    }

    [Fact]
    public void ForwardCount_WhenOverridden_DivergesFromForwardAssetIdsCount()
    {
        _asset.ForwardAssetIds.Add(new AssetId(1));
        _asset.Physical.ForwardCount = 8;

        Assert.Single(_asset.ForwardAssetIds);
        Assert.Equal((ushort)8, _asset.Physical.ForwardCount);
    }

    [Fact]
    public void ForwardCount_SetToMatchForwardAssetIdsCount_KeepsDerivingAfterwards()
    {
        _asset.ForwardAssetIds.Add(new AssetId(1));
        _asset.Physical.ForwardCount = 1;

        _asset.ForwardAssetIds.Add(new AssetId(2));

        Assert.Equal((ushort)2, _asset.Physical.ForwardCount);
    }

    [Fact]
    public void BackwardCount_WhenNotOverridden_DerivesFromBackwardAssetIdsCount()
    {
        _asset.BackwardAssetIds.Add(new AssetId(1));

        Assert.Equal((ushort)1, _asset.Physical.BackwardCount);
    }

    [Fact]
    public void BackwardCount_WhenOverridden_DivergesFromBackwardAssetIdsCount()
    {
        _asset.BackwardAssetIds.Add(new AssetId(1));
        _asset.Physical.BackwardCount = 5;

        Assert.Single(_asset.BackwardAssetIds);
        Assert.Equal((ushort)5, _asset.Physical.BackwardCount);
    }

    [Fact]
    public void BackwardCount_SetToMatchBackwardAssetIdsCount_KeepsDerivingAfterwards()
    {
        _asset.BackwardAssetIds.Add(new AssetId(1));
        _asset.Physical.BackwardCount = 1;

        _asset.BackwardAssetIds.Add(new AssetId(2));

        Assert.Equal((ushort)2, _asset.Physical.BackwardCount);
    }

    [Fact]
    public void Read_SplinePath_ProducesSplinePathAsset() =>
        Assert.IsType<SplinePathAsset>(Read(Data()), exactMatch: false);

    [Fact]
    public void Read_SplinePath_PopulatesFields()
    {
        var hover = new Vector3(1.5f, 2.5f, 3.5f);
        byte[] data = Data(
            isExclusive: true,
            used: 0x12,
            hasHover: true,
            pad0: 0x34,
            speed: 25.0f,
            hoverTime: 4.0f,
            hoverPoint: hover,
            splineId: 0x99AABBCC,
            forwardIdsPointer: 0x80102030,
            backwardIdsPointer: 0x80405060);

        var asset = (SplinePathAsset)Read(data);

        Assert.True(asset.IsExclusive);
        Assert.Equal((byte)0x12, asset.Physical.Used);
        Assert.True(asset.HasHover);
        Assert.Equal((byte)0x34, asset.Physical.Pad0);
        Assert.Equal(25.0f, asset.Speed);
        Assert.Equal(4.0f, asset.HoverTime);
        Assert.Equal(hover, asset.HoverPoint);
        Assert.Equal(new AssetId(0x99AABBCC), asset.SplineId);
        Assert.Equal(0x80102030u, asset.Physical.ForwardIdsPointer);
        Assert.Equal(0x80405060u, asset.Physical.BackwardIdsPointer);
    }

    [Fact]
    public void Read_SplinePath_PopulatesForwardAndBackwardAssetIds()
    {
        byte[] data = Data(
            forwardCount: 2,
            backwardCount: 1,
            forwardIds: [0x11111111, 0x22222222],
            backwardIds: [0x33333333]);

        var asset = (SplinePathAsset)Read(data);

        Assert.Equal(2, asset.ForwardAssetIds.Count);
        Assert.Equal(new AssetId(0x11111111), asset.ForwardAssetIds[0]);
        Assert.Equal(new AssetId(0x22222222), asset.ForwardAssetIds[1]);

        Assert.Single(asset.BackwardAssetIds);
        Assert.Equal(new AssetId(0x33333333), asset.BackwardAssetIds[0]);
    }

    [Fact]
    public void Read_SplinePath_ReadsLinksAtTheDocumentedOffset()
    {
        byte[] data =
        [
            .. Data(forwardCount: 1, backwardCount: 1, forwardIds: [0x11111111], backwardIds: [0x22222222], linkCount: 1),
            .. LinkBytes(10, 20, 0xAABBCCDD),
        ];

        var asset = (SplinePathAsset)Read(data);

        Assert.Single(asset.Links);
        Assert.Equal(10, asset.Links[0].SourceEvent);
        Assert.Equal(20, asset.Links[0].DestinationEvent);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Links[0].DestinationAssetId);
    }

    [Fact]
    public void Read_SplinePath_ForwardAndBackwardCountKeepDerivingAfterCollectionsAreMutated()
    {
        byte[] data = Data(forwardCount: 1, backwardCount: 1, forwardIds: [0x11111111], backwardIds: [0x22222222]);

        var asset = (SplinePathAsset)Read(data);
        Assert.Equal((ushort)1, asset.Physical.ForwardCount);
        Assert.Equal((ushort)1, asset.Physical.BackwardCount);

        asset.ForwardAssetIds.Add(new AssetId(0x33333333));
        asset.BackwardAssetIds.Add(new AssetId(0x44444444));

        Assert.Equal((ushort)2, asset.Physical.ForwardCount);
        Assert.Equal((ushort)2, asset.Physical.BackwardCount);
    }

    [Fact]
    public void Read_ThenWrite_SplinePathWithNoPathsOrLinks_ReproducesInputBytes()
    {
        byte[] data = Data(
            isExclusive: false,
            used: 0x4D,
            hasHover: false,
            pad0: 0x53,
            speed: 15.0f,
            hoverTime: 2.0f,
            hoverPoint: new Vector3(1f, 2f, 3f),
            splineId: 0x12345678,
            forwardIdsPointer: 0x80000000,
            backwardIdsPointer: 0x80000000);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SplinePathWithPathsAndLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(
                isExclusive: true,
                used: 0x4D,
                hasHover: true,
                pad0: 0x53,
                forwardCount: 2,
                backwardCount: 1,
                speed: 30.0f,
                hoverTime: 5.0f,
                hoverPoint: new Vector3(10f, 20f, 30f),
                splineId: 0x1B541CBC,
                forwardIdsPointer: 0x80BB5B24,
                backwardIdsPointer: 0x88BB5B24,
                linkCount: 1,
                forwardIds: [0x1CA3208B, 0x1D0A095F],
                backwardIds: [0xEC7CD0D7]),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SplinePathWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Theory]
    [InlineData(GameVersion.BFBB)]
    [InlineData(GameVersion.TSSM)]
    [InlineData(GameVersion.ROTU)]
    [InlineData(GameVersion.Ratatouille)]
    [InlineData(GameVersion.N100F)]
    public void Read_SplinePath_UnderUnsupportedGames_DegradesToGenericBaseAsset(GameVersion game)
    {
        var profile = game switch
        {
            GameVersion.BFBB => BFBBSerializer.DefaultProfile,
            GameVersion.TSSM => TSSMSerializer.DefaultProfile,
            GameVersion.ROTU => ROTUSerializer.DefaultProfile,
            GameVersion.Ratatouille => RatatouilleSerializer.DefaultProfile,
            GameVersion.N100F => N100FSerializer.DefaultProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(game)),
        };

        var asset = Read(Data(), profile);

        Assert.IsNotType<SplinePathAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void Read_ThenWrite_RealIncrediblesExemplar_ReproducesInputBytes()
    {
        // incredibles/prototype_2004-07-19/GC/NTSC-U/US/FT/ft01.HIP, AHDR id=0x1C80D2EF
        byte[] data =
        [
            0x1C, 0x80, 0xD2, 0xEF, 0xE5, 0x00, 0x00, 0x1D,
            0x00, 0x4D, 0x00, 0x53, 0x00, 0x02, 0x00, 0x01,
            0x41, 0xF0, 0x00, 0x00, 0x40, 0xA0, 0x00, 0x00,
            0x50, 0x53, 0x5F, 0x31, 0x45, 0x4E, 0x49, 0x4C,
            0x48, 0x54, 0x41, 0x50, 0x3D, 0x8A, 0x5A, 0xD9,
            0x80, 0xBB, 0x5B, 0x24, 0x88, 0xBB, 0x5B, 0x24,
            0x1C, 0xA3, 0x20, 0x8B, 0x1D, 0x0A, 0x09, 0x5F,
            0xEC, 0x7C, 0xD0, 0xD7,
        ];

        var asset = (SplinePathAsset)Read(data);

        Assert.Equal(new AssetId(0x1C80D2EF), asset.Physical.BaseId);
        Assert.Equal(0xE5, asset.Physical.BaseType);
        Assert.False(asset.IsExclusive);
        Assert.False(asset.HasHover);
        Assert.Equal(2, asset.ForwardAssetIds.Count);
        Assert.Single(asset.BackwardAssetIds);
        Assert.Equal(30.0f, asset.Speed);
        Assert.Equal(5.0f, asset.HoverTime);
        Assert.Equal(new AssetId(0x3D8A5AD9), asset.SplineId);
        Assert.Equal(new AssetId(0x1CA3208B), asset.ForwardAssetIds[0]);
        Assert.Equal(new AssetId(0x1D0A095F), asset.ForwardAssetIds[1]);
        Assert.Equal(new AssetId(0xEC7CD0D7), asset.BackwardAssetIds[0]);

        Assert.Equal(data, Write(asset));
    }
}

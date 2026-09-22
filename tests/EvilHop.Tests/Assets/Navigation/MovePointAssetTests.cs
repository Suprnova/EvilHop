using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class MovePointAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(FormatProfile profile)
    {
        var serializer = profile.Game switch
        {
            GameVersion.BFBB => (Serializer)new BFBBSerializer(),
            _ => new N100FSerializer(),
        };
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.MovePoint;
        header.Debug = debug;

        return (header, debug);
    }

    private static MovePointAsset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        var (header, debug) = HeaderFor(profile);
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return (MovePointAsset)AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(MovePointAsset asset, FormatProfile? profile = null)
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
        0x0D,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] AssetIdBytes(uint value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    private static byte[] FixedFields(ushort weight, byte kind, byte bezierRole, byte flagsProps, ushort numPoints, float delay) =>
    [
        .. F32(1.0f), .. F32(2.0f), .. F32(3.0f), // Position
        (byte)(weight >> 8), (byte)weight,
        kind,
        bezierRole,
        flagsProps,
        0x00, // pad
        (byte)(numPoints >> 8), (byte)numPoints,
        .. F32(delay),
    ];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        .. AssetIdBytes(destinationAssetId),
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] N100FData(byte linkCount = 0, ushort numPoints = 0) =>
    [
        .. Prefix(linkCount),
        .. FixedFields(10000, 1, 0, 0, numPoints, -1.0f),
    ];

    private static byte[] BFBBData(byte linkCount = 0, ushort numPoints = 0) =>
    [
        .. Prefix(linkCount),
        .. FixedFields(10000, 0, 1, 2, numPoints, 5.0f),
        .. F32(3.0f),  // ZoneRadius
        .. F32(-1.0f), // ArenaRadius
    ];

    [Fact]
    public void Read_MovePoint_ProducesMovePointAsset() =>
        Assert.IsType<MovePointAsset>(Read(N100FData()));

    [Fact]
    public void Read_MovePoint_UnderN100F_PopulatesEveryFieldExceptRadii()
    {
        byte[] data =
        [
            .. N100FData(numPoints: 2),
            .. AssetIdBytes(0xAABBCCDD),
            .. AssetIdBytes(0x11223344),
        ];

        var asset = Read(data);

        Assert.Equal(new Vector3(1.0f, 2.0f, 3.0f), asset.Position);
        Assert.Equal(10000, asset.Weight);
        Assert.Equal(MovePointAsset.PathKind.Zone, asset.Kind);
        Assert.Equal(MovePointAsset.BezierRole.None, asset.Role);
        Assert.Equal(0, asset.Physical.FlagsProps);
        Assert.Equal(-1.0f, asset.Delay);
        Assert.Equal(default, asset.ZoneRadius);
        Assert.Equal(default, asset.ArenaRadius);
        Assert.Equal(2, asset.SiblingIds.Count);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.SiblingIds[0]);
        Assert.Equal(new AssetId(0x11223344), asset.SiblingIds[1]);
    }

    [Fact]
    public void Read_MovePoint_UnderBFBB_PopulatesRadii()
    {
        var asset = Read(BFBBData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(MovePointAsset.PathKind.Arena, asset.Kind);
        Assert.Equal(MovePointAsset.BezierRole.Curve, asset.Role);
        Assert.Equal(2, asset.Physical.FlagsProps);
        Assert.Equal(5.0f, asset.Delay);
        Assert.Equal(3.0f, asset.ZoneRadius);
        Assert.Equal(-1.0f, asset.ArenaRadius);
    }

    [Fact]
    public void Read_MovePoint_NumPointsKeepsDerivingAfterSiblingIdsAreMutated()
    {
        byte[] data = [.. N100FData(numPoints: 1), .. AssetIdBytes(0xAABBCCDD)];

        var asset = Read(data);
        Assert.Equal(1, asset.Physical.NumPoints);

        asset.SiblingIds.Add(AssetId.None);

        Assert.Equal(2, asset.Physical.NumPoints);
    }

    [Fact]
    public void Read_MovePoint_LinkCountKeepsDerivingAfterLinksAreMutated()
    {
        byte[] data = [.. N100FData(linkCount: 1), .. LinkBytes(0, 0, 0)];

        var asset = Read(data);
        Assert.Equal(1, asset.Physical.LinkCount);

        asset.Links.Add(new Link());

        Assert.Equal(2, asset.Physical.LinkCount);
    }

    [Fact]
    public void Read_ThenWrite_MovePointUnderN100F_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. N100FData(linkCount: 1, numPoints: 2),
            .. AssetIdBytes(0xAABBCCDD),
            .. AssetIdBytes(0x11223344),
            .. LinkBytes(1, 2, 0x55667788),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_MovePointUnderBFBB_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BFBBData(linkCount: 1, numPoints: 1),
            .. AssetIdBytes(0xAABBCCDD),
            .. LinkBytes(1, 2, 0x55667788),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_MovePointWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. N100FData(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void FlagsProps_SetThroughPhysical_IsStoredIndependently()
    {
        var asset = new MovePointAsset();

        asset.Physical.FlagsProps = 2;

        Assert.Equal(2, asset.Physical.FlagsProps);
    }
}

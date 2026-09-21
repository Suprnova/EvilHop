using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using static EvilHop.Assets.ConditionalAsset;

namespace EvilHop.Tests.Serialization;

public class ConditionalAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Conditional;
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
        0x1F,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] CondFields(uint evaluationAmount, uint variable, uint operation) =>
    [
        .. BitConverter.GetBytes(evaluationAmount).Reverse(),
        .. BitConverter.GetBytes(variable).Reverse(),
        .. BitConverter.GetBytes(operation).Reverse(),
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
        .. CondFields(10, 0x4329EFFD, 1),
        0x00, 0x00, 0x00, 0x00, // TargetId
    ];

    private static byte[] N100FData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. CondFields(10, 0x4329EFFD, 1),
    ];

    [Fact]
    public void Read_Conditional_ProducesConditionalAsset() =>
        Assert.IsType<ConditionalAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_Conditional_UnderBfbb_PopulatesEveryField()
    {
        byte[] data =
        [
            .. Prefix(0),
            .. CondFields(10, 0x4329EFFD, 1),
            0xAA, 0xBB, 0xCC, 0xDD, // TargetId
        ];

        var asset = (ConditionalAsset)Read(data, BFBBSerializer.DefaultProfile);

        Assert.Equal(10u, asset.EvaluationAmount);
        Assert.Equal(ConditionalVariable.CounterValue, asset.Variable);
        Assert.Equal(ConditionalOperation.GreaterThan, asset.Operation);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.TargetId);
    }

    [Fact]
    public void Read_Conditional_UnderN100F_LeavesTargetIdAtDefault()
    {
        var asset = (ConditionalAsset)Read(N100FData(), N100FSerializer.DefaultProfile);

        Assert.Equal(10u, asset.EvaluationAmount);
        Assert.Equal(ConditionalVariable.CounterValue, asset.Variable);
        Assert.Equal(ConditionalOperation.GreaterThan, asset.Operation);
        Assert.Equal(default, asset.TargetId);
    }

    [Fact]
    public void Read_Conditional_ReadsLinksAtTheDocumentedOffset()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];

        var asset = (ConditionalAsset)Read(data, BFBBSerializer.DefaultProfile);

        Assert.Equal(2, asset.Links.Count);
        Assert.Equal(1, asset.Links[0].SourceEvent);
        Assert.Equal(2, asset.Links[0].DestinationEvent);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Links[0].DestinationAssetId);
        Assert.Equal(3, asset.Links[1].SourceEvent);
    }

    [Fact]
    public void Read_ThenWrite_ConditionalUnderBfbb_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 1),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_ConditionalUnderN100F_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. N100FData(linkCount: 1),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_ConditionalWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }
}

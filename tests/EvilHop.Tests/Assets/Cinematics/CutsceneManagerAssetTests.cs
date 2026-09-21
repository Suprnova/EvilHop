using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class CutsceneManagerAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(Serializer serializer)
    {
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.CutsceneManager;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile profile, Serializer serializer)
    {
        var (header, debug) = HeaderFor(serializer);
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
        0x28,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] Fields(uint cutsceneAssetId, uint flags, float interpSpeed, uint? subtitlesId, float[] startTimes, float[] endTimes, uint[] emitIds) =>
    [
        .. BitConverter.GetBytes(cutsceneAssetId).Reverse(),
        .. BitConverter.GetBytes(flags).Reverse(),
        .. BitConverter.GetBytes(interpSpeed).Reverse(),
        .. subtitlesId is uint id ? BitConverter.GetBytes(id).Reverse() : [],
        .. startTimes.SelectMany(v => BitConverter.GetBytes(v).Reverse()),
        .. endTimes.SelectMany(v => BitConverter.GetBytes(v).Reverse()),
        .. emitIds.SelectMany(v => BitConverter.GetBytes(v).Reverse()),
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
        .. Fields(0xAABBCCDD, 0x1C, 2.5f, subtitlesId: null, new float[15], new float[15], new uint[15]),
    ];

    private static byte[] TssmData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. Fields(0xAABBCCDD, 0x1C, 2.5f, subtitlesId: 0x11112222, new float[15], new float[15], new uint[15]),
    ];

    [Fact]
    public void Read_CutsceneManager_ProducesCutsceneManagerAsset() =>
        Assert.IsType<CutsceneManagerAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile, new BFBBSerializer()));

    [Fact]
    public void Read_CutsceneManager_UnderBfbb_PopulatesCommonFieldsAndLeavesSubtitlesIdAtDefault()
    {
        var asset = (CutsceneManagerAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile, new BFBBSerializer());

        Assert.Equal(new AssetId(0xAABBCCDD), asset.CutsceneId);
        Assert.Equal(2.5f, asset.InterpSpeed);
        Assert.Equal(default, asset.SubtitlesId);
        Assert.Equal(15, asset.EmitterCues.Length);
    }

    [Fact]
    public void Read_CutsceneManager_UnderTSSM_PopulatesSubtitlesId()
    {
        var asset = (CutsceneManagerAsset)Read(TssmData(), TSSMSerializer.DefaultProfile, new TSSMSerializer());

        Assert.Equal(new AssetId(0x11112222), asset.SubtitlesId);
    }

    [Fact]
    public void Read_CutsceneManager_PopulatesEmitterCues()
    {
        byte[] data =
        [
            .. Prefix(0),
            .. Fields(0xAABBCCDD, 0x1C, 2.5f, subtitlesId: null,
                startTimes: [1.0f, .. new float[14]],
                endTimes: [2.0f, .. new float[14]],
                emitIds: [0x55555555u, .. new uint[14]]),
        ];

        var asset = (CutsceneManagerAsset)Read(data, BFBBSerializer.DefaultProfile, new BFBBSerializer());

        Assert.Equal(new AssetId(0x55555555), asset.EmitterCues[0].EmitterId);
        Assert.Equal(1.0f, asset.EmitterCues[0].StartTime);
        Assert.Equal(2.0f, asset.EmitterCues[0].EndTime);
        Assert.Equal(default, asset.EmitterCues[1].EmitterId);
    }

    [Fact]
    public void Read_ThenWrite_CutsceneManagerUnderBfbb_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile, new BFBBSerializer()), profile));
    }

    [Fact]
    public void Read_ThenWrite_CutsceneManagerUnderTSSM_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. TssmData(linkCount: 1),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile, new TSSMSerializer()), profile));
    }

    [Fact]
    public void Read_ThenWrite_CutsceneManagerWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile, new BFBBSerializer()), profile));
    }

    [Fact]
    public void Read_CutsceneManager_UnderUnsupportedGame_DegradesToGenericAsset()
    {
        byte[] data = BfbbData();

        var asset = Read(data, RatatouilleSerializer.DefaultProfile, new RatatouilleSerializer());

        Assert.IsNotType<CutsceneManagerAsset>(asset);
    }

    [Fact]
    public void EmitterCues_WhenAssignedWrongLength_Throws()
    {
        var asset = new CutsceneManagerAsset();

        Assert.Throws<ArgumentException>(() => asset.EmitterCues = []);
    }
}

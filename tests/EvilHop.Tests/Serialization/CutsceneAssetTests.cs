using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class CutsceneAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(Serializer serializer)
    {
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Cutscene;
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

    private static byte[] Header(uint numData, uint numTime, uint headerSize, uint visCount = 0, uint visSize = 0, uint breakCount = 0) =>
    [
        .. BitConverter.GetBytes(0x4E535443u).Reverse(), // Magic "NSTC"
        0x00, 0x00, 0x12, 0x34, // AssetID
        .. BitConverter.GetBytes(numData).Reverse(),
        .. BitConverter.GetBytes(numTime).Reverse(),
        0x00, 0x00, 0x00, 0x00, // MaxModel
        0x00, 0x00, 0x00, 0x00, // MaxBufEven
        0x00, 0x00, 0x00, 0x00, // MaxBufOdd
        .. BitConverter.GetBytes(headerSize).Reverse(),
        .. BitConverter.GetBytes(visCount).Reverse(),
        .. BitConverter.GetBytes(visSize).Reverse(),
        .. BitConverter.GetBytes(breakCount).Reverse(),
        0x00, 0x00, 0x00, 0x00, // pad
    ];

    private static byte[] FixedString(string value, int length) =>
    [
        .. System.Text.Encoding.ASCII.GetBytes(value),
        .. new byte[length - value.Length],
    ];

    private static byte[] DataEntry(uint dataType, uint assetId, uint chunkSize, uint fileOffset) =>
    [
        .. BitConverter.GetBytes(dataType).Reverse(),
        .. BitConverter.GetBytes(assetId).Reverse(),
        .. BitConverter.GetBytes(chunkSize).Reverse(),
        .. BitConverter.GetBytes(fileOffset).Reverse(),
    ];

    private static byte[] N100FData(byte[]? tail = null) =>
    [
        .. Header(numData: 1, numTime: 0, headerSize: 0x64),
        .. FixedString("boss_intro", 16),
        .. FixedString(string.Empty, 16),
        .. DataEntry(1, 0xAABBCCDD, 100, 2048),
        .. tail ?? [],
    ];

    [Fact]
    public void Read_Cutscene_ProducesCutsceneAsset() =>
        Assert.IsType<CutsceneAsset>(Read(N100FData(), N100FSerializer.DefaultProfile, new N100FSerializer()));

    [Fact]
    public void Read_Cutscene_UnderN100F_PopulatesHeaderAndData()
    {
        var asset = (CutsceneAsset)Read(N100FData(), N100FSerializer.DefaultProfile, new N100FSerializer());

        Assert.Equal(new AssetId(0x1234), asset.Physical.AssetId);
        Assert.Equal(1u, asset.Physical.NumData);
        Assert.Equal("boss_intro", asset.SoundLeft);
        Assert.Equal(string.Empty, asset.SoundRight);
        Assert.Empty(asset.AudioTracks);

        var entry = Assert.Single(asset.Data);
        Assert.Equal(CutsceneDataType.RWModel, entry.DataType);
        Assert.Equal(new AssetId(0xAABBCCDD), entry.AssetId);
        Assert.Equal(100u, entry.ChunkSize);
        Assert.Equal(2048u, entry.FileOffset);
    }

    [Fact]
    public void Read_ThenWrite_Cutscene_UnderN100F_ReproducesInputBytes()
    {
        byte[] data = N100FData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile, new N100FSerializer()), profile));
    }

    [Fact]
    public void Read_Cutscene_WithSoundNameFillingItsFieldExactly_ReadsWithoutATerminator()
    {
        // A BFBB PS2 German build was found on disk with "B3_Ending_German" - exactly 16 bytes,
        // leaving no room for a null terminator. strncpy-style truncation, not corruption.
        byte[] data =
        [
            .. Header(numData: 0, numTime: 0, headerSize: 0x50),
            .. System.Text.Encoding.ASCII.GetBytes("B3_Ending_German"),
            .. FixedString(string.Empty, 16),
        ];

        var asset = (CutsceneAsset)Read(data, N100FSerializer.DefaultProfile, new N100FSerializer());

        Assert.Equal("B3_Ending_German", asset.SoundLeft);
    }

    [Fact]
    public void Read_ThenWrite_CutsceneWithSoundNameFillingItsFieldExactly_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Header(numData: 0, numTime: 0, headerSize: 0x50),
            .. System.Text.Encoding.ASCII.GetBytes("B3_Ending_German"),
            .. FixedString(string.Empty, 16),
        ];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile, new N100FSerializer()), profile));
    }

    [Fact]
    public void Read_ThenWrite_CutsceneWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = N100FData([0xDE, 0xAD, 0xBE, 0xEF]);
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile, new N100FSerializer()), profile));
    }

    [Fact]
    public void Read_Cutscene_UnderUnsupportedGame_DegradesToGenericAsset()
    {
        byte[] data = N100FData();

        var asset = Read(data, RatatouilleSerializer.DefaultProfile, new RatatouilleSerializer());

        Assert.IsNotType<CutsceneAsset>(asset);
    }

    // TSSM/Incredibles use a 32-slot xCutsceneAudioTrack[] array instead of the plain SoundLeft/
    // SoundRight pair. Its per-track string length isn't a game constant - an Incredibles prototype
    // was found on disk still using TSSM's smaller layout - so the codec derives it from HeaderSize
    // rather than hardcoding it. This test uses an atypically short length (8) specifically to prove
    // that derivation, not a hardcoded 28/60.
    private const int TrackSoundLength = 8;
    private const int TrackSize = 8 + TrackSoundLength * 2;
    private const uint AudioTracksHeaderSize = 0x30 + TrackSize * 32 + 1 * 4; // + (NumTime=0 + 1) * 4

    private static byte[] AudioTrack(uint leftId, uint rightId, string left, string right) =>
    [
        .. BitConverter.GetBytes(leftId).Reverse(),
        .. BitConverter.GetBytes(rightId).Reverse(),
        .. FixedString(left, TrackSoundLength),
        .. FixedString(right, TrackSoundLength),
    ];

    private static byte[] TSSMData()
    {
        List<byte> bytes = [.. Header(numData: 0, numTime: 0, headerSize: AudioTracksHeaderSize)];
        bytes.AddRange(AudioTrack(0x11111111, 0x22222222, "left", "right"));
        for (int i = 1; i < 32; i++)
            bytes.AddRange(AudioTrack(0, 0, string.Empty, string.Empty));
        return [.. bytes];
    }

    [Fact]
    public void Read_Cutscene_UnderTSSM_DerivesTrackLengthAndPopulatesAudioTracks()
    {
        var asset = (CutsceneAsset)Read(TSSMData(), TSSMSerializer.DefaultProfile, new TSSMSerializer());

        Assert.Equal(32, asset.AudioTracks.Count);
        Assert.Equal(new AssetId(0x11111111), asset.AudioTracks[0].LeftSoundId);
        Assert.Equal(new AssetId(0x22222222), asset.AudioTracks[0].RightSoundId);
        Assert.Equal("left", asset.AudioTracks[0].LeftSound);
        Assert.Equal("right", asset.AudioTracks[0].RightSound);
        Assert.Equal(string.Empty, asset.AudioTracks[1].LeftSound);
    }

    [Fact]
    public void Read_ThenWrite_Cutscene_UnderTSSM_ReproducesInputBytes()
    {
        byte[] data = TSSMData();
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile, new TSSMSerializer()), profile));
    }

    [Fact]
    public void AssetId_WhenNotOverridden_FollowsId()
    {
        var asset = new CutsceneAsset { Id = new AssetId(0x1234) };

        Assert.Equal(new AssetId(0x1234), asset.Physical.AssetId);
    }

    [Fact]
    public void AssetId_WhenOverridden_DivergesFromId()
    {
        var asset = new CutsceneAsset { Id = new AssetId(0x1234) };
        asset.Physical.AssetId = new AssetId(0xDEAD);

        Assert.Equal(new AssetId(0xDEAD), asset.Physical.AssetId);
        Assert.Equal(new AssetId(0x1234), asset.Id);
    }

    [Fact]
    public void NumData_WhenNotOverridden_DerivesFromDataCount()
    {
        var asset = new CutsceneAsset();
        asset.Data.Add(new CutsceneDataEntry());
        asset.Data.Add(new CutsceneDataEntry());

        Assert.Equal(2u, asset.Physical.NumData);
    }
}

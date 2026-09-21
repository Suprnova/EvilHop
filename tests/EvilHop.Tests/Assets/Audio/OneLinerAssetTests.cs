using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using static EvilHop.Assets.OneLinerAsset;

namespace EvilHop.Tests.Serialization;

public class OneLinerAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.OneLiner;
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

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] I32(int value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] I16(short value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Entry(
        uint soundGroupId, float soundStartDelay, float timeSpan, float timeLastPlayed, uint numPlays,
        float delayBetweenPlays, float probability, float defaultDuration, float lastDuration, uint maxPlays,
        short eventType, bool playsInMusicChannel, OneLinerPlayerType playerType, int firstParam, float secondParam) =>
    [
        .. U32(soundGroupId), .. F32(soundStartDelay), .. F32(timeSpan), .. F32(timeLastPlayed),
        .. U32(numPlays), .. F32(delayBetweenPlays), .. F32(probability), .. F32(defaultDuration),
        .. F32(lastDuration), .. U32(maxPlays),
        .. U32(0), // m_soundGroupHandle
        .. U32(0), // m_pOLManager
        .. I16(eventType), .. I16((short)(playsInMusicChannel ? 1 : 0)),
        .. U32(0), // m_pData
        .. I32((int)playerType), .. I32(firstParam), .. F32(secondParam),
    ];

    private static byte[] Data(uint entryCount, params byte[][] entries) =>
    [
        .. U32(entryCount),
        .. entries.SelectMany(e => e),
    ];

    [Fact]
    public void Read_OneLiner_ProducesOneLinerAsset() =>
        Assert.IsType<OneLinerAsset>(Read(Data(0)));

    [Fact]
    public void Read_OneLiner_PopulatesEntries()
    {
        byte[] data = Data(2,
            Entry(0x03A934D6, 0f, 0f, 0f, 0u, 0f, 1f, 0f, 0f, 0u, 7, false, OneLinerPlayerType.Tester, 0, 0f),
            Entry(0x77AB0A67, 0f, 0f, 0f, 0u, 3f, 0.5f, 0f, 0f, 0u, 26, true, OneLinerPlayerType.Tester, 1, 3f));

        var asset = (OneLinerAsset)Read(data);

        Assert.Equal(2, asset.Entries.Count);

        var first = asset.Entries[0];
        Assert.Equal(new AssetId(0x03A934D6), first.SoundGroupId);
        Assert.Equal(1f, first.Probability);
        Assert.Equal((short)7, first.EventType);
        Assert.False(first.PlaysInMusicChannel);
        Assert.Equal(OneLinerPlayerType.Tester, first.PlayerType);
        Assert.Equal(0, first.FirstParam);
        Assert.Equal(0f, first.SecondParam);

        var second = asset.Entries[1];
        Assert.Equal(new AssetId(0x77AB0A67), second.SoundGroupId);
        Assert.Equal(3f, second.DelayBetweenPlays);
        Assert.Equal(0.5f, second.Probability);
        Assert.Equal((short)26, second.EventType);
        Assert.True(second.PlaysInMusicChannel);
        Assert.Equal(1, second.FirstParam);
        Assert.Equal(3f, second.SecondParam);
    }

    [Fact]
    public void Read_OneLiner_EntryCountKeepsDerivingAfterEntriesAreMutated()
    {
        byte[] data = Data(1, Entry(0x1, 0f, 0f, 0f, 0u, 0f, 0f, 0f, 0f, 0u, 0, false, OneLinerPlayerType.Always, 0, 0f));

        var asset = (OneLinerAsset)Read(data);
        Assert.Equal(1u, asset.Physical.EntryCount);

        asset.Entries.Add(new OneLinerEntry());

        Assert.Equal(2u, asset.Physical.EntryCount);
    }

    [Fact]
    public void Read_ThenWrite_OneLinerWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = Data(0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_OneLinerWithEntries_ReproducesInputBytes()
    {
        byte[] data = Data(2,
            Entry(0x03A934D6, 0f, 0f, 0f, 0u, 0f, 1f, 0f, 0f, 0u, 7, false, OneLinerPlayerType.Tester, 0, 0f),
            Entry(0x77AB0A67, 0f, 0f, 0f, 0u, 3f, 0.5f, 0f, 0f, 0u, 26, true, OneLinerPlayerType.Tester, 1, 3f));

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_OneLinerWithUnparsedTail_ReproducesInputBytes()
    {
        // Every real archive checked carries a 67-byte all-zero trailer after the entry array, of
        // unknown purpose; this is preserved as an unparsed tail rather than modelled.
        byte[] data = [.. Data(0), .. new byte[67]];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_OneLiner_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = Data(0);

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<OneLinerAsset>(asset);
    }
}

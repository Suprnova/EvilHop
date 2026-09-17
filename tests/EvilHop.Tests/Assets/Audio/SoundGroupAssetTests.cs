using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class SoundGroupAssetTests
{
    private readonly SoundGroupAsset _asset;

    public SoundGroupAssetTests()
    {
        _asset = new SoundGroupAsset();
    }

    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new TSSMSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.SoundGroup;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= TSSMSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= TSSMSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x4A,                   // BaseType
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

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] EntryBytes(uint soundId, float volume, float minPitch, float maxPitch) =>
    [
        .. U32(soundId),
        .. F32(volume),
        .. F32(minPitch),
        .. F32(maxPitch),
    ];

    private static byte[] Data(
        uint playedMask = 0, byte entryCount = 0, byte setBits = 0, sbyte maxPlays = 0, byte priority = 0,
        byte soundGroupFlags = 0, byte soundCategory = 0, byte playRule = 0, byte infoPad0 = 0,
        float innerRadius = 0f, float outerRadius = 0f, uint groupNamePointer = 0, byte linkCount = 0,
        byte[][]? entries = null) =>
    [
        .. Prefix(linkCount),
        .. U32(playedMask),
        entryCount,
        setBits,
        (byte)maxPlays,
        priority,
        soundGroupFlags,
        soundCategory,
        playRule,
        infoPad0,
        .. F32(innerRadius),
        .. F32(outerRadius),
        .. U32(groupNamePointer),
        .. (entries is null ? [] : entries.SelectMany(e => e)),
    ];

    [Theory]
    [InlineData(sbyte.MinValue)]
    [InlineData((sbyte)-1)]
    [InlineData((sbyte)0)]
    [InlineData((sbyte)15)]
    [InlineData(sbyte.MaxValue)]
    public void MaxPlays_WhenAssigned_SetsValue(sbyte value)
    {
        _asset.MaxPlays = value;

        Assert.Equal(value, _asset.MaxPlays);
    }

    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData((byte)1)]
    [InlineData((byte)87)]
    [InlineData((byte)128)]
    [InlineData(byte.MaxValue)]
    public void Priority_WhenAssigned_SetsValue(byte value)
    {
        _asset.Priority = value;

        Assert.Equal(value, _asset.Priority);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-5.0f)]
    [InlineData(12.75f)]
    [InlineData(1000.0f)]
    public void InnerRadius_WhenAssigned_SetsValue(float value)
    {
        _asset.InnerRadius = value;

        Assert.Equal(value, _asset.InnerRadius);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-5.0f)]
    [InlineData(48.5f)]
    [InlineData(2500.0f)]
    public void OuterRadius_WhenAssigned_SetsValue(float value)
    {
        _asset.OuterRadius = value;

        Assert.Equal(value, _asset.OuterRadius);
    }

    [Fact]
    public void Entries_WhenEntryAdded_ContainsSameEntryInstance()
    {
        var entry = new SoundGroupEntry();

        _asset.Entries.Add(entry);

        Assert.Same(entry, Assert.Single(_asset.Entries));
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(0x11223344u)]
    [InlineData(0xFFFFFFFFu)]
    public void SoundId_WhenAssigned_SetsValue(uint idValue)
    {
        var entry = new SoundGroupEntry();
        var id = new AssetId(idValue);

        entry.SoundId = id;

        Assert.Equal(id, entry.SoundId);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.75f)]
    [InlineData(1.0f)]
    public void Volume_WhenAssigned_SetsValue(float value)
    {
        var entry = new SoundGroupEntry();

        entry.Volume = value;

        Assert.Equal(value, entry.Volume);
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    public void MinPitchMultiplier_WhenAssigned_SetsValue(float value)
    {
        var entry = new SoundGroupEntry();

        entry.MinPitchMultiplier = value;

        Assert.Equal(value, entry.MinPitchMultiplier);
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    public void MaxPitchMultiplier_WhenAssigned_SetsValue(float value)
    {
        var entry = new SoundGroupEntry();

        entry.MaxPitchMultiplier = value;

        Assert.Equal(value, entry.MaxPitchMultiplier);
    }

    [Fact]
    public void IsEnvironmentalStream_WhenBit2Set_ReturnsTrue()
    {
        _asset.Physical.SoundGroupFlags = 0x02;

        Assert.True(_asset.IsEnvironmentalStream);
    }

    [Fact]
    public void IsEnvironmentalStream_WhenBit2NotSet_ReturnsFalse()
    {
        _asset.Physical.SoundGroupFlags = 0xFD;

        Assert.False(_asset.IsEnvironmentalStream);
    }

    [Fact]
    public void IsEnvironmentalStream_SetTrue_SetsBit2AndPreservesOtherBits()
    {
        _asset.Physical.SoundGroupFlags = 0x05;

        _asset.IsEnvironmentalStream = true;

        Assert.Equal((byte)0x07, _asset.Physical.SoundGroupFlags);
    }

    [Fact]
    public void IsEnvironmentalStream_SetFalse_ClearsBit2AndPreservesOtherBits()
    {
        _asset.Physical.SoundGroupFlags = 0x07;

        _asset.IsEnvironmentalStream = false;

        Assert.Equal((byte)0x05, _asset.Physical.SoundGroupFlags);
    }

    [Fact]
    public void EntryCount_WhenNotOverridden_DerivesFromEntriesCount()
    {
        _asset.Entries.Add(new SoundGroupEntry());
        _asset.Entries.Add(new SoundGroupEntry());

        Assert.Equal((byte)2, _asset.Physical.EntryCount);
    }

    [Fact]
    public void EntryCount_WhenOverridden_DivergesFromEntriesCount()
    {
        _asset.Entries.Add(new SoundGroupEntry());
        _asset.Physical.EntryCount = 7;

        Assert.Single(_asset.Entries);
        Assert.Equal((byte)7, _asset.Physical.EntryCount);
    }

    [Fact]
    public void EntryCount_SetToMatchEntriesCount_KeepsDerivingAfterwards()
    {
        _asset.Entries.Add(new SoundGroupEntry());
        _asset.Physical.EntryCount = 1;

        _asset.Entries.Add(new SoundGroupEntry());

        Assert.Equal((byte)2, _asset.Physical.EntryCount);
    }

    [Fact]
    public void EntryCount_WhenOverridden_StopsFollowingEntriesCount()
    {
        _asset.Entries.Add(new SoundGroupEntry());
        _asset.Physical.EntryCount = 10;

        _asset.Entries.Add(new SoundGroupEntry());

        Assert.Equal((byte)10, _asset.Physical.EntryCount);
    }

    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(0x12345678u)]
    [InlineData(uint.MaxValue)]
    public void PlayedMask_WhenAssigned_SetsValue(uint value)
    {
        _asset.Physical.PlayedMask = value;

        Assert.Equal(value, _asset.Physical.PlayedMask);
    }

    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData((byte)17)]
    [InlineData(byte.MaxValue)]
    public void SetBits_WhenAssigned_SetsValue(byte value)
    {
        _asset.Physical.SetBits = value;

        Assert.Equal(value, _asset.Physical.SetBits);
    }

    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData((byte)35)]
    [InlineData(byte.MaxValue)]
    public void SoundGroupFlags_WhenAssigned_SetsValue(byte value)
    {
        _asset.Physical.SoundGroupFlags = value;

        Assert.Equal(value, _asset.Physical.SoundGroupFlags);
    }

    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData((byte)3)]
    [InlineData(byte.MaxValue)]
    public void SoundCategory_WhenAssigned_SetsValue(byte value)
    {
        _asset.Physical.SoundCategory = value;

        Assert.Equal(value, _asset.Physical.SoundCategory);
    }

    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData((byte)2)]
    [InlineData(byte.MaxValue)]
    public void PlayRule_WhenAssigned_SetsValue(byte value)
    {
        _asset.Physical.PlayRule = value;

        Assert.Equal(value, _asset.Physical.PlayRule);
    }

    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData((byte)9)]
    [InlineData(byte.MaxValue)]
    public void InfoPad0_WhenAssigned_SetsValue(byte value)
    {
        _asset.Physical.InfoPad0 = value;

        Assert.Equal(value, _asset.Physical.InfoPad0);
    }

    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(0x80102030u)]
    [InlineData(uint.MaxValue)]
    public void GroupNamePointer_WhenAssigned_SetsValue(uint value)
    {
        _asset.Physical.GroupNamePointer = value;

        Assert.Equal(value, _asset.Physical.GroupNamePointer);
    }

    [Fact]
    public void Read_SoundGroup_ProducesSoundGroupAsset() =>
        Assert.IsType<SoundGroupAsset>(Read(Data()), exactMatch: false);

    [Fact]
    public void Read_SoundGroup_PopulatesFields()
    {
        byte[] data = Data(
            playedMask: 0xA1B2C3D4,
            entryCount: 0,
            setBits: 0x07,
            maxPlays: 3,
            priority: 128,
            soundGroupFlags: 0x02,
            soundCategory: 0x05,
            playRule: 0x01,
            infoPad0: 0x09,
            innerRadius: 15.0f,
            outerRadius: 60.0f,
            groupNamePointer: 0x80102030);

        var asset = (SoundGroupAsset)Read(data);

        Assert.Equal(0xA1B2C3D4u, asset.Physical.PlayedMask);
        Assert.Equal(0, asset.Physical.EntryCount);
        Assert.Equal(0x07, asset.Physical.SetBits);
        Assert.Equal((sbyte)3, asset.MaxPlays);
        Assert.Equal((byte)128, asset.Priority);
        Assert.Equal((byte)0x02, asset.Physical.SoundGroupFlags);
        Assert.True(asset.IsEnvironmentalStream);
        Assert.Equal((byte)0x05, asset.Physical.SoundCategory);
        Assert.Equal((byte)0x01, asset.Physical.PlayRule);
        Assert.Equal((byte)0x09, asset.Physical.InfoPad0);
        Assert.Equal(15.0f, asset.InnerRadius);
        Assert.Equal(60.0f, asset.OuterRadius);
        Assert.Equal(0x80102030u, asset.Physical.GroupNamePointer);
    }

    [Fact]
    public void Read_SoundGroup_PopulatesEntries()
    {
        byte[] entry1 = EntryBytes(0x11223344, 0.75f, 0.9f, 1.1f);
        byte[] entry2 = EntryBytes(0x55667788, 1.0f, 0.8f, 1.2f);
        byte[] data = Data(
            entryCount: 2,
            entries: [entry1, entry2]);

        var asset = (SoundGroupAsset)Read(data);

        Assert.Equal(2, asset.Entries.Count);
        Assert.Equal((byte)2, asset.Physical.EntryCount);

        var first = asset.Entries[0];
        Assert.Equal(new AssetId(0x11223344), first.SoundId);
        Assert.Equal(0.75f, first.Volume);
        Assert.Equal(0.9f, first.MinPitchMultiplier);
        Assert.Equal(1.1f, first.MaxPitchMultiplier);

        var second = asset.Entries[1];
        Assert.Equal(new AssetId(0x55667788), second.SoundId);
        Assert.Equal(1.0f, second.Volume);
        Assert.Equal(0.8f, second.MinPitchMultiplier);
        Assert.Equal(1.2f, second.MaxPitchMultiplier);
    }

    [Fact]
    public void Read_SoundGroup_ReadsLinksAtTheDocumentedOffset()
    {
        byte[] entry1 = EntryBytes(0x11223344, 1.0f, 1.0f, 1.0f);
        byte[] data =
        [
            .. Data(entryCount: 1, entries: [entry1], linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x55667788),
        ];

        var asset = (SoundGroupAsset)Read(data);

        Assert.Equal(2, asset.Links.Count);
        Assert.Equal(1, asset.Links[0].SourceEvent);
        Assert.Equal(2, asset.Links[0].DestinationEvent);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Links[0].DestinationAssetId);
        Assert.Equal(3, asset.Links[1].SourceEvent);
        Assert.Equal(4, asset.Links[1].DestinationEvent);
        Assert.Equal(new AssetId(0x55667788), asset.Links[1].DestinationAssetId);
    }

    [Fact]
    public void Read_SoundGroup_EntryCountKeepsDerivingAfterEntriesAreMutated()
    {
        byte[] entry1 = EntryBytes(0x11223344, 1.0f, 1.0f, 1.0f);
        byte[] data = Data(entryCount: 1, entries: [entry1]);

        var asset = (SoundGroupAsset)Read(data);
        Assert.Equal((byte)1, asset.Physical.EntryCount);

        asset.Entries.Add(new SoundGroupEntry());

        Assert.Equal((byte)2, asset.Physical.EntryCount);
    }

    [Fact]
    public void Read_ThenWrite_SoundGroupWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = Data(
            playedMask: 0,
            entryCount: 0,
            setBits: 1,
            maxPlays: 4,
            priority: 64,
            soundGroupFlags: 0x02,
            soundCategory: 3,
            playRule: 1,
            infoPad0: 0,
            innerRadius: 10.0f,
            outerRadius: 50.0f,
            groupNamePointer: 0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SoundGroupWithEntriesAndLinks_ReproducesInputBytes()
    {
        byte[] entry1 = EntryBytes(0x11223344, 0.5f, 0.95f, 1.05f);
        byte[] entry2 = EntryBytes(0x55667788, 1.0f, 0.85f, 1.15f);
        byte[] data =
        [
            .. Data(
                playedMask: 0x01,
                entryCount: 2,
                setBits: 0x03,
                maxPlays: -1,
                priority: 128,
                soundGroupFlags: 0x02,
                soundCategory: 2,
                playRule: 1,
                infoPad0: 0,
                innerRadius: 20.0f,
                outerRadius: 80.0f,
                groupNamePointer: 0x12345678,
                linkCount: 1,
                entries: [entry1, entry2]),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_SoundGroupWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_SoundGroup_UnderBFBB_DegradesToGenericBaseAsset()
    {
        byte[] data = Data();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<SoundGroupAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void Read_SoundGroup_UnderN100F_DegradesToGenericBaseAsset()
    {
        byte[] data = Data();

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<SoundGroupAsset>(asset, exactMatch: false);
    }

    [Theory]
    [InlineData(GameVersion.TSSM)]
    [InlineData(GameVersion.Incredibles)]
    [InlineData(GameVersion.ROTU)]
    [InlineData(GameVersion.Ratatouille)]
    public void Read_ThenWrite_UnderSupportedGame_ReproducesInputBytes(GameVersion game)
    {
        var profile = game switch
        {
            GameVersion.TSSM => TSSMSerializer.DefaultProfile,
            GameVersion.Incredibles => IncrediblesSerializer.DefaultProfile,
            GameVersion.ROTU => ROTUSerializer.DefaultProfile,
            GameVersion.Ratatouille => RatatouilleSerializer.DefaultProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(game)),
        };

        byte[] entry1 = EntryBytes(0x11223344, 0.75f, 0.9f, 1.1f);
        byte[] data =
        [
            .. Data(playedMask: 0, entryCount: 1, entries: [entry1], linkCount: 1),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];

        var asset = Read(data, profile);
        Assert.IsType<SoundGroupAsset>(asset, exactMatch: false);
        Assert.Equal(data, Write(asset, profile));
    }
}

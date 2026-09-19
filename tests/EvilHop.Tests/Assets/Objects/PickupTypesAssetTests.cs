using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class PickupTypesAssetTests
{
    private readonly PickupTypesAsset _asset;

    public PickupTypesAssetTests()
    {
        _asset = new PickupTypesAsset();
    }

    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(uint id = 0x12345678)
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.PickupTypes;
        header.Id = id;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null, uint id = 0x12345678)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        var (header, debug) = HeaderFor(id);
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

    private static byte[] Prefix(
        byte linkCount = 0,
        byte baseType = 0x00,
        ushort baseFlags = 0x001D,
        uint baseId = 0x12345678,
        bool bigEndian = true)
    {
        var idBytes = BitConverter.GetBytes(baseId);
        var flagBytes = BitConverter.GetBytes(baseFlags);
        if (bigEndian)
        {
            Array.Reverse(idBytes);
            Array.Reverse(flagBytes);
        }

        return
        [
            idBytes[0], idBytes[1], idBytes[2], idBytes[3],
            baseType,
            linkCount,
            flagBytes[0], flagBytes[1],
        ];
    }

    private static byte[] TableHeader(int version, int rowCount, bool bigEndian = true)
    {
        var versionBytes = BitConverter.GetBytes(version);
        var rowCountBytes = BitConverter.GetBytes(rowCount);
        if (bigEndian)
        {
            Array.Reverse(versionBytes);
            Array.Reverse(rowCountBytes);
        }

        return [.. versionBytes, .. rowCountBytes];
    }

    private static byte[] U32(uint value, bool bigEndian = true)
    {
        var bytes = BitConverter.GetBytes(value);
        if (bigEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    private static byte[] F32(float value, bool bigEndian = true)
    {
        var bytes = BitConverter.GetBytes(value);
        if (bigEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    private static byte[] Entry(
        uint typeHash,
        uint modelId,
        uint pulseModelId,
        float pulseTime,
        float pulseAddScale,
        float pulseMoveDown,
        Rgb colorMultiplier,
        uint color,
        uint flyingSoundGroupId,
        uint usedSoundGroupId,
        uint cantUseSoundGroupId,
        byte healthGain,
        byte powerGain,
        byte saveFlag,
        sbyte initialized,
        bool bigEndian = true) =>
    [
        .. U32(typeHash, bigEndian),
        .. U32(modelId, bigEndian),
        .. U32(pulseModelId, bigEndian),
        .. F32(pulseTime, bigEndian),
        .. F32(pulseAddScale, bigEndian),
        .. F32(pulseMoveDown, bigEndian),
        .. F32(colorMultiplier.R, bigEndian),
        .. F32(colorMultiplier.G, bigEndian),
        .. F32(colorMultiplier.B, bigEndian),
        .. U32(color, bigEndian),
        .. U32(flyingSoundGroupId, bigEndian),
        .. U32(usedSoundGroupId, bigEndian),
        .. U32(cantUseSoundGroupId, bigEndian),
        healthGain,
        powerGain,
        saveFlag,
        (byte)initialized,
    ];

    /// <summary>An entry as laid out before <see cref="FormatProfile.PickupTypesHasPulseFields"/> existed.</summary>
    private static byte[] ReducedEntry(
        uint typeHash,
        uint modelId,
        uint color,
        uint flyingSoundGroupId,
        uint usedSoundGroupId,
        uint cantUseSoundGroupId,
        byte healthGain,
        byte powerGain,
        byte saveFlag,
        sbyte initialized,
        bool bigEndian = true) =>
    [
        .. U32(typeHash, bigEndian),
        .. U32(modelId, bigEndian),
        .. U32(color, bigEndian),
        .. U32(flyingSoundGroupId, bigEndian),
        .. U32(usedSoundGroupId, bigEndian),
        .. U32(cantUseSoundGroupId, bigEndian),
        healthGain,
        powerGain,
        saveFlag,
        (byte)initialized,
    ];

    [Fact]
    public void Defaults_HaveExpectedValues()
    {
        Assert.Equal(AssetType.PickupTypes, _asset.Type);
        Assert.Equal(0x00, ((IPhysicalBaseAsset)_asset).BaseType);
        Assert.Equal(0, _asset.Version);
        Assert.Empty(_asset.Entries);
        Assert.Equal(0, _asset.Physical.RowCount);
    }

    [Fact]
    public void Read_PickupTypes_ProducesPickupTypesAsset()
    {
        byte[] data = [.. Prefix(), .. TableHeader(1, 0)];

        var asset = Read(data);

        Assert.IsType<PickupTypesAsset>(asset);
    }

    [Fact]
    public void Read_PickupTypes_PopulatesFieldsAndEntries()
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(2, 1),
            .. Entry(
                typeHash: 0x11111111,
                modelId: 0x22222222,
                pulseModelId: 0x33333333,
                pulseTime: 1.5f,
                pulseAddScale: 0.25f,
                pulseMoveDown: 0.5f,
                colorMultiplier: new Rgb(0.1f, 0.2f, 0.3f),
                color: 0xAABBCCDD,
                flyingSoundGroupId: 0x44444444,
                usedSoundGroupId: 0x55555555,
                cantUseSoundGroupId: 0x66666666,
                healthGain: 10,
                powerGain: 20,
                saveFlag: 1,
                initialized: -1),
        ];

        var asset = (PickupTypesAsset)Read(data);

        Assert.Equal(2, asset.Version);
        Assert.Single(asset.Entries);
        Assert.Equal(1, asset.Physical.RowCount);

        var entry = asset.Entries[0];
        Assert.Equal(new AssetId(0x11111111), entry.TypeHash);
        Assert.Equal(new AssetId(0x22222222), entry.ModelId);
        Assert.Equal(new AssetId(0x33333333), entry.PulseModelId);
        Assert.Equal(1.5f, entry.PulseTime);
        Assert.Equal(0.25f, entry.PulseAddScale);
        Assert.Equal(0.5f, entry.PulseMoveDown);
        Assert.Equal(new Rgb(0.1f, 0.2f, 0.3f), entry.ColorMultiplier);
        Assert.Equal(0xAABBCCDDu, entry.Color);
        Assert.Equal(new AssetId(0x44444444), entry.FlyingSoundGroupId);
        Assert.Equal(new AssetId(0x55555555), entry.UsedSoundGroupId);
        Assert.Equal(new AssetId(0x66666666), entry.CantUseSoundGroupId);
        Assert.Equal((byte)10, entry.HealthGain);
        Assert.Equal((byte)20, entry.PowerGain);
        Assert.Equal((byte)1, entry.SaveFlag);
        Assert.Equal((sbyte)(-1), entry.Initialized);
    }

    [Fact]
    public void Read_RowCountKeepsDerivingAfterEntriesAreMutated()
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(1, 1),
            .. Entry(1, 2, 3, 0, 0, 0, default, 0, 0, 0, 0, 0, 0, 0, 0),
        ];

        var asset = (PickupTypesAsset)Read(data);
        Assert.Equal(1, asset.Physical.RowCount);

        asset.Entries.Add(new PickupTypeEntry());
        Assert.Equal(2, asset.Physical.RowCount);
    }

    [Fact]
    public void RowCount_DisagreeingWithEntries_IsStoredIndependently()
    {
        var asset = new PickupTypesAsset();
        asset.Entries.Add(new PickupTypeEntry());

        asset.Physical.RowCount = 5;

        Assert.Equal(5, asset.Physical.RowCount);
        Assert.Single(asset.Entries);
    }

    [Fact]
    public void RowCount_MatchingEntries_DerivesFromEntries()
    {
        var asset = new PickupTypesAsset();
        asset.Entries.Add(new PickupTypeEntry());
        asset.Entries.Add(new PickupTypeEntry());

        asset.Physical.RowCount = 2;
        asset.Entries.Add(new PickupTypeEntry());

        Assert.Equal(3, asset.Physical.RowCount);
    }

    [Fact]
    public void Read_ThenWrite_PickupTypesWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(), .. TableHeader(1, 0)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_PickupTypesWithMultipleEntries_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(2, 2),
            .. Entry(0x11111111, 0x22222222, 0x33333333, 1.0f, 0.2f, 0.3f, new Rgb(1f, 0.5f, 0f), 0x12345678, 0x44444444, 0x55555555, 0x66666666, 5, 10, 2, 0),
            .. Entry(0x77777777, 0x88888888, 0x99999999, 2.0f, 0.4f, 0.6f, new Rgb(0f, 0.5f, 1f), 0x87654321, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC, 15, 25, 3, 1),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_PreservesUnparsedTail()
    {
        byte[] tail = [0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02];
        byte[] data = [.. Prefix(), .. TableHeader(1, 0), .. tail];

        var asset = Read(data);
        Assert.Equal(tail, asset.GetUnparsedTail().ToArray());

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Write_WithOverriddenRowCount_WritesOverriddenValue()
    {
        var asset = new PickupTypesAsset
        {
            Version = 3,
        };
        ((IPhysicalBaseAsset)asset).BaseId = new AssetId(0x12345678);
        ((IPhysicalBaseAsset)asset).BaseType = 0x00;
        ((IPhysicalBaseAsset)asset).LinkCount = 0;
        asset.BaseFlags = (BaseAssetFlags)0x001D;
        asset.Physical.RowCount = 42;

        byte[] written = Write(asset);
        byte[] expected = [.. Prefix(), .. TableHeader(3, 42)];

        Assert.Equal(expected, written);
    }

    [Theory]
    [InlineData(GameVersion.BFBB)]
    [InlineData(GameVersion.N100F)]
    [InlineData(GameVersion.TSSM)]
    public void Read_PickupTypes_UnderUnsupportedGames_DegradesToGenericBaseAsset(GameVersion game)
    {
        byte[] data = [.. Prefix(), .. TableHeader(1, 0)];
        var profile = game switch
        {
            GameVersion.BFBB => BFBBSerializer.DefaultProfile,
            GameVersion.N100F => N100FSerializer.DefaultProfile,
            GameVersion.TSSM => TSSMSerializer.DefaultProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(game)),
        };

        var asset = Read(data, profile);

        Assert.IsNotType<PickupTypesAsset>(asset);
        Assert.IsType<BaseAsset>(asset, exactMatch: false);
    }

    [Theory]
    [InlineData(GameVersion.Incredibles)]
    [InlineData(GameVersion.ROTU)]
    [InlineData(GameVersion.Ratatouille)]
    public void Read_ThenWrite_UnderSupportedGames_ReproducesInputBytes(GameVersion game)
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(1, 1),
            .. Entry(0x12345678, 0x23456789, 0x3456789A, 1.0f, 0.5f, 0.2f, new Rgb(0.2f, 0.4f, 0.6f), 0xFFEEDDCC, 0x456789AB, 0x56789ABC, 0x6789ABCD, 1, 2, 3, 0),
        ];
        var profile = game switch
        {
            GameVersion.Incredibles => IncrediblesSerializer.DefaultProfile,
            GameVersion.ROTU => ROTUSerializer.DefaultProfile,
            GameVersion.Ratatouille => RatatouilleSerializer.DefaultProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(game)),
        };

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LittleEndian_ReproducesInputBytes()
    {
        var profile = IncrediblesSerializer.DefaultProfile with { Platform = Platform.Xbox };
        byte[] data =
        [
            .. Prefix(bigEndian: false),
            .. TableHeader(1, 1, bigEndian: false),
            .. Entry(0x12345678, 0x23456789, 0x3456789A, 1.5f, 0.5f, -0.2f, new Rgb(0.5f, 0.5f, 0.5f), 0x11223344, 0x55667788, 0x99AABBCC, 0xDDEEFF00, 10, 20, 5, -2, bigEndian: false),
        ];

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_WithoutPulseFields_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(3, 2),
            .. ReducedEntry(0x11111111, 0x22222222, 0x33333333, 0x44444444, 0x55555555, 0x66666666, 10, 0, 0, 0),
            .. ReducedEntry(0x77777777, 0x88888888, 0, 0, 0x99999999, 0xAAAAAAAA, 0, 25, 1, -1),
        ];
        var profile = IncrediblesSerializer.DefaultProfile with { PickupTypesHasPulseFields = false };

        var asset = (PickupTypesAsset)Read(data, profile);

        Assert.Equal(2, asset.Entries.Count);
        Assert.Equal(new AssetId(0x11111111), asset.Entries[0].TypeHash);
        Assert.Equal(default, asset.Entries[0].PulseModelId);
        Assert.Equal(0f, asset.Entries[0].PulseTime);
        Assert.Equal(default, asset.Entries[0].ColorMultiplier);
        Assert.Equal((byte)10, asset.Entries[0].HealthGain);

        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_RealIncrediblesPrototypeExemplar_ReproducesInputBytesWithoutPulseFields()
    {
        // incredibles/prototype_2004-07-19/GC/NTSC-U/US/BOOT.HIP, AHDR id=0x7C1E7D4D
        byte[] data =
        [
            0x7C, 0x1E, 0x7D, 0x4D, 0x00, 0x00, 0x00, 0x1D,
            0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x09,
            .. ReducedEntry(0x66BFB77B, 0x72E0B667, 0x00000000, 0x00000000, 0x98EB34D6, 0xA1A1F173, 0x0A, 0x00, 0x00, 0x00),
            .. ReducedEntry(0x66BFB803, 0x72E0B667, 0x00000000, 0x00000000, 0x98EB34D6, 0xA1A1F173, 0x19, 0x00, 0x00, 0x00),
            .. ReducedEntry(0x66BFB987, 0xF6F0DF51, 0x00000000, 0x00000000, 0x98EB34D6, 0xA1A1F173, 0x32, 0x00, 0x00, 0x00),
            .. ReducedEntry(0x941AE421, 0xF6F0DF51, 0x00000000, 0x00000000, 0x4C02878E, 0xA1A1F173, 0x64, 0x00, 0x00, 0x00),
            .. ReducedEntry(0xD4F738C2, 0x3773BBB2, 0x00000000, 0x00000000, 0xAE0A24EA, 0xA1A1F173, 0x00, 0x0A, 0x00, 0x00),
            .. ReducedEntry(0xD4F7394A, 0x3773BBB2, 0x00000000, 0x00000000, 0xAE0A24EA, 0xA1A1F173, 0x00, 0x19, 0x00, 0x00),
            .. ReducedEntry(0xD4F73ACE, 0x3773BBB2, 0x00000000, 0x00000000, 0xAE0A24EA, 0xA1A1F173, 0x00, 0x32, 0x00, 0x00),
            .. ReducedEntry(0xFA820B76, 0x3773BBB2, 0x00000000, 0x00000000, 0xAE0A24EA, 0xA1A1F173, 0x00, 0x64, 0x00, 0x00),
            .. ReducedEntry(0x91338C47, 0x3773BBB2, 0x00000000, 0x00000000, 0xAE0A24EA, 0x00000000, 0x00, 0x00, 0x01, 0x00),
        ];
        var profile = IncrediblesSerializer.DefaultProfile with { PickupTypesHasPulseFields = false };

        var asset = (PickupTypesAsset)Read(data, profile);

        Assert.Equal(9, asset.Entries.Count);
        Assert.Equal(new AssetId(0x66BFB77B), asset.Entries[0].TypeHash);
        Assert.Equal((byte)10, asset.Entries[0].HealthGain);
        Assert.Equal((byte)25, asset.Entries[5].PowerGain);
        Assert.Equal((byte)1, asset.Entries[8].SaveFlag);
        Assert.Empty(asset.GetUnparsedTail().ToArray());

        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Write_GuardedNonPickupTypesAsset_DoesNotThrow()
    {
        var generic = new GenericBaseAsset(AssetType.PickupTypes);
        generic.Physical.BaseId = new AssetId(0x12345678);
        generic.Physical.BaseType = 0x00;
        generic.Physical.LinkCount = 0;
        generic.BaseFlags = (BaseAssetFlags)0x001D;
        generic.SetUnparsedTail([.. TableHeader(1, 0), 0x11, 0x22, 0x33, 0x44]);

        byte[] written = Write(generic);
        byte[] expected =
        [
            .. Prefix(),
            .. TableHeader(1, 0),
            0x11, 0x22, 0x33, 0x44,
        ];

        Assert.Equal(expected, written);
    }
}

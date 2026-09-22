using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class ThrowableTableAssetTests
{
    private readonly ThrowableTableAsset _asset;

    public ThrowableTableAssetTests()
    {
        _asset = new ThrowableTableAsset();
    }

    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(uint id = 0x12345678)
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.ThrowableTable;
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

    private static byte[] I32(int value, bool bigEndian = true)
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

    private static byte[] RowV3(
        uint modelId,
        uint type,
        uint shrapnelId,
        int damage,
        float damageRadius,
        bool bigEndian = true) =>
    [
        .. U32(modelId, bigEndian),
        .. U32(type, bigEndian),
        .. U32(shrapnelId, bigEndian),
        .. I32(damage, bigEndian),
        .. F32(damageRadius, bigEndian),
    ];

    private static byte[] RowV2(
        uint modelId,
        uint type,
        uint shrapnelId,
        int damage,
        bool bigEndian = true) =>
    [
        .. U32(modelId, bigEndian),
        .. U32(type, bigEndian),
        .. U32(shrapnelId, bigEndian),
        .. I32(damage, bigEndian),
    ];

    [Fact]
    public void Defaults_HaveExpectedValues()
    {
        Assert.Equal(AssetType.ThrowableTable, _asset.Type);
        Assert.Equal(0x00, ((Physical.IBaseAsset)_asset).BaseType);
        Assert.Equal(3, _asset.Version);
        Assert.Empty(_asset.Rows);
        Assert.Equal(0, _asset.Physical.RowCount);
    }

    [Fact]
    public void Read_ThrowableTable_ProducesThrowableTableAsset()
    {
        byte[] data = [.. Prefix(), .. TableHeader(3, 0)];

        var asset = Read(data);

        Assert.IsType<ThrowableTableAsset>(asset);
    }

    [Fact]
    public void Read_ThrowableTable_PopulatesFieldsAndRows_Version3()
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(3, 1),
            .. RowV3(
                modelId: 0x26489B89,
                type: 1,
                shrapnelId: 0x11223344,
                damage: 20,
                damageRadius: 2.5f),
        ];

        var asset = (ThrowableTableAsset)Read(data);

        Assert.Equal(3, asset.Version);
        Assert.Single(asset.Rows);
        Assert.Equal(1, asset.Physical.RowCount);

        var row = asset.Rows[0];
        Assert.Equal(new AssetId(0x26489B89), row.ModelId);
        Assert.Equal(1u, row.Type);
        Assert.Equal(new AssetId(0x11223344), row.ShrapnelId);
        Assert.Equal(20, row.Damage);
        Assert.Equal(2.5f, row.DamageRadius);
    }

    [Fact]
    public void Read_ThrowableTable_PopulatesFieldsAndRows_Version2()
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(2, 1),
            .. RowV2(
                modelId: 0x6105C4FB,
                type: 1,
                shrapnelId: 0x00000000,
                damage: 10),
        ];

        var asset = (ThrowableTableAsset)Read(data);

        Assert.Equal(2, asset.Version);
        Assert.Single(asset.Rows);
        Assert.Equal(1, asset.Physical.RowCount);

        var row = asset.Rows[0];
        Assert.Equal(new AssetId(0x6105C4FB), row.ModelId);
        Assert.Equal(1u, row.Type);
        Assert.Equal(AssetId.None, row.ShrapnelId);
        Assert.Equal(10, row.Damage);
        Assert.Equal(0.0f, row.DamageRadius);
    }

    [Fact]
    public void RowCount_KeepsDerivingAfterRowsAreMutated()
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(3, 1),
            .. RowV3(1, 2, 3, 4, 5.0f),
        ];

        var asset = (ThrowableTableAsset)Read(data);
        Assert.Equal(1, asset.Physical.RowCount);

        asset.Rows.Add(new ThrowableTableAsset.Entry());
        Assert.Equal(2, asset.Physical.RowCount);
    }

    [Fact]
    public void RowCount_DisagreeingWithRows_IsStoredIndependently()
    {
        var asset = new ThrowableTableAsset();
        asset.Rows.Add(new ThrowableTableAsset.Entry());

        asset.Physical.RowCount = 5;

        Assert.Equal(5, asset.Physical.RowCount);
        Assert.Single(asset.Rows);
    }

    [Fact]
    public void RowCount_MatchingRows_DerivesFromRows()
    {
        var asset = new ThrowableTableAsset();
        asset.Rows.Add(new ThrowableTableAsset.Entry());
        asset.Rows.Add(new ThrowableTableAsset.Entry());

        asset.Physical.RowCount = 2;
        asset.Rows.Add(new ThrowableTableAsset.Entry());

        Assert.Equal(3, asset.Physical.RowCount);
    }

    [Fact]
    public void Read_ThenWrite_ThrowableTableWithNoRows_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(), .. TableHeader(3, 0)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_Version3_WithMultipleRows_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(3, 2),
            .. RowV3(0x26489B89, 1, 0, 20, 2.0f),
            .. RowV3(0xBAA19B2F, 2, 0x12345678, 25, 5.0f),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_Version2_WithMultipleRows_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(2, 2),
            .. RowV2(0x6105C4FB, 1, 0, 10),
            .. RowV2(0xE300359F, 1, 0x12345678, 10),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_PreservesUnparsedTail()
    {
        byte[] tail = [0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02];
        byte[] data = [.. Prefix(), .. TableHeader(3, 0), .. tail];

        var asset = Read(data);
        Assert.Equal(tail, asset.GetUnparsedTail().ToArray());

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Write_WithOverriddenRowCount_WritesOverriddenValue()
    {
        var asset = new ThrowableTableAsset
        {
            Version = 3,
        };
        ((Physical.IBaseAsset)asset).BaseId = new AssetId(0x12345678);
        ((Physical.IBaseAsset)asset).BaseType = 0x00;
        ((Physical.IBaseAsset)asset).LinkCount = 0;
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
    [InlineData(GameVersion.Ratatouille)]
    public void Read_ThrowableTable_UnderUnsupportedGames_DegradesToGenericBaseAsset(GameVersion game)
    {
        byte[] data = [.. Prefix(), .. TableHeader(3, 0)];
        var profile = game switch
        {
            GameVersion.BFBB => BFBBSerializer.DefaultProfile,
            GameVersion.N100F => N100FSerializer.DefaultProfile,
            GameVersion.TSSM => TSSMSerializer.DefaultProfile,
            GameVersion.Ratatouille => RatatouilleSerializer.DefaultProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(game)),
        };

        var asset = Read(data, profile);

        Assert.IsNotType<ThrowableTableAsset>(asset);
        Assert.IsType<BaseAsset>(asset, exactMatch: false);
    }

    [Theory]
    [InlineData(GameVersion.Incredibles)]
    [InlineData(GameVersion.ROTU)]
    public void Read_ThenWrite_UnderSupportedGames_ReproducesInputBytes(GameVersion game)
    {
        byte[] data =
        [
            .. Prefix(),
            .. TableHeader(3, 1),
            .. RowV3(0x26489B89, 1, 0, 20, 2.0f),
        ];
        var profile = game switch
        {
            GameVersion.Incredibles => IncrediblesSerializer.DefaultProfile,
            GameVersion.ROTU => ROTUSerializer.DefaultProfile,
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
            .. TableHeader(3, 1, bigEndian: false),
            .. RowV3(0x26489B89, 1, 0x11223344, 20, 2.0f, bigEndian: false),
        ];

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Write_GuardedNonThrowableTableAsset_DoesNotThrow()
    {
        var generic = new GenericBaseAsset(AssetType.ThrowableTable);
        generic.Physical.BaseId = new AssetId(0x12345678);
        generic.Physical.BaseType = 0x00;
        generic.Physical.LinkCount = 0;
        generic.BaseFlags = (BaseAssetFlags)0x001D;
        generic.SetUnparsedTail([.. TableHeader(3, 0), 0x11, 0x22, 0x33, 0x44]);

        byte[] written = Write(generic);
        byte[] expected =
        [
            .. Prefix(),
            .. TableHeader(3, 0),
            0x11, 0x22, 0x33, 0x44,
        ];

        Assert.Equal(expected, written);
    }
}

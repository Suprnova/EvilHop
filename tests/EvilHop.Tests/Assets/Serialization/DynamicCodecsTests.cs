using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Assets.Serialization;

public class DynamicCodecsTests
{
    private const DynamicKind TestKind = (DynamicKind)0x7E57D1A0;
    private const short TestVersion = 3;

    /// <summary>A typed model for <see cref="TestKind"/> whose fields are a single <see cref="int"/>.</summary>
    private sealed class TestDynamicAsset : DynamicAsset
    {
        public override DynamicKind Kind => TestKind;

        public int Value { get; set; }

        public static void Read(TestDynamicAsset asset, EndianReader reader, FormatProfile _) => asset.Value = reader.ReadInt32();

        public static void Write(TestDynamicAsset asset, EndianWriter writer, FormatProfile _) => writer.Write(asset.Value);
    }

    static DynamicCodecsTests() =>
        DynamicCodecs.Register<TestDynamicAsset>(TestKind, TestDynamicAsset.Read, TestDynamicAsset.Write, new HashSet<(GameVersion, short)>
        {
            (GameVersion.BFBB, TestVersion),
        });

    private static DynamicAsset Read(byte[] data, FormatProfile profile)
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();
        header.Type = AssetType.Dynamic;
        header.Debug = debug;

        using var reader = new EndianReader(new MemoryStream(data), Endianness.Big);
        return (DynamicAsset)AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(DynamicAsset asset, FormatProfile profile)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Data(short version, byte[] body) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x00,                   // BaseType
        0x01,                   // LinkCount
        0x00, 0x1D,             // BaseFlags
        0x7E, 0x57, 0xD1, 0xA0, // Kind
        (byte)(version >> 8), (byte)version,
        0x00, 0x00,             // Handle
        .. body,
        0x00, 0x01, 0x00, 0x02, 0xAA, 0xBB, 0xCC, 0xDD, // link events and destination
        .. new byte[24],                                // link params and ids
    ];

    private static readonly byte[] ValueBody = [0x00, 0x00, 0x01, 0x2C];

    [Fact]
    public void Read_RegisteredLayout_ProducesTheTypedModel() =>
        Assert.IsType<TestDynamicAsset>(Read(Data(TestVersion, ValueBody), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_RegisteredLayout_ReadsItsFieldsAndLinks()
    {
        var asset = (TestDynamicAsset)Read(Data(TestVersion, ValueBody), BFBBSerializer.DefaultProfile);

        Assert.Equal(300, asset.Value);
        Assert.Equal(new AssetId(0xAABBCCDD), Assert.Single(asset.Links).DestinationAssetId);
    }

    [Fact]
    public void Read_UnregisteredVersion_ProducesGenericDynamicAsset() =>
        Assert.IsType<GenericDynamicAsset>(Read(Data(TestVersion + 1, ValueBody), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_UnregisteredGame_ProducesGenericDynamicAsset() =>
        Assert.IsType<GenericDynamicAsset>(Read(Data(TestVersion, ValueBody), TSSMSerializer.DefaultProfile));

    [Fact]
    public void Read_FieldsShorterThanTheBody_PreservesTheRestAsTheUnparsedTail()
    {
        var asset = Read(Data(TestVersion, [.. ValueBody, 0xDE, 0xAD]), BFBBSerializer.DefaultProfile);

        Assert.Equal([0xDE, 0xAD], asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Read_ThenWrite_RegisteredLayoutWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = Data(TestVersion, [.. ValueBody, 0xDE, 0xAD]);

        Assert.Equal(data, Write(Read(data, BFBBSerializer.DefaultProfile), BFBBSerializer.DefaultProfile));
    }
}

using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class AssetCodecsTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(AssetType type)
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Id = 0x1234;
        header.Type = type;
        header.Flags = AssetFlags.SourceVirtual;
        debug.Name = "test_asset";
        debug.Alignment = 16;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(AssetType type, byte[] data, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        var (header, debug) = HeaderFor(type);
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    [Theory]
    [InlineData(AssetType.Trigger)]
    [InlineData(AssetType.Player)]
    [InlineData(AssetType.SimpleObject)]
    public void Read_EntityShapedType_ProducesAnEntityAsset(AssetType type) =>
        Assert.IsType<EntityAsset>(Read(type, new byte[80]), exactMatch: false);

    [Theory]
    [InlineData(AssetType.Group)]
    [InlineData(AssetType.Timer)]
    [InlineData(AssetType.Counter)]
    public void Read_BaseShapedType_ProducesABaseAssetButNotAnEntity(AssetType type)
    {
        var asset = Read(type, new byte[16]);

        Assert.IsType<BaseAsset>(asset, exactMatch: false);
        Assert.IsNotType<EntityAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void Read_Dynamic_ProducesADynaAsset() =>
        Assert.IsType<DynaAsset>(Read(AssetType.Dynamic, new byte[16]), exactMatch: false);

    [Theory]
    [InlineData(AssetType.Texture)]
    [InlineData(AssetType.Model)]
    [InlineData(AssetType.BinkVideo)]
    public void Read_PayloadShapedType_ProducesAPayloadAsset(AssetType type) =>
        Assert.IsType<PayloadAsset>(Read(type, new byte[16]), exactMatch: false);

    [Theory]
    [InlineData(AssetType.Animation)]
    [InlineData(AssetType.Text)]
    [InlineData(AssetType.Wireframe)]
    public void Read_UnclassifiedType_PreservesEveryByte(AssetType type)
    {
        byte[] data = [0xDE, 0xAD, 0xBE, 0xEF];

        var asset = Read(type, data);

        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Read_PopulatesHeaderSourcedFields()
    {
        var asset = Read(AssetType.Texture, new byte[8]);

        Assert.Equal(0x1234u, asset.Id.Value);
        Assert.Equal(AssetType.Texture, asset.Type);
        Assert.Equal("test_asset", asset.Name);
        Assert.Equal(AssetFlags.SourceVirtual, asset.Physical.Flags);
        Assert.Equal(16, asset.Physical.Alignment);
    }

    [Fact]
    public void Read_BaseShapedType_ReadsTheFixedHeader()
    {
        byte[] data =
        [
            0x00, 0x00, 0x12, 0x34, // BaseId
            0x07,                   // BaseType
            0x02,                   // LinkCount
            0x00, 0x05,             // BaseFlags
        ];

        var asset = (BaseAsset)Read(AssetType.Group, data);

        Assert.Equal(0x1234u, asset.Physical.BaseId.Value);
        Assert.Equal(7, asset.Physical.BaseType);
        Assert.Equal(2, asset.Physical.LinkCount);
        Assert.Equal(BaseAssetFlags.Enabled | BaseAssetFlags.Valid, asset.BaseFlags);
    }

    [Theory]
    [InlineData(AssetType.Group, 16)]
    [InlineData(AssetType.Trigger, 96)]
    [InlineData(AssetType.Dynamic, 24)]
    [InlineData(AssetType.Texture, 16)]
    [InlineData(AssetType.Animation, 16)]
    public void Read_ThenWrite_ReproducesTheInputBytes(AssetType type, int length)
    {
        byte[] data = [.. Enumerable.Range(1, length).Select(i => (byte)i)];

        Assert.Equal(data, Write(Read(type, data)));
    }

    /// <summary>
    /// Bytes 12-15 of an entity are BFBB's padding. They are always zero in real archives, and the
    /// codec writes them as zero rather than preserving them, so a fixture that round-trips has to
    /// have them zeroed too.
    /// </summary>
    private static byte[] EntityBytes(int length, bool zeroPadding)
    {
        byte[] data = [.. Enumerable.Range(1, length).Select(i => (byte)i)];
        if (zeroPadding) Array.Clear(data, 12, 4);
        return data;
    }

    [Fact]
    public void Read_ThenWrite_EntityWithPadding_ReproducesTheInputBytes()
    {
        // BFBB's four padding bytes sit after the flag bytes, so a BFBB entity's prefix is four
        // bytes longer than every other game's. Round-tripping under BFBB's own profile is what
        // proves the switch is read consistently on both sides.
        byte[] data = EntityBytes(100, zeroPadding: true);
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(AssetType.Trigger, data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_EntityWithNonZeroPadding_NormalizesItToZero()
    {
        // The one region the asset layer deliberately does not preserve. It is zero in every
        // canonically-pathed BFBB archive checked, so regenerating it costs no real fidelity - but
        // it is a documented exception to "unknown bytes are carried through untouched", not an
        // oversight.
        byte[] data = EntityBytes(100, zeroPadding: false);
        var profile = BFBBSerializer.DefaultProfile;

        byte[] written = Write(Read(AssetType.Trigger, data, profile), profile);

        Assert.Equal<byte>([0, 0, 0, 0], written.AsSpan(12, 4).ToArray());
        Assert.Equal(data.AsSpan(16).ToArray(), written.AsSpan(16).ToArray());
    }

    [Fact]
    public void Read_EntityWithPadding_ShiftsTheFieldsPastIt()
    {
        byte[] data = [.. Enumerable.Range(1, 100).Select(i => (byte)i)];

        var withPadding = (EntityAsset)Read(AssetType.Trigger, data, BFBBSerializer.DefaultProfile);
        var without = (EntityAsset)Read(AssetType.Trigger, data, N100FSerializer.DefaultProfile);

        Assert.NotEqual(without.Physical.SurfaceId, withPadding.Physical.SurfaceId);
    }

    [Fact]
    public void Read_EntityShapedType_ReadsColorMultiplierInRgbaOrder()
    {
        byte[] data =
        [
            .. new byte[8],         // BaseAssetPrefix: BaseId, BaseType, LinkCount, BaseFlags
            0x00, 0x00, 0x00, 0x00, // EntityFlags/Subtype/PFlags/CollisionFlags
            0x00, 0x00, 0x00, 0x00, // SurfaceId
            .. new byte[36],        // Angle/Position/Scale
            0x3F, 0x80, 0x00, 0x00, // R = 1.0f
            0x40, 0x00, 0x00, 0x00, // G = 2.0f
            0x40, 0x40, 0x00, 0x00, // B = 3.0f
            0x40, 0x80, 0x00, 0x00, // A = 4.0f
            0x00, 0x00, 0x00, 0x00, // SeeThroughSpeed
            0x00, 0x00, 0x00, 0x00, // ModelId
            0x00, 0x00, 0x00, 0x00, // AnimListId
        ];

        var entity = (EntityAsset)Read(AssetType.Trigger, data, N100FSerializer.DefaultProfile);

        Assert.Equal(new RgbaColor(1.0f, 2.0f, 3.0f, 4.0f), entity.ColorMultiplier);
    }

    private sealed class StubAsset : Asset;

    [Fact]
    public void Write_GenericAssetCarryingAShapedType_FallsBackToItsOwnUnparsedTail()
    {
        // Mirrors what AssetSession.ParseOne does for an asset too short to parse under its shape's
        // codec (for example, zero bytes - not even enough for the fixed BaseAsset prefix): it
        // degrades to a bare GenericAsset while keeping the AssetType that failure happened under.
        // Dispatching on that AssetType alone would route it to WriteBase and throw on the cast.
        var asset = new GenericAsset { Type = AssetType.Group };
        asset.SetUnparsedTail([0xDE, 0xAD]);

        Assert.Equal<byte>([0xDE, 0xAD], Write(asset));
    }

    [Fact]
    public void Register_AfterAnAssetOfThatTypeWasAlreadyRead_WriteFallsBackToItsRuntimeShape()
    {
        // Chosen for its own sake, like Register_OverwritesTheSeededGenericHandler: this permanently
        // repoints the entry, so it must be a type nothing else in this class asserts on.
        const AssetType type = AssetType.SlideProperty;
        var asset = (BaseAsset)Read(type, new byte[16]);
        byte[] beforeRegistration = Write(asset);

        AssetCodecs.Register<StubAsset>(
            type,
            (data, header, debug, profile) => new StubAsset(),
            (stub, writer, profile) => writer.Write("stub"u8));

        // Handlers is one global table: registering a new codec for `type` retargets every asset
        // that already carries it, not just future reads. `asset` is still a GenericBaseAsset, not a
        // StubAsset, so an unchecked cast in the new handler would throw here.
        byte[] afterRegistration = Write(asset);

        Assert.Equal(beforeRegistration, afterRegistration);
    }

    [Fact]
    public void Register_OverwritesTheSeededGenericHandler()
    {
        // Chosen for its own sake: this test permanently replaces the entry, and the registry is
        // static, so it must be a type nothing else asserts on.
        const AssetType type = AssetType.LODTable;
        Assert.IsNotType<StubAsset>(Read(type, new byte[4]));

        AssetCodecs.Register<StubAsset>(
            type,
            (data, header, debug, profile) => new StubAsset(),
            (asset, writer, profile) => writer.Write("stub"u8));

        Assert.IsType<StubAsset>(Read(type, new byte[4]));
        Assert.Equal("stub"u8.ToArray(), Write(new StubAsset { Type = type }));
    }
}

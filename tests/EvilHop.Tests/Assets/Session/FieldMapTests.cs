using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Assets;

public class FieldMapTests
{
    private static readonly byte[] Tail = [0xC1, 0xC2, 0xC3];
    private static readonly byte[] PayloadData = [0x10, 0x20, 0x30, 0x40, 0x50];

    private const uint CounterId = 1;
    private const uint UntypedId = 2;
    private const uint ShapedId = 3;
    private const uint PayloadId = 4;
    private const uint EmptyId = 5;
    private const uint DynamicId = 6;

    private static readonly string[] BasePrefixPaths =
        ["Physical.BaseId", "Physical.BaseType", "Physical.LinkCount", "Physical.BaseFlags"];

    private static readonly string[] LinkPaths =
    [
        "Links[0].SourceEvent", "Links[0].DestinationEvent", "Links[0].DestinationAssetId",
        "Links[0].Params[0]", "Links[0].Params[1]", "Links[0].Params[2]", "Links[0].Params[3]",
        "Links[0].ParamWidgetAssetId", "Links[0].CheckAssetId",
    ];

    private readonly AssetView view;

    public FieldMapTests()
    {
        var archive = Archive.Create(Serializer.Create(GameVersion.TSSM, Platform.Xbox));
        using (var session = archive.OpenAssets())
        {
            var layer = session.CreateLayer(LayerType.Default);

            var counter = new CounterAsset { Id = new AssetId(CounterId), Name = "counter", InitialValue = 7 };
            counter.Links.Add(new Link { DestinationAssetId = new AssetId(UntypedId) });
            counter.SetUnparsedTail(Tail);
            layer.Add(counter);

            var untyped = new GenericAsset((AssetType)0x4E4F4E45) { Id = new AssetId(UntypedId) };
            untyped.SetUnparsedTail(Tail);
            layer.Add(untyped);

            var shaped = new GenericBaseAsset(AssetType.PickupTypes) { Id = new AssetId(ShapedId) };
            shaped.SetUnparsedTail(Tail);
            layer.Add(shaped);

            layer.Add(new GenericPayloadAsset(AssetType.Texture) { Id = new AssetId(PayloadId), Data = PayloadData });
            layer.Add(new EmptyAsset(AssetType.Texture) { Id = new AssetId(EmptyId) });

            var dynamic = new GenericDynamicAsset((DynamicKind)0x12345678) { Id = new AssetId(DynamicId) };
            dynamic.Links.Add(new Link());
            layer.Add(dynamic);
        }

        using var stream = new MemoryStream();
        archive.Save(stream);
        view = Archive.Load(new MemoryStream(stream.ToArray()), GameVersion.TSSM, Platform.Xbox).ReadAssets();
    }

    private Asset AssetOf(uint id) =>
        view.Layers.SelectMany(layer => layer.Assets).Single(asset => asset.Id.Value == id);

    private static (uint Offset, uint Size, FieldKind Kind)[] Shape(FieldMap map) =>
        [.. map.Fields.Select(field => (field.Offset, field.Size, field.Kind))];

    private static (string Path, FieldSurface Surface)[] Names(FieldMap map) =>
        [.. map.Fields.Select(field => (field.Path, field.Surface))];

    private static (string, FieldSurface)[] Logical(params string[] paths) =>
        [.. paths.Select(path => (path, FieldSurface.Logical))];

    private static (string, FieldSurface)[] Physical(params string[] paths) =>
        [.. paths.Select(path => (path, FieldSurface.Physical))];

    private static FieldEntry Tailing(uint offset, uint size) =>
        new(offset, size, FieldMap.UnparsedTail, FieldSurface.Unmodeled, FieldKind.Bytes);

    [Fact]
    public void Describe_TypedAsset_GivesOneFieldPerRead()
    {
        var map = view.Describe(AssetOf(CounterId));

        Assert.Equal(
        [
            (0, 4, FieldKind.AssetId), (4, 1, FieldKind.U8), (5, 1, FieldKind.U8), (6, 2, FieldKind.S16),
            (8, 2, FieldKind.S16), (10, 2, FieldKind.S16),
            (12, 2, FieldKind.S16), (14, 2, FieldKind.S16), (16, 4, FieldKind.AssetId),
            (20, 4, FieldKind.Bytes), (24, 4, FieldKind.Bytes), (28, 4, FieldKind.Bytes), (32, 4, FieldKind.Bytes),
            (36, 4, FieldKind.AssetId), (40, 4, FieldKind.AssetId),
            (44, 3, FieldKind.Bytes),
        ], Shape(map));
    }

    [Fact]
    public void Describe_TypedAsset_NamesItsPrefixLinksAndTail()
    {
        var map = view.Describe(AssetOf(CounterId));

        Assert.Equal(
            [
                .. Physical(BasePrefixPaths),
                .. Logical(FieldMap.Unnamed, FieldMap.Unnamed),
                .. Logical(LinkPaths),
                (FieldMap.UnparsedTail, FieldSurface.Unmodeled),
            ],
            Names(map));
    }

    [Fact]
    public void Describe_Dynamic_NamesThePrefixItPeeksAtAsTheReadAfter()
    {
        var map = view.Describe(AssetOf(DynamicId));

        Assert.Equal(
            [
                .. Physical(BasePrefixPaths),
                .. Physical("Physical.Kind", "Physical.Version", "Physical.Handle"),
                .. Logical(LinkPaths),
            ],
            Names(map));
    }

    [Fact]
    public void Describe_UntypedAssetWithNoShape_IsOneUnparsedTail()
    {
        var map = view.Describe(AssetOf(UntypedId));

        Assert.Equal([Tailing(0, 3)], map.Fields);
    }

    [Fact]
    public void Describe_UntypedAssetWithAShape_HoldsItsPrefixAndTail()
    {
        var map = view.Describe(AssetOf(ShapedId));

        Assert.Equal(
            [(0, 4, FieldKind.AssetId), (4, 1, FieldKind.U8), (5, 1, FieldKind.U8), (6, 2, FieldKind.S16), (8, 3, FieldKind.Bytes)],
            Shape(map));
        Assert.Equal([.. Physical(BasePrefixPaths), (FieldMap.UnparsedTail, FieldSurface.Unmodeled)], Names(map));
    }

    [Fact]
    public void Describe_Payload_IsOneUnmodeledField()
    {
        var map = view.Describe(AssetOf(PayloadId));

        Assert.Equal([new FieldEntry(0, 5, nameof(PayloadAsset.Data), FieldSurface.Unmodeled, FieldKind.Bytes)], map.Fields);
    }

    [Fact]
    public void Describe_ZeroSizeAsset_HasNoFields()
    {
        var map = view.Describe(AssetOf(EmptyId));

        Assert.Equal(0u, map.Length);
        Assert.Empty(map.Fields);
    }

    [Fact]
    public void Describe_SameAssetTwice_ReturnsTheSameMap() =>
        Assert.Same(view.Describe(AssetOf(CounterId)), view.Describe(AssetOf(CounterId)));

    [Fact]
    public void Describe_AssetNotReadByTheView_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => view.Describe(new CounterAsset()));

    [Fact]
    public void Describe_EveryAssetOfAnArchive_CoversItsBytesExactly()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "unofficial", "tssm.hip"));
        var fixture = Archive.Load(stream, GameVersion.TSSM, Platform.GameCube).ReadAssets();

        Assert.All(fixture.Layers.SelectMany(layer => layer.Assets), asset =>
        {
            var map = fixture.Describe(asset);
            uint end = 0;
            foreach (var field in map.Fields)
            {
                Assert.Equal(end, field.Offset);
                Assert.True(field.Size > 0);
                end = field.End;
            }
            Assert.Equal((uint)fixture.GetBytes(asset).Length, end);
            Assert.Equal(end, map.Length);
        });
    }

    [Theory]
    [InlineData(16u, 16u, 0u)]
    [InlineData(18u, 16u, 2u)]
    [InlineData(19u, 16u, 3u)]
    [InlineData(46u, 44u, 2u)]
    public void Locate_InsideTheAsset_GivesTheFieldAndOffsetInIt(uint offset, uint fieldOffset, uint offsetInField)
    {
        var map = view.Describe(AssetOf(CounterId));

        var location = map.Locate(offset);

        Assert.NotNull(location);
        Assert.Equal(fieldOffset, location.Value.Field.Offset);
        Assert.Equal(offsetInField, location.Value.OffsetInField);
    }

    [Theory]
    [InlineData(47u)]
    [InlineData(uint.MaxValue)]
    public void Locate_PastTheAssetsEnd_ReturnsNull(uint offset) =>
        Assert.Null(view.Describe(AssetOf(CounterId)).Locate(offset));

    [Fact]
    public void Recorder_BytesReadAgain_BelongToTheLastRead()
    {
        using var stream = new MemoryStream(new byte[8]);
        var recorder = new FieldRecorder(stream);
        using var reader = new EndianReader(stream, Endianness.Big) { Recorder = recorder };

        reader.ReadUInt32();
        stream.Position = 2;
        reader.ReadUInt16();
        reader.ReadUInt32();

        Assert.Equal(
            [
                new FieldEntry(0, 2, FieldMap.Unread, FieldSurface.Unmodeled, FieldKind.Bytes),
                new FieldEntry(2, 2, FieldMap.Unnamed, FieldSurface.Logical, FieldKind.U16),
                new FieldEntry(4, 4, FieldMap.Unnamed, FieldSurface.Logical, FieldKind.U32),
            ],
            recorder.Build(8).Fields);
    }

    [Fact]
    public void Recorder_BytesPassedOver_AreUnread()
    {
        using var stream = new MemoryStream(new byte[8]);
        var recorder = new FieldRecorder(stream);
        using var reader = new EndianReader(stream, Endianness.Big) { Recorder = recorder };

        reader.ReadByte();
        stream.Position = 4;
        reader.ReadSingle();

        Assert.Equal(
            [
                new FieldEntry(0, 1, FieldMap.Unnamed, FieldSurface.Logical, FieldKind.U8),
                new FieldEntry(1, 3, FieldMap.Unread, FieldSurface.Unmodeled, FieldKind.Bytes),
                new FieldEntry(4, 4, FieldMap.Unnamed, FieldSurface.Logical, FieldKind.F32),
            ],
            recorder.Build(8).Fields);
    }

    [Fact]
    public void Recorder_EntityPrefixWithPadding_NamesThePaddingForTheFieldBefore()
    {
        var profile = BFBBSerializer.DefaultProfile.OnGameCube();
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            EntityAssetPrefix.Write(new GenericEntityAsset(AssetType.Platform), writer, profile);
        stream.Position = 0;
        var recorder = new FieldRecorder(stream);
        using var reader = new EndianReader(stream, profile.Endianness) { Recorder = recorder };

        EntityAssetPrefix.Read(new GenericEntityAsset(AssetType.Platform), reader, profile);

        Assert.Equal(
            [
                (nameof(IEntity.EntityFlags), FieldSurface.Logical),
                .. Physical("Physical.Subtype", "Physical.PFlags", "Physical.CollisionFlags"),
                ("Physical.CollisionFlags.Padding", FieldSurface.Unmodeled),
                .. Physical("Physical.SurfaceId"),
                .. Logical("Angle", "Position", "Scale", "ColorMultiplier"),
                .. Physical("Physical.SeeThroughSpeed", "Physical.ModelId", "Physical.AnimListId"),
            ],
            Names(recorder.Build((uint)stream.Length)));
    }

    [Fact]
    public void Recorder_ReadsInAScope_AreNamedUnderIt()
    {
        using var stream = new MemoryStream(new byte[6]);
        var recorder = new FieldRecorder(stream);
        using var reader = new EndianReader(stream, Endianness.Big) { Recorder = recorder };

        using (reader.Field("Events", 3))
        {
            reader.ReadInt16("Physical.Delay");
            reader.ReadInt16();
        }
        reader.ReadInt16("After");

        Assert.Equal(
            [("Events[3].Physical.Delay", FieldSurface.Physical), ("Events[3].Unnamed", FieldSurface.Logical), ("After", FieldSurface.Logical)],
            Names(recorder.Build(6)));
    }

    [Fact]
    public void Recorder_PaddingBeforeAnyField_IsNamedPadding()
    {
        using var stream = new MemoryStream(new byte[2]);
        var recorder = new FieldRecorder(stream);
        using var reader = new EndianReader(stream, Endianness.Big) { Recorder = recorder };

        reader.SkipPadding(2);

        Assert.Equal([new FieldEntry(0, 2, "Padding", FieldSurface.Unmodeled, FieldKind.Bytes)], recorder.Build(2).Fields);
    }

    [Fact]
    public void Recorder_ReadInAnotherByteOrder_IsRecorded()
    {
        using var stream = new MemoryStream(new byte[6]);
        var recorder = new FieldRecorder(stream);
        using var reader = new EndianReader(stream, Endianness.Big) { Recorder = recorder };

        reader.ReadInt16();
        reader.WithEndianness(Endianness.Little).ReadAssetId();

        Assert.Equal([(0, 2, FieldKind.S16), (2, 4, FieldKind.AssetId)], Shape(recorder.Build(6)));
    }
}

using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Assets;

public class SupportStateTests
{
    public static TheoryData<Asset, SupportState> InstancesByClass => new()
    {
        { new CounterAsset(), SupportState.Typed },
        { new TalkBoxDynamicAsset(), SupportState.Typed },
        { new GenericPayloadAsset(AssetType.Texture), SupportState.Payload },
        { new GenericAsset(AssetType.Counter), SupportState.Untyped },
        { new GenericBaseAsset(AssetType.Counter), SupportState.Untyped },
        { new GenericEntityAsset(AssetType.SimpleObject), SupportState.Untyped },
        { new GenericDynamicAsset(DynamicKind.TalkBox), SupportState.Untyped },
        { new EmptyAsset(AssetType.Counter), SupportState.Untyped }
    };

    private static byte[] Saved(Action<Layer> fill)
    {
        var archive = Archive.Create(Serializer.Create(GameVersion.TSSM, Platform.Xbox));
        using (var session = archive.OpenAssets())
            fill(session.CreateLayer(LayerType.Default));

        using var stream = new MemoryStream();
        archive.Save(stream);
        return stream.ToArray();
    }

    private static Archive Load(byte[] bytes) => Archive.Load(new MemoryStream(bytes), GameVersion.TSSM, Platform.Xbox);

    [Theory]
    [MemberData(nameof(InstancesByClass))]
    public void Support_IsTheClasssState(Asset asset, SupportState expected) =>
        Assert.Equal(expected, asset.Support);

    [Fact]
    public void Support_TypedAssetReadBack_IsTyped()
    {
        var view = Load(Saved(layer => layer.Add(new CounterAsset { Id = new AssetId(1) }))).ReadAssets();

        Assert.Equal(SupportState.Typed, view.Layers[0].Assets.Single().Support);
    }

    [Fact]
    public void Support_AssetWhoseParseFailed_IsUntyped()
    {
        var archive = Load(Saved(layer => layer.Add(new CounterAsset { Id = new AssetId(1) })));
        archive.Roots.OfType<Dictionary>().Single().AssetTable.Headers.Single().Size = 4;

        var view = archive.ReadAssets();

        Assert.Single(view.Diagnostics);
        Assert.Equal(SupportState.Untyped, view.Layers[0].Assets.Single().Support);
    }

    [Fact]
    public void Support_TypedTypeReadUnderAGameItsCodecDoesntCover_IsUntyped()
    {
        var view = Load(Saved(layer => layer.Add(new PickupTypesAsset { Id = new AssetId(1) }))).ReadAssets();

        Assert.Equal(SupportState.Untyped, view.Layers[0].Assets.Single().Support);
        Assert.Empty(view.Diagnostics);
    }
}

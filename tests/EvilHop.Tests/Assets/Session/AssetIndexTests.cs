using EvilHop.Assets;
using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Assets;

public class AssetIndexTests
{
    private const string LongName = "a_name_longer_than_thirty_one_characters";

    private static Link LinkTo(Asset target) => new() { DestinationAssetId = target.Id };

    private static T Named<T>(T asset, string name) where T : Asset
    {
        asset.Name = name;
        asset.CalculateId();
        return asset;
    }

    /// <summary>
    /// Two layers: a Counter and a Group in the first, a Script and an Animation in the second. The
    /// Group holds the Counter and the Script; the Counter and the Script both link to the Group; the
    /// Script also links to an ID no asset has.
    /// </summary>
    private static (AssetIndex Index, CounterAsset Counter, GroupAsset Group, ScriptAsset Script, AnimationAsset Animation) Fixture()
    {
        var counter = Named(new CounterAsset(), "counter");
        var group = Named(new GroupAsset(), "group");
        var script = Named(new ScriptAsset(), "script");
        var animation = Named(new AnimationAsset(), "walk");
        animation.Name = "walk.anm";

        group.Items.Add(counter.Id);
        group.Items.Add(script.Id);
        counter.Links.Add(LinkTo(group));
        script.Links.Add(LinkTo(group));
        script.Links.Add(new Link { DestinationAssetId = new AssetId(0xDEAD) });

        var first = new Layer { Type = LayerType.Default };
        first.Add(counter);
        first.Add(group);
        var second = new Layer { Type = LayerType.Animation };
        second.Add(script);
        second.Add(animation);

        return (new AssetIndex([first, second]), counter, group, script, animation);
    }

    [Fact]
    public void Find_KnownId_ReturnsTheAsset()
    {
        var (index, counter, _, _, _) = Fixture();

        Assert.Same(counter, index.Find(counter.Id));
    }

    [Fact]
    public void Find_UnknownId_ReturnsNull() =>
        Assert.Null(Fixture().Index.Find(new AssetId(0xDEAD)));

    [Fact]
    public void FindByName_PlainName_FindsTheAssetThroughItsId()
    {
        var (index, counter, _, _, _) = Fixture();
        counter.Name = "renamed";

        Assert.Equal([counter], index.FindByName("counter"));
    }

    [Fact]
    public void FindByName_AnimationNameWithoutExtension_FindsItThroughTheTransform()
    {
        var (index, _, _, _, animation) = Fixture();

        Assert.Equal([animation], index.FindByName("walk"));
    }

    [Fact]
    public void FindByName_StoredName_FindsAnAssetWhoseIdIsNotItsHash()
    {
        var counter = new CounterAsset { Name = LongName[..31], Id = AssetId.FromName(LongName) };
        var layer = new Layer();
        layer.Add(counter);
        var index = new AssetIndex([layer]);

        Assert.Equal([counter], index.FindByName(LongName[..31]));
        Assert.Equal([counter], index.FindByName(LongName));
    }

    [Fact]
    public void FindByName_IgnoresLetterCase()
    {
        var (index, counter, _, _, _) = Fixture();

        Assert.Equal([counter], index.FindByName("COUNTER"));
    }

    [Fact]
    public void FindByName_NoMatch_IsEmpty() =>
        Assert.Empty(Fixture().Index.FindByName("nothing"));

    [Fact]
    public void FindByName_SeveralMatches_AreInLoadOrder()
    {
        var first = new CounterAsset { Name = "same", Id = new AssetId(1) };
        var second = new CounterAsset { Name = "same", Id = new AssetId(2) };
        var layers = new[] { new Layer(), new Layer() };
        layers[1].Add(second);
        layers[0].Add(first);

        Assert.Equal([first, second], new AssetIndex(layers).FindByName("same"));
    }

    [Fact]
    public void OfType_ReturnsEveryAssetOfTheTypeInLoadOrder()
    {
        var (index, counter, _, _, _) = Fixture();

        Assert.Equal([counter], index.OfType(AssetType.Counter));
        Assert.Empty(index.OfType(AssetType.Timer));
    }

    [Fact]
    public void NameOf_ReturnsTheStoredName()
    {
        var (index, _, _, _, animation) = Fixture();

        Assert.Equal("walk.anm", index.NameOf(animation.Id));
        Assert.Null(index.NameOf(new AssetId(0xDEAD)));
    }

    [Fact]
    public void ReferencesFrom_PlacesTheSourceAndNamesTheField()
    {
        var (index, _, group, script, _) = Fixture();

        Assert.Equal(
            [
                new AssetReference(script, 1, 0, "Links[0].DestinationAssetId", group.Id),
                new AssetReference(script, 1, 0, "Links[1].DestinationAssetId", new AssetId(0xDEAD))
            ],
            index.ReferencesFrom(script));
    }

    [Fact]
    public void ReferencesFrom_AssetOfAnotherIndex_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => Fixture().Index.ReferencesFrom(new CounterAsset()));

    [Fact]
    public void ReferencesTo_ListsEverySourceInLoadOrder()
    {
        var (index, counter, group, script, _) = Fixture();

        Assert.Equal([counter, script], index.ReferencesTo(group.Id).Select(reference => reference.Source));
        Assert.Equal(["Items[0]"], index.ReferencesTo(counter.Id).Select(reference => reference.Field));
    }

    [Fact]
    public void ReferencesTo_AnIdNoAssetHas_StillListsItsReferences()
    {
        var (index, _, _, script, _) = Fixture();

        Assert.Equal([script], index.ReferencesTo(new AssetId(0xDEAD)).Select(reference => reference.Source));
    }

    [Fact]
    public void Index_OfAView_ReadsTheArchivesAssets()
    {
        var archive = Archive.Create(Serializer.Create(GameVersion.TSSM, Platform.Xbox));
        using (var session = archive.OpenAssets())
        {
            var layer = session.CreateLayer(LayerType.Default);
            var target = Named(new CounterAsset(), "target");
            var source = Named(new CounterAsset(), "source");
            source.Links.Add(LinkTo(target));
            layer.Add(target);
            layer.Add(source);
        }
        using var saved = new MemoryStream();
        archive.Save(saved);

        var view = Archive.Load(new MemoryStream(saved.ToArray()), GameVersion.TSSM, Platform.Xbox).ReadAssets();

        Assert.Equal("source", view.Index.ReferencesTo(AssetId.FromName("target")).Single().Source.Name);
        Assert.Same(view.Index, view.Index);
    }
}

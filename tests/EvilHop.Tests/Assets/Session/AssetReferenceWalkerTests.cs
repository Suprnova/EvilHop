using EvilHop.Assets;
using EvilHop.Common;

namespace EvilHop.Tests.Assets;

public class AssetReferenceWalkerTests
{
    private static List<(string Field, uint Target)> Walk(Asset asset)
    {
        var found = new List<(string, uint)>();
        AssetReferenceWalker.Walk(asset, (field, target) => found.Add((field, target.Value)));
        return found;
    }

    private static Link LinkTo(uint destination, uint paramWidget = 0, uint check = 0) => new()
    {
        DestinationAssetId = new AssetId(destination),
        ParamWidgetAssetId = new AssetId(paramWidget),
        CheckAssetId = new AssetId(check)
    };

    [Fact]
    public void Walk_Links_GiveTheirDestinationParamWidgetAndCheckIds()
    {
        var counter = new CounterAsset { Id = new AssetId(1) };
        counter.Links.Add(LinkTo(10));
        counter.Links.Add(LinkTo(11, paramWidget: 12, check: 13));

        Assert.Equal(
            [
                ("Links[0].DestinationAssetId", 10u),
                ("Links[1].DestinationAssetId", 11u),
                ("Links[1].ParamWidgetAssetId", 12u),
                ("Links[1].CheckAssetId", 13u)
            ],
            Walk(counter));
    }

    [Fact]
    public void Walk_LinkParameter_CountsOnlyWhenItIsAnAssetId()
    {
        var counter = new CounterAsset();
        var link = LinkTo(10);
        link.Params = [new AssetIdParameter(new AssetId(20)), new RawParameter([0, 0, 0, 21]), new IntParameter(22), new FloatParameter(23)];
        counter.Links.Add(link);

        Assert.Equal([("Links[0].DestinationAssetId", 10u), ("Links[0].Params[0].Value", 20u)], Walk(counter));
    }

    [Fact]
    public void Walk_GroupItems_AreIndexed()
    {
        var group = new GroupAsset();
        group.Items.Add(new AssetId(5));
        group.Items.Add(new AssetId(6));

        Assert.Equal([("Items[0]", 5u), ("Items[1]", 6u)], Walk(group));
    }

    [Fact]
    public void Walk_ScriptEvents_GiveTheirWidgetAndParamWidgetIds()
    {
        var script = new ScriptAsset();
        script.Events.Add(new ScriptAsset.ScriptEvent { WidgetId = new AssetId(7), ParamWidgetId = new AssetId(8) });

        Assert.Equal([("Events[0].WidgetId", 7u), ("Events[0].ParamWidgetId", 8u)], Walk(script));
    }

    [Fact]
    public void Walk_NestedTypeBehindAnAbstractMember_IsFound()
    {
        var platform = new PlatformAsset { Motion = new EntityMotion.MovePoint { MovePointId = new AssetId(9) } };

        Assert.Contains(("Motion.MovePointId", 9u), Walk(platform));
    }

    [Fact]
    public void Walk_PhysicalOnlyIds_AreFoundUnderPhysical()
    {
        var simple = new SimpleObjectAsset();
        simple.Physical.ModelId = new AssetId(30);
        simple.Physical.SurfaceId = new AssetId(31);

        var found = Walk(simple);

        Assert.Contains(("Physical.ModelId", 30u), found);
        Assert.Contains(("Physical.SurfaceId", 31u), found);
        Assert.DoesNotContain(found, f => f.Field.Contains(nameof(IHasModel.ModelId), StringComparison.Ordinal) && !f.Field.StartsWith("Physical.", StringComparison.Ordinal));
    }

    [Fact]
    public void Walk_OwnIdStoredAgain_IsNotAReference()
    {
        var counter = new CounterAsset { Id = new AssetId(1) };
        counter.Physical.BaseId = new AssetId(2);

        Assert.Empty(Walk(counter));
    }

    [Fact]
    public void Walk_NoneIds_AreSkipped()
    {
        var group = new GroupAsset();
        group.Items.Add(AssetId.None);
        group.Links.Add(LinkTo(0));

        Assert.Empty(Walk(group));
    }

    [Fact]
    public void Walk_UntypedBaseAsset_GivesItsLinks()
    {
        var untyped = new GenericBaseAsset(AssetType.Dispatcher);
        untyped.Links.Add(LinkTo(40));

        Assert.Equal([("Links[0].DestinationAssetId", 40u)], Walk(untyped));
    }

    [Fact]
    public void Walk_AssetWithoutIdMembers_GivesNothing() =>
        Assert.Empty(Walk(new EmptyAsset(AssetType.Texture) { Id = new AssetId(1) }));
}

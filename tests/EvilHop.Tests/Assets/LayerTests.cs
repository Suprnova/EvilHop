using EvilHop.Assets;

namespace EvilHop.Tests.Assets;

public class LayerTests
{
    private sealed class TestAsset : Asset { }

    [Fact]
    public void Add_Asset_SetsLayer()
    {
        var layer = new Layer();
        var asset = new TestAsset();

        layer.Add(asset);

        Assert.Same(layer, asset.Layer);
    }

    [Fact]
    public void Add_Asset_AppearsInCollection()
    {
        var layer = new Layer();
        var asset = new TestAsset();

        layer.Add(asset);

        Assert.Contains(asset, layer.Assets);
    }

    [Fact]
    public void Add_AssetAlreadyInALayer_ThrowsInvalidOperationException()
    {
        var layer = new Layer();
        var otherLayer = new Layer();
        var asset = new TestAsset();
        layer.Add(asset);

        Assert.Throws<InvalidOperationException>(() => otherLayer.Add(asset));
    }

    [Fact]
    public void Remove_Asset_ClearsLayer()
    {
        var layer = new Layer();
        var asset = new TestAsset();
        layer.Add(asset);

        layer.Remove(asset);

        Assert.Null(asset.Layer);
    }

    [Fact]
    public void Remove_Asset_RemovesFromCollection()
    {
        var layer = new Layer();
        var asset = new TestAsset();
        layer.Add(asset);

        layer.Remove(asset);

        Assert.DoesNotContain(asset, layer.Assets);
    }

    [Fact]
    public void Remove_AssetNotInLayer_ReturnsFalse()
    {
        var layer = new Layer();
        var asset = new TestAsset();

        Assert.False(layer.Remove(asset));
    }
}

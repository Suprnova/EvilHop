using EvilHop.Blocks;
using EvilHop.Validation;

namespace EvilHop.Tests.Validation;

public class BlockPathTests
{
    private sealed class RootTestBlock : Block
    {
        protected internal override string Tag => "ROOT";
    }

    private sealed class ChildTestBlock : Block
    {
        protected internal override string Tag => "CHLD";
    }

    [Fact]
    public void For_RootBlock_ReturnsSingleSegment()
    {
        var root = new RootTestBlock();

        var path = BlockPath.For(root);

        Assert.Equal([("ROOT", 0)], path.Segments);
    }

    [Fact]
    public void For_NestedBlock_ReturnsSegmentPerAncestor()
    {
        var root = new RootTestBlock();
        var child = new ChildTestBlock();
        root.Children.Add(child);

        var path = BlockPath.For(child);

        Assert.Equal([("ROOT", 0), ("CHLD", 0)], path.Segments);
    }

    [Fact]
    public void For_SecondSiblingOfSameTag_AssignsIncrementingOrdinal()
    {
        var root = new RootTestBlock();
        var first = new ChildTestBlock();
        var second = new ChildTestBlock();
        root.Children.Add(first);
        root.Children.Add(second);

        var path = BlockPath.For(second);

        Assert.Equal([("ROOT", 0), ("CHLD", 1)], path.Segments);
    }

    [Fact]
    public void For_SiblingOfDifferentTag_DoesNotAffectOrdinal()
    {
        var root = new RootTestBlock();
        root.Children.Add(new RootTestBlock());
        var child = new ChildTestBlock();
        root.Children.Add(child);

        var path = BlockPath.For(child);

        Assert.Equal([("ROOT", 0), ("CHLD", 0)], path.Segments);
    }

    [Fact]
    public void ToString_FirstOfTagAtEverySegment_OmitsEveryOrdinal()
    {
        var path = new BlockPath([("PACK", 0), ("PLAT", 0)]);

        Assert.Equal("PACK/PLAT", path.ToString());
    }

    [Fact]
    public void ToString_LaterSiblingOfTag_AppendsOrdinal()
    {
        var path = new BlockPath([("LTOC", 0), ("LHDR", 3)]);

        Assert.Equal("LTOC/LHDR[3]", path.ToString());
    }

    [Fact]
    public void Equals_SameSegments_ReturnsTrue()
    {
        var first = new BlockPath([("PACK", 0), ("PLAT", 0)]);
        var second = new BlockPath([("PACK", 0), ("PLAT", 0)]);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_DifferentSegments_ReturnsFalse()
    {
        var first = new BlockPath([("PACK", 0), ("PLAT", 0)]);
        var second = new BlockPath([("PACK", 0), ("PVER", 0)]);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GetHashCode_SameSegments_AreEqual()
    {
        var first = new BlockPath([("LTOC", 0), ("LHDR", 3)]);
        var second = new BlockPath([("LTOC", 0), ("LHDR", 3)]);

        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}

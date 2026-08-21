using EvilHop.Blocks;
using static EvilHop.Tests.Blocks.BlockTests;

namespace EvilHop.Tests.Blocks;

public class BlockChildrenTests
{
    private readonly BlockChildren blockChildren;
    private readonly Block parent;

    public BlockChildrenTests()
    {
        parent = new TestBlock();
        blockChildren = new BlockChildren(parent);
    }

    [Fact]
    public void Add_ChildBlock_SetsParent()
    {
        var child = new TestBlock();

        blockChildren.Add(child);

        Assert.Same(parent, child.Parent);
    }

    [Fact]
    public void Add_ChildBlock_AppearsInCollection()
    {
        var child = new TestBlock();

        blockChildren.Add(child);

        Assert.Contains(blockChildren, c => ReferenceEquals(c, child));
    }

    [Fact]
    public void Add_ChildBlock_CountIncrements()
    {
        var child = new TestBlock();
        int initialCount = blockChildren.Count;

        blockChildren.Add(child);

        Assert.Equal(initialCount + 1, blockChildren.Count);
    }

    [Fact]
    public void Add_Self_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => blockChildren.Add(parent));
    }

    [Fact]
    public void Add_AncestralBlock_ThrowsInvalidOperationException()
    {
        var grandparent = new TestBlock();

        grandparent.Children.Add(parent);

        Assert.Throws<InvalidOperationException>(() => blockChildren.Add(grandparent));
    }

    [Fact]
    public void Add_MultiGenerationAncestralBlock_ThrowsInvalidOperationException()
    {
        var grandparent = new TestBlock();
        var middleParent = new TestBlock();

        middleParent.Children.Add(parent);
        grandparent.Children.Add(middleParent);

        Assert.Throws<InvalidOperationException>(() => blockChildren.Add(grandparent));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(17, 0)]
    [InlineData(17, 14)]
    [InlineData(38, 0)]
    [InlineData(38, 17)]
    public void Insert_ChildBlock_SetsParent(int initialCount, int index)
    {
        AddChildren(initialCount);
        var child = new TestBlock();

        blockChildren.Insert(index, child);

        Assert.Same(parent, child.Parent);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(17, 0)]
    [InlineData(17, 14)]
    [InlineData(38, 0)]
    [InlineData(38, 17)]
    public void Insert_ChildBlock_AppearsAtIndex(int initialCount, int index)
    {
        AddChildren(initialCount);
        var child = new TestBlock();

        blockChildren.Insert(index, child);

        Assert.Same(child, blockChildren[index]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(17, 0)]
    [InlineData(17, 14)]
    [InlineData(38, 0)]
    [InlineData(38, 17)]
    public void Insert_ChildBlock_CountIncrements(int initialCount, int index)
    {
        AddChildren(initialCount);
        var child = new TestBlock();
        int initialCountBeforeInsert = blockChildren.Count;

        blockChildren.Insert(index, child);

        Assert.Equal(initialCountBeforeInsert + 1, blockChildren.Count);
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(17, 7, 0)]
    [InlineData(17, 14, 14)]
    [InlineData(38, 17, 0)]
    [InlineData(38, 37, 17)]
    public void Insert_ChildBlock_ShiftsExistingChildren(int initialCount, int expectedShift, int index)
    {
        AddChildren(initialCount);
        var toShift = blockChildren[expectedShift];
        var child = new TestBlock();

        blockChildren.Insert(index, child);

        Assert.Same(toShift, blockChildren[expectedShift + 1]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(17, 0)]
    [InlineData(17, 14)]
    [InlineData(38, 0)]
    [InlineData(38, 17)]
    public void Insert_Self_ThrowsInvalidOperationException(int initialCount, int index)
    {
        AddChildren(initialCount);

        Assert.Throws<InvalidOperationException>(() => blockChildren.Insert(index, parent));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(17, 0)]
    [InlineData(17, 14)]
    [InlineData(38, 0)]
    [InlineData(38, 17)]
    public void Insert_AncestralBlock_ThrowsInvalidOperationException(int initialCount, int index)
    {
        AddChildren(initialCount);
        var grandparent = new TestBlock();

        grandparent.Children.Add(parent);

        Assert.Throws<InvalidOperationException>(() => blockChildren.Insert(index, grandparent));
    }

    [Fact]
    public void Remove_NonChildBlock_ReturnsFalse()
    {
        var nonChild = new TestBlock();

        bool result = blockChildren.Remove(nonChild);

        Assert.False(result);
    }

    [Fact]
    public void Remove_NonChildBlock_DoesNotAlterParent()
    {
        var nonChild = new TestBlock();

        blockChildren.Remove(nonChild);

        Assert.Null(nonChild.Parent);
    }

    [Fact]
    public void Remove_ChildBlock_ReturnsTrue()
    {
        var child = new TestBlock();
        blockChildren.Add(child);

        bool result = blockChildren.Remove(child);

        Assert.True(result);
    }

    [Fact]
    public void Remove_ChildBlock_SetsParentToNull()
    {
        var child = new TestBlock();
        blockChildren.Add(child);

        blockChildren.Remove(child);

        Assert.Null(child.Parent);
    }

    [Fact]
    public void Remove_ChildBlock_DisappearsFromCollection()
    {
        var child = new TestBlock();
        blockChildren.Add(child);

        blockChildren.Remove(child);

        Assert.DoesNotContain(blockChildren, c => ReferenceEquals(c, child));
    }

    [Fact]
    public void Remove_ChildBlock_DecrementsCount()
    {
        var child = new TestBlock();
        blockChildren.Add(child);
        int initialCount = blockChildren.Count;

        blockChildren.Remove(child);

        Assert.Equal(initialCount - 1, blockChildren.Count);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(17, 14)]
    [InlineData(38, 17)]
    public void RemoveAt_ChildBlock_DisappearsFromCollection(int initialCount, int index)
    {
        AddChildren(initialCount);
        var child = new TestBlock();
        blockChildren.Insert(index, child);

        blockChildren.RemoveAt(index);

        Assert.DoesNotContain(blockChildren, c => ReferenceEquals(c, child));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(17, 14)]
    [InlineData(38, 17)]
    public void RemoveAt_ChildBlock_SetsParentToNull(int initialCount, int index)
    {
        AddChildren(initialCount);
        var child = new TestBlock();
        blockChildren.Insert(index, child);

        blockChildren.RemoveAt(index);

        Assert.Null(child.Parent);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(17, 14)]
    [InlineData(38, 17)]
    public void RemoveAt_ChildBlock_DecrementsCount(int initialCount, int index)
    {
        AddChildren(initialCount);

        blockChildren.RemoveAt(index);

        Assert.Equal(initialCount - 1, blockChildren.Count);
    }

    [Fact]
    public void Contains_ChildBlock_ReturnsTrue()
    {
        var child = new TestBlock();
        blockChildren.Add(child);

        bool result = blockChildren.Contains(child);

        Assert.True(result);
    }

    [Fact]
    public void Contains_NonChildBlock_ReturnsFalse()
    {
        var nonChild = new TestBlock();

        bool result = blockChildren.Contains(nonChild);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(17)]
    [InlineData(38)]
    public void Clear_RemovesAllChildren(int initialCount)
    {
        AddChildren(initialCount);

        blockChildren.Clear();

        Assert.Empty(blockChildren);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(17)]
    [InlineData(38)]
    public void Clear_SetsAllParentsToNull(int initialCount)
    {
        AddChildren(initialCount);
        List<Block> orphans = [.. blockChildren];

        blockChildren.Clear();

        Assert.All(orphans, orphan => Assert.Null(orphan.Parent));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(17, 0)]
    [InlineData(17, 14)]
    [InlineData(38, 0)]
    [InlineData(38, 17)]
    public void IndexOf_ChildBlock_ReturnsIndex(int initialCount, int index)
    {
        AddChildren(initialCount);
        var child = new TestBlock();
        blockChildren.Insert(index, child);

        int result = blockChildren.IndexOf(child);

        Assert.Equal(index, result);
    }

    [Fact]
    public void IndexOf_NonChildBlock_ReturnsNegativeOne()
    {
        var nonChild = new TestBlock();

        int result = blockChildren.IndexOf(nonChild);

        Assert.Equal(-1, result);
    }

    private void AddChildren(int count)
    {
        for (int i = 0; i < count; i++)
        {
            blockChildren.Add(new TestBlock());
        }
    }
}

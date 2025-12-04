using EvilHop.Blocks;

namespace EvilHop.Tests.Blocks;

public class BlockTests
{
    class TestBlock : Block
    {
        protected override string Id => "TEST";

        // helper constructor for arranging non-AddChild() tests
        internal TestBlock(params Block[] children)
        {
            foreach (var child in children)
                this.AddChild(child);
        }
    }

    class OtherBlock : Block
    {
        protected override string Id => "OTHR";
    }

    [Fact]
    public void AddChild_Succeeds()
    {
        TestBlock test = new();
        Assert.Empty(test.Children);
        test.AddChild(new OtherBlock());
        Assert.Single(test.Children);
    }

    [Fact]
    public void GetChild_ChildExists_Succeeds()
    {
        TestBlock test = new(new TestBlock());
        Assert.NotNull(test.GetChild<TestBlock>());
    }

    [Fact]
    public void GetChild_ChildDoesNotExist_IsNull()
    {
        TestBlock test = new(new TestBlock());
        Assert.Null(test.GetChild<OtherBlock>());
    }

    [Fact]
    public void GetRequiredChild_ChildExists_Succeeds()
    {
        TestBlock test = new(new TestBlock());
        _ = test.GetRequiredChild<TestBlock>();
    }

    [Fact]
    public void GetRequiredChild_ChildDoesNotExist_Throws()
    {
        TestBlock test = new(new TestBlock());
        Assert.Throws<InvalidDataException>(test.GetRequiredChild<OtherBlock>);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GetVariableChild_HasExpectedCount(int count)
    {
        TestBlock test = new();
        for (int i = 0; i < count; i++)
            test.AddChild(new TestBlock());

        Assert.Equal(count, test.GetVariableChildren<TestBlock>().Count());
    }

    [Fact]
    public void SetChild_Null_RemovesChild()
    {
        var parent = new TestBlock(new TestBlock(), new OtherBlock());

        parent.SetChild<TestBlock>(null);

        Assert.Null(parent.GetChild<TestBlock>());
        Assert.DoesNotContain(parent.Children, c => c is TestBlock);
    }

    [Fact]
    public void SetChild_TargetMissing_ThrowsInvalidDataException()
    {
        var parent = new TestBlock(new OtherBlock());

        Assert.Throws<InvalidDataException>(() => parent.SetChild(new TestBlock()));
    }

    [Fact]
    public void GetChild_ReturnsFirstOccurrence_WhenMultipleExist()
    {
        var first = new TestBlock();
        var second = new TestBlock();
        var parent = new TestBlock(first, second);

        var found = parent.GetChild<TestBlock>();

        Assert.Same(first, found);
    }

    [Fact]
    public void GetVariableChildren_FiltersByType()
    {
        var parent = new TestBlock(new TestBlock(), new OtherBlock(), new TestBlock());

        var list = parent.GetVariableChildren<TestBlock>().ToList();

        Assert.Equal(2, list.Count);
        Assert.All(list, item => Assert.IsType<TestBlock>(item));
    }

    [Fact]
    public void AddChild_AllowsDuplicates()
    {
        var instance = new TestBlock();
        var parent = new TestBlock();

        parent.AddChild(instance);
        parent.AddChild(instance);

        Assert.Equal(2, parent.Children.Count(c => ReferenceEquals(c, instance)));
    }

}

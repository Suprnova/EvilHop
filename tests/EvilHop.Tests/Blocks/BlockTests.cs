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
    public void Block_AddChild_Succeeds()
    {
        TestBlock test = new();
        Assert.Empty(test.Children);
        test.AddChild(new OtherBlock());
        Assert.Single(test.Children);
    }

    [Fact]
    public void Block_GetChild_ChildExists_Succeeds()
    {
        TestBlock test = new(new TestBlock());
        Assert.NotNull(test.GetChild<TestBlock>());
    }

    [Fact]
    public void Block_GetChild_ChildDoesNotExist_IsNull()
    {
        TestBlock test = new(new TestBlock());
        Assert.Null(test.GetChild<OtherBlock>());
    }

    [Fact]
    public void Block_GetRequiredChild_ChildExists_Succeeds()
    {
        TestBlock test = new(new TestBlock());
        _ = test.GetRequiredChild<TestBlock>();
    }

    [Fact]
    public void Block_GetRequiredChild_ChildDoesNotExist_Throws()
    {
        TestBlock test = new(new TestBlock());
        Assert.Throws<InvalidDataException>(test.GetRequiredChild<OtherBlock>);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Block_GetVariableChild_HasExpectedCount(int count)
    {
        TestBlock test = new();
        for (int i = 0; i < count; i++)
            test.AddChild(new TestBlock());

        Assert.Equal(count, test.GetVariableChildren<TestBlock>().Count());
    }
}

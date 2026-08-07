using EvilHop.Blocks;

namespace EvilHop.Tests.Blocks;

public class BlockTests
{
    private readonly TestBlock testBlock;
    private readonly int managedFieldValue = 16614;

    public BlockTests()
    {
        testBlock = new TestBlock
        {
            ManagedField = managedFieldValue
        };
    }

    class TestBlock : Block
    {
        protected internal override string Tag => "TEST";

        public int ManagedField
        {
            get => GetManagedBlockField(ref field);
            set => SetManagedBlockField(ref field, value);
        }
    }

    [Fact]
    public void GetManagedBlockField_WhenUnlocked_ReturnsFieldValue()
    {
        testBlock.AreBlockFieldsLocked = false;

        int field = testBlock.ManagedField;

        Assert.Equal(managedFieldValue, field);
    }

    [Fact]
    public void GetManagedBlockField_WhenLocked_ReturnsFieldValue()
    {
        testBlock.AreBlockFieldsLocked = true;

        int field = testBlock.ManagedField;

        Assert.Equal(managedFieldValue, field);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(38)]
    public void SetManagedBlockField_WhenUnlocked_SetsFieldValue(int newValue)
    {
        testBlock.AreBlockFieldsLocked = false;

        testBlock.ManagedField = newValue;

        Assert.Equal(newValue, testBlock.ManagedField);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(38)]

    public void SetManagedBlockField_WhenLocked_ThrowsInvalidOperationException(int newValue)
    {
        testBlock.AreBlockFieldsLocked = true;

        var ex = Assert.Throws<InvalidOperationException>(() => testBlock.ManagedField = newValue);

        Assert.Contains("Cannot modify field of type Int32 on block of type TestBlock", ex.Message);
    }
}

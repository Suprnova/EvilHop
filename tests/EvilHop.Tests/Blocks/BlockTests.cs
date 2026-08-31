using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Tests.Blocks;

public class BlockTests
{
    private readonly TestBlock testBlock;
    private readonly int managedFieldValue = 16614;
    private readonly ValidationContext context = new(
        new FormatProfile(GameVersion.BFBB, Platform.GameCube, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: false));

    public BlockTests()
    {
        testBlock = new TestBlock
        {
            ManagedField = managedFieldValue
        };
    }

    internal class TestBlock : Block
    {
        protected internal override string Tag => "TEST";

        public int ManagedField
        {
            get => GetManagedBlockField(ref field);
            set => SetManagedBlockField(ref field, value);
        }
    }

    internal class OtherTestBlock : Block
    {
        protected internal override string Tag => "OTHR";
    }

    private sealed class RecordingBlock : Block
    {
        protected internal override string Tag => "RECD";

        public List<ValidationIssue> OwnIssues { get; } = [];
        public ValidationContext? ReceivedContext { get; private set; }

        public override IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            ReceivedContext = context;
            return OwnIssues.Concat(base.Validate(context));
        }
    }

    private static ValidationIssue MakeIssue(string ruleId) =>
        new(ruleId, Severity.Warning, new ArchiveSite(), "test issue");

    [Fact]
    public void GetChildren_NoChildren_ReturnsEmpty()
    {
        var children = testBlock.GetChildren<TestBlock>();

        Assert.Empty(children);
    }

    [Fact]
    public void GetChildren_NoMatchingChildren_ReturnsEmpty()
    {
        testBlock.Children.Add(new OtherTestBlock());

        var children = testBlock.GetChildren<TestBlock>();

        Assert.Empty(children);
    }

    [Fact]
    public void GetChildren_OneMatchingChild_ReturnsChild()
    {
        var child = new TestBlock();
        testBlock.Children.Add(child);

        var children = testBlock.GetChildren<TestBlock>();

        Assert.Same(child, Assert.Single(children));
    }

    [Fact]
    public void GetChildren_MultipleMatchingChildren_ReturnsAllMatchingChildren()
    {
        var first = new TestBlock();
        var second = new TestBlock();
        testBlock.Children.Add(first);
        testBlock.Children.Add(second);

        var children = testBlock.GetChildren<TestBlock>();

        Assert.Collection(children,
            c => Assert.Same(first, c),
            c => Assert.Same(second, c));
    }

    [Fact]
    public void GetChildren_MixedChildTypes_ExcludesNonMatchingChildren()
    {
        var match = new TestBlock();
        testBlock.Children.Add(match);
        testBlock.Children.Add(new OtherTestBlock());

        var children = testBlock.GetChildren<TestBlock>();

        Assert.Same(match, Assert.Single(children));
    }

    [Fact]
    public void SetChild_NoExistingChild_AddsValueToChildren()
    {
        var value = new TestBlock();

        testBlock.SetChild(value);

        Assert.Same(value, Assert.Single(testBlock.GetChildren<TestBlock>()));
    }

    [Fact]
    public void SetChild_NoExistingChild_SetsValueParent()
    {
        var value = new TestBlock();

        testBlock.SetChild(value);

        Assert.Same(testBlock, value.Parent);
    }

    [Fact]
    public void SetChild_NoExistingChild_ReturnsNull()
    {
        var value = new TestBlock();

        var previous = testBlock.SetChild(value);

        Assert.Null(previous);
    }

    [Fact]
    public void SetChild_OtherTypedChildPresent_LeavesOtherTypedChildUnaffected()
    {
        var other = new OtherTestBlock();
        testBlock.Children.Add(other);
        var value = new TestBlock();

        testBlock.SetChild(value);

        Assert.Contains(testBlock.Children, c => ReferenceEquals(c, other));
    }

    [Fact]
    public void SetChild_ExistingChild_RemovesExistingChild()
    {
        var existing = new TestBlock();
        testBlock.Children.Add(existing);
        var value = new TestBlock();

        testBlock.SetChild(value);

        Assert.DoesNotContain(testBlock.Children, c => ReferenceEquals(c, existing));
    }

    [Fact]
    public void SetChild_ExistingChild_AddsValueToChildren()
    {
        var existing = new TestBlock();
        testBlock.Children.Add(existing);
        var value = new TestBlock();

        testBlock.SetChild(value);

        Assert.Same(value, Assert.Single(testBlock.GetChildren<TestBlock>()));
    }

    [Fact]
    public void SetChild_ExistingChild_SetsExistingChildParentToNull()
    {
        var existing = new TestBlock();
        testBlock.Children.Add(existing);
        var value = new TestBlock();

        testBlock.SetChild(value);

        Assert.Null(existing.Parent);
    }

    [Fact]
    public void SetChild_ExistingChild_ReturnsExistingChild()
    {
        var existing = new TestBlock();
        testBlock.Children.Add(existing);
        var value = new TestBlock();

        var previous = testBlock.SetChild(value);

        Assert.Same(existing, previous);
    }

    [Fact]
    public void SetChild_NullValueNoExistingChild_ChildrenUnchanged()
    {
        testBlock.SetChild<TestBlock>(null);

        Assert.Empty(testBlock.Children);
    }

    [Fact]
    public void SetChild_NullValueNoExistingChild_ReturnsNull()
    {
        var previous = testBlock.SetChild<TestBlock>(null);

        Assert.Null(previous);
    }

    [Fact]
    public void SetChild_NullValueWithExistingChild_RemovesExistingChild()
    {
        var existing = new TestBlock();
        testBlock.Children.Add(existing);

        testBlock.SetChild<TestBlock>(null);

        Assert.Empty(testBlock.Children);
    }

    [Fact]
    public void SetChild_NullValueWithExistingChild_SetsExistingChildParentToNull()
    {
        var existing = new TestBlock();
        testBlock.Children.Add(existing);

        testBlock.SetChild<TestBlock>(null);

        Assert.Null(existing.Parent);
    }

    [Fact]
    public void SetChild_NullValueWithExistingChild_ReturnsExistingChild()
    {
        var existing = new TestBlock();
        testBlock.Children.Add(existing);

        var previous = testBlock.SetChild<TestBlock>(null);

        Assert.Same(existing, previous);
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

        Assert.Contains("Cannot modify block of type TestBlock because its fields are locked", ex.Message);
    }

    [Fact]
    public void SetChild_WhenLocked_ThrowsInvalidOperationException()
    {
        testBlock.AreBlockFieldsLocked = true;
        var value = new TestBlock();

        var ex = Assert.Throws<InvalidOperationException>(() => testBlock.SetChild(value));

        Assert.Contains("Cannot modify block of type TestBlock because its fields are locked", ex.Message);
    }

    [Fact]
    public void SetChild_WhenLocked_LeavesChildrenUnchanged()
    {
        var existing = new TestBlock();
        testBlock.Children.Add(existing);
        testBlock.AreBlockFieldsLocked = true;

        Assert.ThrowsAny<InvalidOperationException>(() => testBlock.SetChild(new TestBlock()));
        Assert.Same(existing, Assert.Single(testBlock.GetChildren<TestBlock>()));
    }

    [Fact]
    public void LockFields_NestedChildren_LocksSelfAndEveryDescendant()
    {
        var child = new TestBlock();
        var grandchild = new OtherTestBlock();
        testBlock.Children.Add(child);
        child.Children.Add(grandchild);

        testBlock.LockFields();

        Assert.True(testBlock.AreBlockFieldsLocked);
        Assert.True(child.AreBlockFieldsLocked);
        Assert.True(grandchild.AreBlockFieldsLocked);
    }

    [Fact]
    public void UnlockFields_NestedChildren_UnlocksSelfAndEveryDescendant()
    {
        var child = new TestBlock();
        var grandchild = new OtherTestBlock();
        testBlock.Children.Add(child);
        child.Children.Add(grandchild);
        testBlock.LockFields();

        testBlock.UnlockFields();

        Assert.False(testBlock.AreBlockFieldsLocked);
        Assert.False(child.AreBlockFieldsLocked);
        Assert.False(grandchild.AreBlockFieldsLocked);
    }

    [Fact]
    public void LockFields_ChildAddedAfterward_IsNotRetroactivelyLocked()
    {
        testBlock.LockFields();
        var lateChild = new TestBlock();

        testBlock.Children.Add(lateChild);

        Assert.False(lateChild.AreBlockFieldsLocked);
    }

    [Fact]
    public void Validate_NoChildren_ReturnsEmpty()
    {
        var issues = testBlock.Validate(context);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_ChildWithIssue_ReturnsChildsIssue()
    {
        var issue = MakeIssue("child-rule");
        var child = new RecordingBlock();
        child.OwnIssues.Add(issue);
        testBlock.Children.Add(child);

        var issues = testBlock.Validate(context);

        Assert.Equal(issue, Assert.Single(issues));
    }

    [Fact]
    public void Validate_MultipleChildrenWithIssues_ReturnsEveryChildsIssue()
    {
        var firstIssue = MakeIssue("first-rule");
        var secondIssue = MakeIssue("second-rule");
        var first = new RecordingBlock();
        first.OwnIssues.Add(firstIssue);
        var second = new RecordingBlock();
        second.OwnIssues.Add(secondIssue);
        testBlock.Children.Add(first);
        testBlock.Children.Add(second);

        var issues = testBlock.Validate(context);

        Assert.Equal([firstIssue, secondIssue], issues);
    }

    [Fact]
    public void Validate_GrandchildWithIssue_ReturnsGrandchildsIssue()
    {
        var issue = MakeIssue("grandchild-rule");
        var child = new TestBlock();
        var grandchild = new RecordingBlock();
        grandchild.OwnIssues.Add(issue);
        child.Children.Add(grandchild);
        testBlock.Children.Add(child);

        var issues = testBlock.Validate(context);

        Assert.Equal(issue, Assert.Single(issues));
    }

    [Fact]
    public void Validate_ChildBlock_PassesContextThrough()
    {
        var child = new RecordingBlock();
        testBlock.Children.Add(child);

        testBlock.Validate(context).ToList();

        Assert.Same(context, child.ReceivedContext);
    }
}

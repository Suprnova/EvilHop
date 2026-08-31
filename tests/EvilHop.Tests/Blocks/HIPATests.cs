using EvilHop.Blocks;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Tests.Blocks;

public class HIPATests
{
    [Fact]
    public void HIPA_Tag_IsCorrect()
    {
        var hipa = new HIPA();
        Assert.Equal("HIPA", hipa.Tag);
    }

    [Fact]
    public void Validate_NoChildren_ReturnsEmpty()
    {
        var hipa = new HIPA();

        var issues = hipa.Validate(new ValidationContext(N100FSerializer.DefaultProfile));

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_HasChild_ReportsNoChildrenIssue()
    {
        var hipa = new HIPA();
        hipa.Children.Add(new HIPA());

        var issues = hipa.Validate(new ValidationContext(N100FSerializer.DefaultProfile));

        Assert.Contains(issues, i => i.RuleId == "hipa-no-children");
    }
}

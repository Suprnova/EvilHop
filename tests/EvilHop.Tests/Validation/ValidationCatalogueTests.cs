using EvilHop.Blocks;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Tests.Validation;

public class ValidationCatalogueTests
{
    private static readonly ValidationContext BFBB = new(BFBBSerializer.DefaultProfile);

    private sealed class UndecoratedTestBlock : Block
    {
        protected internal override string Tag => "TEST";
    }

    [Fact]
    public void Instance_CalledTwice_ReturnsSameInstance()
    {
        var first = ValidationCatalogue.Instance;
        var second = ValidationCatalogue.Instance;

        Assert.Same(first, second);
    }

    [Fact]
    public void Validate_TypeNotInCatalogue_ReturnsEmpty()
    {
        var block = new UndecoratedTestBlock();

        var issues = ValidationCatalogue.Instance.Validate(block, BFBB);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_PropertyLevelViolation_SitesAtBlockField()
    {
        var version = new PackageVersion { SubVersion = 3, ClientVersion = ClientVersion.Default, CompatVersion = 1 };

        var issue = Assert.Single(
            ValidationCatalogue.Instance.Validate(version, BFBB),
            i => i.RuleId == "pver.subversion-constant");

        var site = Assert.IsType<BlockFieldSite>(issue.Site, exactMatch: false);
        Assert.Equal("SubVersion", site.Member);
    }

    [Fact]
    public void Validate_ClassLevelViolation_SitesAtBlock()
    {
        var hipa = new HIPA();
        hipa.Children.Add(new HIPA());

        var issue = Assert.Single(
            ValidationCatalogue.Instance.Validate(hipa, BFBB),
            i => i.RuleId == "hipa-no-children");

        Assert.IsType<BlockSite>(issue.Site, exactMatch: false);
    }

    [Fact]
    public void Validate_Violation_ReportsSeverityFromAttribute()
    {
        var version = new PackageVersion { SubVersion = 3, ClientVersion = ClientVersion.Default, CompatVersion = 1 };

        var issue = Assert.Single(
            ValidationCatalogue.Instance.Validate(version, BFBB),
            i => i.RuleId == "pver.subversion-constant");

        Assert.Equal(Severity.Error, issue.Severity);
    }
}

using EvilHop.Blocks;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Tests.Validation;

public class ValidationCatalogueTests
{
    private static readonly ValidationContext BFBB = new(BFBBSerializer.DefaultProfile);

    private sealed class UndecoratedTestBlock : Block
    {
        public override string Tag => "TEST";
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

        Assert.Equal(Severity.Info, issue.Severity);
    }

    [Fact]
    public void Observables_ContainsOneEntry_PerRuleAttributedProperty()
    {
        var observables = ValidationCatalogue.Instance.Observables.Where(o => o.Id.StartsWith("PVER.") && o.Kind == ObservableKind.FieldValue);

        Assert.Equal(["PVER.clientVersion", "PVER.compatVersion", "PVER.subVersion"], observables.Select(o => o.Id).Order());
    }

    [Fact]
    public void Observables_ConstantValueUintProperty_IsEnumeratedNumber()
    {
        var observable = ValidationCatalogue.Instance.Observables.Single(o => o.Id == "PVER.subVersion");

        Assert.Equal(ObservableScope.Block, observable.Scope);
        Assert.Equal(ObservableCardinality.Enumerated, observable.Cardinality);
        Assert.Equal(ObservablePresentation.Number, observable.Presentation);
    }

    [Fact]
    public void Observables_DefinedBitsFlagsProperty_IsBitmaskHex()
    {
        var observable = ValidationCatalogue.Instance.Observables.Single(o => o.Id == "PFLG.flags");

        Assert.Equal(ObservableCardinality.Bitmask, observable.Cardinality);
        Assert.Equal(ObservablePresentation.Hex, observable.Presentation);
    }

    [Fact]
    public void Observables_AllowedValuesStringProperty_IsEnumeratedText()
    {
        var observable = ValidationCatalogue.Instance.Observables.Single(o => o.Id == "PLAT.region");

        Assert.Equal(ObservableCardinality.Enumerated, observable.Cardinality);
        Assert.Equal(ObservablePresentation.Text, observable.Presentation);
    }

    [Fact]
    public void Observables_RequiredChildAttribute_YieldsAStructuralObservable()
    {
        var observable = ValidationCatalogue.Instance.Observables.Single(o => o.Id == "PACK.platform");

        Assert.Equal(ObservableKind.Structural, observable.Kind);
    }

    [Fact]
    public void Observables_NoChildrenAttribute_YieldsAStructuralObservable()
    {
        var observable = ValidationCatalogue.Instance.Observables.Single(o => o.Id == "PVER.childCount");

        Assert.Equal(ObservableKind.Structural, observable.Kind);
    }

    [Fact]
    public void Observe_DecoratedBlock_YieldsEveryPropertyValue()
    {
        var version = new PackageVersion { SubVersion = 3, ClientVersion = ClientVersion.Default, CompatVersion = 1 };

        var values = ValidationCatalogue.Instance.Observe(version).ToDictionary(o => o.ObservableId, o => o.Value);

        Assert.Equal(3u, values["PVER.subVersion"]);
        Assert.Equal((uint)ClientVersion.Default, values["PVER.clientVersion"]);
        Assert.Equal(1u, values["PVER.compatVersion"]);
    }

    [Fact]
    public void Observe_DecoratedBlock_NeverYieldsALibraryEnum()
    {
        var version = new PackageVersion { SubVersion = 3, ClientVersion = ClientVersion.Default, CompatVersion = 1 };

        var value = ValidationCatalogue.Instance.Observe(version).Single(o => o.ObservableId == "PVER.clientVersion").Value;

        Assert.IsType<uint>(value, exactMatch: false);
        Assert.IsNotType<Enum>(value, exactMatch: false);
    }

    [Fact]
    public void Observe_UndecoratedBlock_ReturnsEmpty()
    {
        var block = new UndecoratedTestBlock();

        var values = ValidationCatalogue.Instance.Observe(block);

        Assert.Empty(values);
    }

    [Fact]
    public void DigestOf_UnknownObservable_Throws() =>
        Assert.Throws<ArgumentException>(() => ValidationCatalogue.Instance.DigestOf("NOPE.doesNotExist"));

    [Fact]
    public void DigestOf_CalledTwice_ReturnsSameDigest()
    {
        string first = ValidationCatalogue.Instance.DigestOf("PVER.subVersion");
        string second = ValidationCatalogue.Instance.DigestOf("PVER.subVersion");

        Assert.Equal(first, second);
    }

    [Fact]
    public void DigestOf_DifferentObservables_ReturnDifferentDigests()
    {
        string subVersion = ValidationCatalogue.Instance.DigestOf("PVER.subVersion");
        string compatVersion = ValidationCatalogue.Instance.DigestOf("PVER.compatVersion");

        Assert.NotEqual(subVersion, compatVersion);
    }
}

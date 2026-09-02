using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
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

        Assert.Equal(3L, values["PVER.subVersion"]);
        Assert.Equal((long)ClientVersion.Default, values["PVER.clientVersion"]);
        Assert.Equal(1L, values["PVER.compatVersion"]);
    }

    [Fact]
    public void Observe_DecoratedBlock_NeverYieldsALibraryEnum()
    {
        var version = new PackageVersion { SubVersion = 3, ClientVersion = ClientVersion.Default, CompatVersion = 1 };

        var value = ValidationCatalogue.Instance.Observe(version).Single(o => o.ObservableId == "PVER.clientVersion").Value;

        Assert.IsType<long>(value, exactMatch: false);
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

    [Fact]
    public void DigestOf_AssetCodecs_ReturnsADigest() =>
        Assert.NotEmpty(ValidationCatalogue.Instance.DigestOf(ValidationCatalogue.AssetCodecsKey));

    [Fact]
    public void DigestOf_TwoGranularitiesOfOneMember_ReturnDifferentDigests()
    {
        string cumulative = ValidationCatalogue.Instance.DigestOf("baseAsset.physical.baseType");
        string perType = ValidationCatalogue.Instance.DigestOf("baseAsset.physical.baseType@assetType");

        Assert.NotEqual(cumulative, perType);
    }

    [Fact]
    public void Observables_PhysicalAssetMember_IsIdentifiedByItsSurfaceAndMember()
    {
        var observable = ValidationCatalogue.Instance.Observables.Single(o => o.Id == "asset.physical.alignment@assetType");

        Assert.Equal(ObservableScope.Asset, observable.Scope);
        Assert.Equal(ObservableGrouping.AssetType, observable.Grouping);
        Assert.Equal(ObservablePresentation.Fourcc, observable.KeyPresentation);
    }

    [Fact]
    public void Observables_MemberDeclaringBothGranularities_YieldsOnePerGranularity()
    {
        var baseTypes = ValidationCatalogue.Instance.Observables
            .Where(o => o.Id.StartsWith("baseAsset.physical.baseType", StringComparison.Ordinal));

        Assert.Equal(
            ["baseAsset.physical.baseType", "baseAsset.physical.baseType@assetType"],
            baseTypes.Select(o => o.Id).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Observables_UngroupedObservable_HasNoKeyPresentation()
    {
        var observable = ValidationCatalogue.Instance.Observables.Single(o => o.Id == "baseAsset.physical.baseType");

        Assert.Equal(ObservableGrouping.None, observable.Grouping);
        Assert.Null(observable.KeyPresentation);
    }

    [Fact]
    public void Observables_AssetScopedIds_NeverCollideWithABlockTag()
    {
        var assetIds = ValidationCatalogue.Instance.Observables
            .Where(o => o.Scope == ObservableScope.Asset)
            .Select(o => o.Id);

        Assert.All(assetIds, id => Assert.DoesNotContain(id, ValidationCatalogue.Instance.Observables
            .Where(o => o.Scope == ObservableScope.Block)
            .Select(o => o.Id)));
    }

    [Fact]
    public void Observe_EntityShapedAsset_YieldsEveryDeclaredMemberOfEverySurface()
    {
        var asset = new GenericEntityAsset { Type = AssetType.Trigger };
        asset.Physical.Alignment = 32;
        asset.Physical.BaseType = 5;
        asset.Physical.Subtype = 2;

        var observations = ValidationCatalogue.Instance.Observe(asset).ToDictionary(o => o.ObservableId, o => o.Value);

        Assert.Equal(32L, observations["asset.physical.alignment@assetType"]);
        Assert.Equal(5L, observations["baseAsset.physical.baseType"]);
        Assert.Equal(5L, observations["baseAsset.physical.baseType@assetType"]);
        Assert.Equal(2L, observations["entityAsset.physical.subtype@assetType"]);
    }

    [Fact]
    public void Observe_GroupedObservable_KeysOnTheRawAssetType()
    {
        var asset = new GenericEntityAsset { Type = AssetType.Trigger };

        var observation = ValidationCatalogue.Instance.Observe(asset)
            .First(o => o.ObservableId == "asset.physical.alignment@assetType");

        Assert.Equal((uint)AssetType.Trigger, observation.GroupKey);
    }

    [Fact]
    public void Observe_UngroupedObservable_CarriesNoGroupKey()
    {
        var asset = new GenericEntityAsset { Type = AssetType.Trigger };

        var observation = ValidationCatalogue.Instance.Observe(asset)
            .First(o => o.ObservableId == "baseAsset.physical.baseType");

        Assert.Null(observation.GroupKey);
    }

    [Fact]
    public void Observe_AssetDegradedPastBaseAsset_YieldsOnlyWhatItActuallyCarries()
    {
        // A parse failure degrades an asset to GenericAsset, whose bytes were never read - so its
        // header-sourced fields are still observed and its payload-sourced ones genuinely aren't.
        var asset = new GenericAsset { Type = AssetType.Trigger };
        asset.Physical.Alignment = 16;

        var observed = ValidationCatalogue.Instance.Observe(asset).Select(o => o.ObservableId).ToList();

        Assert.Contains("asset.physical.alignment@assetType", observed);
        Assert.DoesNotContain("baseAsset.physical.baseType", observed);
    }
}

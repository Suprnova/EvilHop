using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Tests.Validation;

/// <summary>
/// Exercises <see cref="ValueRule.Holds"/> for every attribute-declared rule kind, through the real
/// attribute placements on the block classes rather than synthetic types - these tests double-check
/// §7's conversions, not just the rule mechanics.
/// </summary>
public class ValueRuleTests
{
    private static readonly ValidationContext N100F = new(N100FSerializer.DefaultProfile);
    private static readonly ValidationContext BFBB = new(BFBBSerializer.DefaultProfile);

    private static bool Violates(Block block, ValidationContext context, string ruleId) =>
        ValidationCatalogue.Instance.Validate(block, context).Any(issue => issue.RuleId == ruleId);

    // ConstantValue

    [Fact]
    public void ConstantValue_ExpectedValue_Holds()
    {
        var version = new PackageVersion { SubVersion = 2, ClientVersion = ClientVersion.Default, CompatVersion = 1 };

        Assert.False(Violates(version, BFBB, "pver.subversion-constant"));
    }

    [Fact]
    public void ConstantValue_UnexpectedValue_DoesNotHold()
    {
        var version = new PackageVersion { SubVersion = 3, ClientVersion = ClientVersion.Default, CompatVersion = 1 };

        Assert.True(Violates(version, BFBB, "pver.subversion-constant"));
    }

    [Fact]
    public void ConstantValue_ClientVersion_N100F_AllowsPrototypeAndRelease()
    {
        var prototype = new PackageVersion { SubVersion = 2, ClientVersion = ClientVersion.N100FPrototype, CompatVersion = 1 };
        var release = new PackageVersion { SubVersion = 2, ClientVersion = ClientVersion.N100FRelease, CompatVersion = 1 };

        Assert.False(Violates(prototype, N100F, "pver.clientversion-allowed-values"));
        Assert.False(Violates(release, N100F, "pver.clientversion-allowed-values"));
    }

    [Fact]
    public void ConstantValue_ClientVersion_BFBB_RequiresDefault()
    {
        var version = new PackageVersion { SubVersion = 2, ClientVersion = ClientVersion.N100FRelease, CompatVersion = 1 };

        Assert.True(Violates(version, BFBB, "pver.clientversion-constant"));
    }

    // AllowedValues

    [Theory]
    [InlineData("NTSC")]
    [InlineData("PAL")]
    public void AllowedValues_ExpectedValue_Holds(string region)
    {
        var platform = new PackagePlatform { Region = region };

        Assert.False(Violates(platform, BFBB, "plat.region-allowed-values"));
    }

    [Fact]
    public void AllowedValues_UnexpectedValue_DoesNotHold()
    {
        var platform = new PackagePlatform { Region = "JPN" };

        Assert.True(Violates(platform, BFBB, "plat.region-allowed-values"));
    }

    // ClosedEnum

    [Fact]
    public void ClosedEnum_DefinedMember_Holds()
    {
        var header = new AssetHeader { Type = AssetType.Animation, Debug = new AssetDebug() };

        Assert.False(Violates(header, BFBB, "ahdr.type-closed-enum"));
    }

    [Fact]
    public void ClosedEnum_UndefinedValue_DoesNotHold()
    {
        var header = new AssetHeader { Type = (AssetType)0, Debug = new AssetDebug() };

        Assert.True(Violates(header, BFBB, "ahdr.type-closed-enum"));
    }

    // DefinedBits

    [Fact]
    public void DefinedBits_OnlyKnownBits_Holds()
    {
        var flags = new PackageFlags { Flags = PackFlags.Default | PackFlags.GameCube };

        Assert.False(Violates(flags, BFBB, "pflg.flags-defined-bits"));
    }

    [Fact]
    public void DefinedBits_UnknownBitSet_DoesNotHold()
    {
        var flags = new PackageFlags { Flags = PackFlags.Default | (PackFlags)(1U << 30) };

        Assert.True(Violates(flags, BFBB, "pflg.flags-defined-bits"));
    }

    // RequiredBits

    [Fact]
    public void RequiredBits_AllRequiredBitsSet_Holds()
    {
        var flags = new PackageFlags { Flags = PackFlags.Default | PackFlags.GameCube };

        Assert.False(Violates(flags, BFBB, "pflg.flags-required-bits"));
    }

    [Fact]
    public void RequiredBits_MissingSomeRequiredBits_DoesNotHold()
    {
        var flags = new PackageFlags { Flags = PackFlags.Unknown2 };

        Assert.True(Violates(flags, BFBB, "pflg.flags-required-bits"));
    }

    // RequiredChild

    [Fact]
    public void RequiredChild_InScope_ChildPresent_Holds()
    {
        var package = new Package { Platform = new PackagePlatform() };

        Assert.False(Violates(package, BFBB, "pack.platform-required"));
    }

    [Fact]
    public void RequiredChild_InScope_ChildMissing_DoesNotHold()
    {
        var package = new Package();

        Assert.True(Violates(package, BFBB, "pack.platform-required"));
    }

    [Fact]
    public void RequiredChild_OutOfScope_ChildAbsent_Holds()
    {
        var package = new Package();

        Assert.False(Violates(package, N100F, "pack.platform-required"));
    }

    [Fact]
    public void RequiredChild_OutOfScope_ChildPresent_DoesNotHold()
    {
        var package = new Package { Platform = new PackagePlatform() };

        Assert.True(Violates(package, N100F, "pack.platform-required"));
    }

    // NoChildren

    [Fact]
    public void NoChildren_NoChildren_Holds()
    {
        var hipa = new HIPA();

        Assert.False(Violates(hipa, BFBB, "hipa-no-children"));
    }

    [Fact]
    public void NoChildren_HasChild_DoesNotHold()
    {
        var hipa = new HIPA();
        hipa.Children.Add(new HIPA());

        Assert.True(Violates(hipa, BFBB, "hipa-no-children"));
    }

    // RepeatableChild - no rule, so nothing to hold or fail

    [Fact]
    public void RepeatableChild_AnyCount_ProducesNoRuleAtAll()
    {
        var table = new AssetTable { Inf = new AssetInf() };

        var issues = ValidationCatalogue.Instance.Validate(table, BFBB);

        Assert.DoesNotContain(issues, issue => issue.RuleId.StartsWith("atoc.headers", StringComparison.Ordinal));
    }
}

using EvilHop.Blocks;
using EvilHop.Corpus.Invariants;

namespace EvilHop.Corpus.Tests.Invariants;

public class StructuralInvariantTests
{
    private static ArchiveContext ArchiveOf(params Block[] roots) => new()
    {
        BuildKey = "n100f/release",
        RelativePath = "n100f/release/boot.HIP",
        Roots = roots,
        ArchiveLength = 0
    };

    [Fact]
    public void Check_StructurallyValidMinimalArchive_EveryDeclaredBlockTypePasses()
    {
        var invariant = new StructuralInvariant();

        invariant.Check(ArchiveOf([.. BlockFactory.MinimalArchive()]));

        var json = invariant.ToJson();
        foreach (var blockType in new[] { "HIPA", "Package", "Dictionary", "AssetTable", "LayerTable", "AssetStream" })
        {
            Assert.Equal(0, json[blockType]!["outcomes"]!["violated"]!.GetValue<long>());
            Assert.Equal(1, json[blockType]!["outcomes"]!["passing"]!.GetValue<long>());
        }
    }

    [Fact]
    public void Check_PackageMissingRequiredChild_RecordsViolation()
    {
        var package = BlockFactory.Create<Package>();
        package.Version = BlockFactory.Create<PackageVersion>();
        package.Flags = BlockFactory.Create<PackageFlags>();
        package.Counts = BlockFactory.Create<PackageCount>();
        package.Created = BlockFactory.Create<PackageCreated>();
        // PackageModified intentionally omitted.

        var invariant = new StructuralInvariant();
        invariant.Check(ArchiveOf(package));

        var outcomes = invariant.ToJson()["Package"]!["outcomes"]!;
        Assert.Equal(1, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(0, outcomes["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_PackageWithDuplicateExactlyChild_RecordsViolation()
    {
        var package = BlockFactory.Create<Package>();
        package.Version = BlockFactory.Create<PackageVersion>();
        package.Flags = BlockFactory.Create<PackageFlags>();
        package.Counts = BlockFactory.Create<PackageCount>();
        package.Created = BlockFactory.Create<PackageCreated>();
        package.Modified = BlockFactory.Create<PackageModified>();
        package.Children.Add(BlockFactory.Create<PackageVersion>());

        var invariant = new StructuralInvariant();
        invariant.Check(ArchiveOf(package));

        var outcomes = invariant.ToJson()["Package"]!["outcomes"]!;
        Assert.Equal(1, outcomes["violated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_AssetTableWithManyHeaders_Passes()
    {
        var assetTable = BlockFactory.Create<AssetTable>();
        assetTable.Inf = BlockFactory.Create<AssetInf>();
        assetTable.Children.Add(BlockFactory.CreateAssetHeader(1, "a"));
        assetTable.Children.Add(BlockFactory.CreateAssetHeader(2, "b"));

        var invariant = new StructuralInvariant();
        invariant.Check(ArchiveOf(assetTable));

        var outcomes = invariant.ToJson()["AssetTable"]!["outcomes"]!;
        Assert.Equal(0, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(1, outcomes["passing"]!.GetValue<long>());
    }
}

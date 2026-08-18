using EvilHop.Blocks;
using EvilHop.Corpus.Extraction;

namespace EvilHop.Corpus.Tests.Extraction;

public class FieldExtractorTests
{
    [Fact]
    public void GetFields_AnyBlockType_ExcludesChildrenAndParent()
    {
        var fields = FieldExtractor.GetFields(typeof(AssetHeader));

        Assert.DoesNotContain(fields, f => f.Name is nameof(Block.Children) or nameof(Block.Parent));
    }

    [Fact]
    public void GetFields_AssetTable_ExcludesHeadersBlockCollectionAndInfChildAccessor()
    {
        var fields = FieldExtractor.GetFields(typeof(AssetTable));

        Assert.DoesNotContain(fields, f => f.Name == nameof(AssetTable.Headers));
        Assert.DoesNotContain(fields, f => f.Name == nameof(AssetTable.Inf));
    }

    [Fact]
    public void GetFields_Dictionary_ExcludesRequiredChildAccessors()
    {
        var fields = FieldExtractor.GetFields(typeof(Dictionary));

        Assert.DoesNotContain(fields, f => f.Name is nameof(Dictionary.AssetTable) or nameof(Dictionary.LayerTable));
    }

    [Fact]
    public void GetFields_StreamData_IncludesByteArrayFields()
    {
        var fields = FieldExtractor.GetFields(typeof(StreamData));

        Assert.Contains(fields, f => f.Name == nameof(StreamData.Data));
        Assert.Contains(fields, f => f.Name == nameof(StreamData.Padding));
    }

    [Fact]
    public void GetFields_LayerHeader_IncludesNonBlockCollection()
    {
        var fields = FieldExtractor.GetFields(typeof(LayerHeader));

        Assert.Contains(fields, f => f.Name == nameof(LayerHeader.AssetIds));
    }

    [Fact]
    public void TryGetValue_ThrowingRequiredChildGetter_ReturnsFalseWithoutThrowing()
    {
        var package = BlockFactory.Create<Package>();
        var property = typeof(Package).GetProperty(nameof(Package.Version))!;

        bool succeeded = FieldExtractor.TryGetValue(property, package, out var value);

        Assert.False(succeeded);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_NonThrowingGetter_ReturnsTrueWithValue()
    {
        var version = BlockFactory.Create<PackageVersion>();
        version.SubVersion = 2;
        var property = typeof(PackageVersion).GetProperty(nameof(PackageVersion.SubVersion))!;

        bool succeeded = FieldExtractor.TryGetValue(property, version, out var value);

        Assert.True(succeeded);
        Assert.Equal(2u, value);
    }
}

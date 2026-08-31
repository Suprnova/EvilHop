using EvilHop.Blocks;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Tests.Blocks;

public class DictionaryTests
{
    public static IEnumerable<object[]> AssetHeaderAllFields =>
    [
        [(Action<AssetHeader>)((h) => h.Id = 0)],
        [(Action<AssetHeader>)((h) => h.Type = 0)],
        [(Action<AssetHeader>)((h) => h.Offset = 0)],
        [(Action<AssetHeader>)((h) => h.Size = 0)],
        [(Action<AssetHeader>)((h) => h.Plus = 0)],
        [(Action<AssetHeader>)((h) => h.Flags = 0)],
    ];

    public static IEnumerable<object[]> AssetDebugManagedFields =>
    [
        [(Action<AssetDebug>)((d) => d.Alignment = 0)],
        [(Action<AssetDebug>)((d) => d.Name = String.Empty)],
    ];

    public static IEnumerable<object[]> AssetDebugUnmanagedFields =>
    [
        [(Action<AssetDebug>)((d) => d.FileName = String.Empty)],
        [(Action<AssetDebug>)((d) => d.Checksum = 0)],
    ];

    public static IEnumerable<object[]> LayerHeaderAllFields =>
    [
        [(Action<LayerHeader>)((h) => h.Type = 0)],
        [(Action<LayerHeader>)((h) => h.AssetCount = 0)],
        [(Action<LayerHeader>)((h) => h.AssetIds = [])],
    ];

    [Fact]
    public void Dictionary_Tag_IsCorrect()
    {
        var dictionary = new Dictionary();
        Assert.Equal("DICT", dictionary.Tag);
    }

    [Fact]
    public void Dictionary_AssetTable_IsRequired()
    {
        var dictionary = new Dictionary();
        Assert.Throws<InvalidOperationException>(() => dictionary.AssetTable);
    }

    [Fact]
    public void Dictionary_AssetTable_Setter_ReturnsSetValue()
    {
        var dictionary = new Dictionary();
        var assetTable = new AssetTable();

        dictionary.AssetTable = assetTable;
        Assert.Same(assetTable, dictionary.AssetTable);
    }

    [Fact]
    public void Dictionary_LayerTable_IsRequired()
    {
        var dictionary = new Dictionary();
        Assert.Throws<InvalidOperationException>(() => dictionary.LayerTable);
    }

    [Fact]
    public void Dictionary_LayerTable_Setter_ReturnsSetValue()
    {
        var dictionary = new Dictionary();
        var layerTable = new LayerTable();

        dictionary.LayerTable = layerTable;
        Assert.Same(layerTable, dictionary.LayerTable);
    }

    [Fact]
    public void AssetTable_Tag_IsCorrect()
    {
        var atoc = new AssetTable();
        Assert.Equal("ATOC", atoc.Tag);
    }

    [Fact]
    public void AssetTable_AssetInf_IsRequired()
    {
        var atoc = new AssetTable();
        Assert.Throws<InvalidOperationException>(() => atoc.Inf);
    }

    [Fact]
    public void AssetTable_AssetInf_Setter_ReturnsSetValue()
    {
        var atoc = new AssetTable();
        var inf = new AssetInf();

        atoc.Inf = inf;
        Assert.Same(inf, atoc.Inf);
    }

    [Fact]
    public void AssetInf_Tag_IsCorrect()
    {
        var inf = new AssetInf();
        Assert.Equal("AINF", inf.Tag);
    }

    [Fact]
    public void AssetTable_Headers_Setter_ReplacesExistingHeaders()
    {
        var atoc = new AssetTable();
        atoc.Children.Add(new AssetHeader());
        var replacement = new AssetHeader();

        atoc.Headers = [replacement];

        Assert.Same(replacement, Assert.Single(atoc.Headers));
    }

    [Fact]
    public void AssetTable_Headers_Setter_ClearsReplacedHeaderParent()
    {
        var atoc = new AssetTable();
        var original = new AssetHeader();
        atoc.Children.Add(original);

        atoc.Headers = [];

        Assert.Null(original.Parent);
    }

    [Fact]
    public void AssetTable_Headers_Setter_WhenLocked_ThrowsInvalidOperationException()
    {
        var atoc = new AssetTable { AreBlockFieldsLocked = true };

        Assert.Throws<InvalidOperationException>(() => atoc.Headers = []);
    }

    [Fact]
    public void AssetHeader_Tag_IsCorrect()
    {
        var header = new AssetHeader();
        Assert.Equal("AHDR", header.Tag);
    }

    [Fact]
    public void AssetHeader_AssetDebug_IsRequired()
    {
        var header = new AssetHeader();
        Assert.Throws<InvalidOperationException>(() => header.Debug);
    }

    [Fact]
    public void AssetHeader_AssetDebug_Setter_ReturnsSetValue()
    {
        var header = new AssetHeader();
        var debug = new AssetDebug();

        header.Debug = debug;
        Assert.Same(debug, header.Debug);
    }

    [Theory]
    [MemberData(nameof(AssetHeaderAllFields))]
    public void AssetHeader_AllFields_AreManaged(Action<AssetHeader> setter)
    {
        var header = new AssetHeader
        {
            AreBlockFieldsLocked = true
        };
        Assert.Throws<InvalidOperationException>(() => setter(header));
    }

    [Fact]
    public void AssetHeader_Type_IsManaged()
    {
        var header = new AssetHeader
        {
            AreBlockFieldsLocked = true
        };
        Assert.Throws<InvalidOperationException>(() => header.Type = 0);
    }

    [Fact]
    public void AssetDebug_Tag_IsCorrect()
    {
        var debug = new AssetDebug();
        Assert.Equal("ADBG", debug.Tag);
    }

    [Theory]
    [MemberData(nameof(AssetDebugManagedFields))]
    public void AssetDebug_ManagedFields_AreManaged(Action<AssetDebug> setter)
    {
        var debug = new AssetDebug
        {
            AreBlockFieldsLocked = true
        };
        Assert.Throws<InvalidOperationException>(() => setter(debug));
    }

    [Theory]
    [MemberData(nameof(AssetDebugUnmanagedFields))]
    public void AssetDebug_UnmanagedFields_AreNotManaged(Action<AssetDebug> setter)
    {
        var debug = new AssetDebug
        {
            AreBlockFieldsLocked = true
        };

        var exception = Record.Exception(() => setter(debug));

        Assert.Null(exception);
    }

    [Fact]
    public void LayerTable_Tag_IsCorrect()
    {
        var ltoc = new LayerTable();
        Assert.Equal("LTOC", ltoc.Tag);
    }

    [Fact]
    public void LayerTable_LayerInf_IsRequired()
    {
        var ltoc = new LayerTable();
        Assert.Throws<InvalidOperationException>(() => ltoc.Inf);
    }

    [Fact]
    public void LayerTable_LayerInf_Setter_ReturnsSetValue()
    {
        var ltoc = new LayerTable();
        var inf = new LayerInf();

        ltoc.Inf = inf;
        Assert.Same(inf, ltoc.Inf);
    }

    [Fact]
    public void LayerInf_Tag_IsCorrect()
    {
        var inf = new LayerInf();
        Assert.Equal("LINF", inf.Tag);
    }

    [Fact]
    public void LayerTable_Headers_Setter_ReplacesExistingHeaders()
    {
        var ltoc = new LayerTable();
        ltoc.Children.Add(new LayerHeader());
        var replacement = new LayerHeader();

        ltoc.Headers = [replacement];

        Assert.Same(replacement, Assert.Single(ltoc.Headers));
    }

    [Fact]
    public void LayerTable_Headers_Setter_ClearsReplacedHeaderParent()
    {
        var ltoc = new LayerTable();
        var original = new LayerHeader();
        ltoc.Children.Add(original);

        ltoc.Headers = [];

        Assert.Null(original.Parent);
    }

    [Fact]
    public void LayerTable_Headers_Setter_WhenLocked_ThrowsInvalidOperationException()
    {
        var ltoc = new LayerTable { AreBlockFieldsLocked = true };

        Assert.Throws<InvalidOperationException>(() => ltoc.Headers = []);
    }

    [Fact]
    public void LayerHeader_Tag_IsCorrect()
    {
        var header = new LayerHeader();
        Assert.Equal("LHDR", header.Tag);
    }

    [Fact]
    public void LayerHeader_LayerDebug_IsRequired()
    {
        var header = new LayerHeader();
        Assert.Throws<InvalidOperationException>(() => header.Debug);
    }

    [Fact]
    public void LayerHeader_LayerDebug_Setter_ReturnsSetValue()
    {
        var header = new LayerHeader();
        var debug = new LayerDebug();

        header.Debug = debug;
        Assert.Same(debug, header.Debug);
    }

    [Theory]
    [MemberData(nameof(LayerHeaderAllFields))]
    public void LayerHeader_AllFields_AreManaged(Action<LayerHeader> setter)
    {
        var header = new LayerHeader
        {
            AreBlockFieldsLocked = true
        };
        Assert.Throws<InvalidOperationException>(() => setter(header));
    }

    [Fact]
    public void LayerDebug_Tag_IsCorrect()
    {
        var debug = new LayerDebug();
        Assert.Equal("LDBG", debug.Tag);
    }

    [Fact]
    public void Validate_AssetTableAndLayerTableMissing_ReportsBothRequiredChildIssues()
    {
        var dictionary = new Dictionary();

        var issues = dictionary.Validate(new ValidationContext(N100FSerializer.DefaultProfile));

        Assert.Contains(issues, i => i.RuleId == "dict.assettable-required");
        Assert.Contains(issues, i => i.RuleId == "dict.layertable-required");
    }

    [Fact]
    public void Validate_AssetTableAndLayerTablePresent_ReportsNeitherRequiredChildIssue()
    {
        var dictionary = new Dictionary
        {
            AssetTable = new AssetTable { Inf = new AssetInf() },
            LayerTable = new LayerTable { Inf = new LayerInf() }
        };

        var issues = dictionary.Validate(new ValidationContext(N100FSerializer.DefaultProfile));

        Assert.DoesNotContain(issues, i => i.RuleId is "dict.assettable-required" or "dict.layertable-required");
    }
}

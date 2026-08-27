using EvilHop.Blocks;
using EvilHop.Corpus.Archives;
using EvilHop.Corpus.Output;
using System.Text.Json;

namespace EvilHop.Corpus.Tests.Output;

public class InventoryWriterTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

    public void Dispose()
    {
        if (File.Exists(path)) File.Delete(path);
        GC.SuppressFinalize(this);
    }

    private static ArchiveContext Archive(string buildKey, string relativePath, uint clientVersion)
    {
        var version = BlockFactory.Create<PackageVersion>();
        version.ClientVersion = (ClientVersion)clientVersion;

        return new ArchiveContext
        {
            BuildKey = buildKey,
            RelativePath = relativePath,
            Roots = [version],
            ArchiveLength = 0
        };
    }

    private static InventoryBuilder BuildSampleInventory()
    {
        var builder = new InventoryBuilder([]);
        builder.Observe(Archive("n100f/release/GC/PAL/UK", "n100f/release/GC/PAL/UK/boot.HIP", 0x000A000F));
        builder.Observe(Archive("bfbb/release/GC/NTSC-U/US", "bfbb/release/GC/NTSC-U/US/boot.HIP", 0x00040006));
        builder.Observe(Archive("n100f/release/GC/NTSC-U/US", "n100f/release/GC/NTSC-U/US/boot.HIP", 0x00040006));
        return builder;
    }

    [Fact]
    public void Write_CalledTwiceWithEquivalentInput_ProducesByteIdenticalOutput()
    {
        string otherPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        try
        {
            InventoryWriter.Write(path, BuildSampleInventory());
            InventoryWriter.Write(otherPath, BuildSampleInventory());

            Assert.Equal(File.ReadAllText(path), File.ReadAllText(otherPath));
        }
        finally
        {
            File.Delete(otherPath);
        }
    }

    [Fact]
    public void Write_OnAnyPlatform_SeparatesLinesWithLineFeedsOnly()
    {
        InventoryWriter.Write(path, BuildSampleInventory());

        string text = File.ReadAllText(path);
        Assert.DoesNotContain('\r', text);
        Assert.EndsWith("\n", text);
    }

    [Fact]
    public void Write_BuildsInsertedOutOfOrder_SortsBuildsArrayByKey()
    {
        InventoryWriter.Write(path, BuildSampleInventory());

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var keys = doc.RootElement.GetProperty("builds").EnumerateArray().Select(b => b.GetProperty("key").GetString()).ToList();

        Assert.Equal(keys.OrderBy(k => k, StringComparer.Ordinal), keys);
    }

    [Fact]
    public void Write_ValuesWithMultipleBuilds_SortsBuildsArrayWithinEachValue()
    {
        InventoryWriter.Write(path, BuildSampleInventory());

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var builds = doc.RootElement
            .GetProperty("fields").GetProperty("PackageVersion.ClientVersion")
            .GetProperty("values").GetProperty("0x00040006").GetProperty("builds")
            .EnumerateArray().Select(b => b.GetString()).ToList();

        Assert.Equal(["bfbb/release/GC/NTSC-U/US", "n100f/release/GC/NTSC-U/US"], builds);
    }

    [Fact]
    public void Write_MultipleFields_SortsFieldKeysAlphabetically()
    {
        InventoryWriter.Write(path, BuildSampleInventory());

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var keys = doc.RootElement.GetProperty("fields").EnumerateObject().Select(p => p.Name).ToList();

        Assert.Equal(keys.OrderBy(k => k, StringComparer.Ordinal), keys);
    }
}

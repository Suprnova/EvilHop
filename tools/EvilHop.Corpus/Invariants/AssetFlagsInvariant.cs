using EvilHop.Blocks;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary><see cref="AssetFlags.SourceFile"/> and <see cref="AssetFlags.SourceVirtual"/> are never both set.</summary>
internal sealed class SourceFlagsExclusiveInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "sourceFlagsExclusive";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var header in archive.AllBlocks.OfType<AssetHeader>())
        {
            bool exclusive = !header.Flags.HasFlag(AssetFlags.SourceFile) || !header.Flags.HasFlag(AssetFlags.SourceVirtual);
            _result.Record(exclusive, () => new JsonObject
            {
                ["path"] = archive.RelativePath,
                ["id"] = $"0x{header.Id:X8}",
                ["flags"] = $"0x{(uint)header.Flags:X8}"
            });
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

/// <summary><see cref="AssetDebug.FileName"/> is populated if and only if <see cref="AssetFlags.SourceFile"/> is set.</summary>
internal sealed class FileNameSetWhenSourceFileInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "fileNameSetWhenSourceFile";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var header in archive.AllBlocks.OfType<AssetHeader>())
        {
            var debug = header.GetChild<AssetDebug>();
            if (debug is null) continue;

            bool isSourceFile = header.Flags.HasFlag(AssetFlags.SourceFile);
            bool hasFileName = debug.FileName.Length > 0;
            _result.Record(isSourceFile == hasFileName, () => new JsonObject
            {
                ["path"] = archive.RelativePath,
                ["id"] = $"0x{header.Id:X8}",
                ["sourceFile"] = isSourceFile,
                ["fileName"] = debug.FileName
            });
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

using EvilHop.Blocks;
using EvilHop.Corpus.Extraction;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary>
/// <see cref="StreamData.Padding"/> is a single repeated fill byte. Records which byte, per build,
/// alongside the pass/fail outcome.
/// </summary>
internal sealed class PaddingIsHomogeneousInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "paddingIsHomogeneous";

    private readonly InvariantResult _result = new();
    private readonly FieldAccumulator _fillBytes = new(FieldKind.Hex);

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var data in archive.AllBlocks.OfType<StreamData>())
        {
            if (data.Padding.Length == 0) continue;

            byte fill = data.Padding[0];
            bool homogeneous = data.Padding.All(b => b == fill);
            _result.Record(homogeneous, () => new JsonObject { ["path"] = archive.RelativePath, ["fillByte"] = $"0x{fill:X2}" });

            if (homogeneous) _fillBytes.Record(fill, archive.BuildKey, archive.RelativePath);
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson()
    {
        var result = _result.ToJson();
        result["fillBytes"] = _fillBytes.ToSummary().ToJson();
        return result;
    }
}

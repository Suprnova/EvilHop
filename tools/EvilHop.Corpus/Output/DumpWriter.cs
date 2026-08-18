using EvilHop.Corpus.Extraction;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Output;

/// <summary>
/// Writes the full-fidelity <c>--dump</c> JSONL - one line per archive, every occurrence of every
/// field value, uncapped and unaggregated. Gitignored; regenerated on demand from the local corpus
/// when the committed, aggregated inventory isn't enough to trace a value back to every file it
/// appears in.
/// </summary>
internal sealed class DumpWriter : IDisposable
{
    private readonly StreamWriter _writer;

    public DumpWriter(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _writer = new StreamWriter(path, append: false);
    }

    /// <summary>
    /// Writes one JSONL record for <paramref name="archive"/>, listing every extracted field value.
    /// </summary>
    public void Write(ArchiveContext archive)
    {
        var fields = new JsonObject();
        foreach (var block in archive.AllBlocks)
        {
            var blockType = block.GetType();
            foreach (var property in FieldExtractor.GetFields(blockType))
            {
                if (!FieldExtractor.TryGetValue(property, block, out var value)) continue;

                var kind = ValueKindClassifier.Classify(property.PropertyType);
                string fieldKey = $"{blockType.Name}.{property.Name}";

                if (fields[fieldKey] is not JsonArray values)
                    fields[fieldKey] = values = [];

                values.Add(RenderValue(value, kind));
            }
        }

        var record = new JsonObject
        {
            ["path"] = archive.RelativePath,
            ["buildKey"] = archive.BuildKey,
            ["fields"] = fields
        };

        _writer.WriteLine(record.ToJsonString());
    }

    private static JsonValue? RenderValue(object? value, ValueKind kind) => kind == ValueKind.Bytes
        ? JsonValue.Create(value is byte[] bytes ? bytes.Length : 0)
        : JsonValue.Create(ValueFormatter.FormatKey(value, kind));

    public void Dispose() => _writer.Dispose();
}

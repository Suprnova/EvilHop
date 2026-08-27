using EvilHop.Blocks;
using EvilHop.Corpus.Archives;
using EvilHop.Corpus.Extraction;
using System.Globalization;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary>
/// <see cref="PackageCreated.CreatedDateString"/> parses to the same wall-clock time as
/// <see cref="PackageCreated.CreatedDate"/>, once converted from UTC to Pacific Time observing
/// whichever DST rule applied on that date - <see cref="PackageCreated.CreatedDate"/> is stored as
/// a raw UTC timestamp, while <see cref="PackageCreated.CreatedDateString"/> was written in the
/// build machine's local (Pacific) clock. Compared whitespace-insensitively: N100F stores a
/// trailing <c>\n</c> that BFBB does not.
/// </summary>
internal sealed class CreatedDateStringMatchesTimestampInvariant : IInvariant
{
    // Matches the private format PackageCreated uses to build CreatedDateString on write.
    private const string Format = "ddd MMM dd HH:mm:ss yyyy";

    private static readonly TimeZoneInfo PacificTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");

    /// <inheritdoc/>
    public string Name => "createdDateStringMatchesTimestamp";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var created in archive.AllBlocks.OfType<PackageCreated>())
        {
            string trimmed = created.CreatedDateString.Trim();
            var pacificCreatedDate = TimeZoneInfo.ConvertTime(created.CreatedDate, PacificTimeZone);
            bool matches = DateTime.TryParseExact(trimmed, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                && parsed == pacificCreatedDate.DateTime;

            _result.Record(matches, () => new JsonObject
            {
                ["path"] = archive.RelativePath,
                ["createdDateString"] = created.CreatedDateString,
                ["createdDate"] = created.CreatedDate.ToString("O", CultureInfo.InvariantCulture)
            });
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

/// <summary>
/// The distinct trailing whitespace sequences observed on <see cref="PackageCreated.CreatedDateString"/>,
/// per build - not itself a pass/fail check, but not visible through the field cardinality summary
/// either, since every archive's timestamp string is otherwise unique and degrades to a length-only
/// summary that would hide the terminator.
/// </summary>
internal sealed class CreatedDateStringTrailingWhitespaceInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "createdDateStringTrailingWhitespace";

    private readonly FieldAccumulator _trailing = new(FieldKind.Text);

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var created in archive.AllBlocks.OfType<PackageCreated>())
        {
            string raw = created.CreatedDateString;
            string trailing = raw[raw.TrimEnd().Length..];
            _trailing.Record(trailing, archive.BuildKey, archive.RelativePath);
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _trailing.ToSummary().ToJson();
}

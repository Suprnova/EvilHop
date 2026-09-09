#pragma warning disable
// locate-asset.cs — for one AssetType tag, find its real occurrences across every game's corpus
// inventory and dump the actual bytes. See SKILL.md for usage.
//
// This automates the one workflow every asset implementation repeats: query corpus/*.json for an
// exemplar archive per game, then use reading-hip-bytes-style raw reads to find that archive's AHDR
// entry and dump its payload. It knows the AHDR field order (id, type, offset, size, plus, flags)
// and the corpus inventory's JSON shape; it does not know anything about a specific asset's own
// layout — that part is still yours to work out from the dump.
//
// HUMAN WARNING: SLOP AHEAD! this was made solely for convenience and may not be the same quality
// as other files in this project. do not trust this script by default.
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

const string Usage = """
locate-asset — find and dump every real occurrence of an AssetType tag across corpus/*.json.

Usage:
  dotnet run --file locate-asset.cs -- <FOURCC> [options]

<FOURCC> is the exact on-disk 4-character tag (pad with a trailing space where the format does,
e.g. 'CAM ', 'UI  ' — quote it so your shell keeps the space).

For every game whose corpus/{game}.json records <FOURCC>, prints the exemplar archive path, that
archive's real AHDR fields (id/offset/size/plus/flags) read directly from the bytes, and a hex dump
of the asset's payload at that offset. Games where the tag was never observed are reported as such,
not silently skipped.

This replaces manually chaining reading-corpus-inventory's 'exemplar' and reading-hip-bytes'
'findall'/'seek'/'bytes' per game. Reach for those two directly only when you need more than a fixed
dump gives you - a different build's archive, a later occurrence, or free-form exploration.

Options:
  --dump <n>          Bytes of payload to hex-dump. Default 96.
  --entry-size <n>    Also read the payload's leading 4 bytes as a big-endian entry count, and hex-
                      dump up to 4 entries of <n> bytes each starting right after it. A guess to
                      confirm, not a fact - plenty of types don't lead with a count.
  --games <list>      Comma-separated subset of n100f,bfbb,incredibles,tssm,rotu,ratatouille.
                      Default: all six.
  --index <n>         Which occurrence of <FOURCC> to read, 0-based, when an archive has more than
                      one. Default 0.
  --corpus-dir <path>     Override the corpus/ directory (default: next to this script's repo).
  --artifacts-dir <path>  Override the artifacts/ directory (default: auto-detected; see below).

Finding artifacts/:
  artifacts/ is gitignored, so a worktree checkout doesn't have its own copy. This script first
  tries <repo>/artifacts, then asks git for the common .git directory (shared by every worktree of
  the same clone) and tries artifacts/ next to that. If neither exists, it says so - point
  --artifacts-dir at wherever your real copy lives rather than guessing further.
""";

var knownGames = new[] { "n100f", "bfbb", "incredibles", "tssm", "rotu", "ratatouille" };

try
{
    if (args.Length == 0 || args[0] is "-h" or "--help")
    {
        Console.WriteLine(Usage);
        return 0;
    }

    string tag = args[0];
    if (tag.Length != 4)
        throw new UsageError($"<FOURCC> must be exactly 4 characters, got '{tag}' ({tag.Length}). Pad with trailing spaces and quote it, e.g. 'CAM '.");

    int dumpBytes = 96;
    int? entrySize = null;
    int index = 0;
    string[] games = knownGames;
    string? corpusDirOverride = null;
    string? artifactsDirOverride = null;

    for (int i = 1; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--dump":
                dumpBytes = ParsePositiveInt(NextArg(args, ref i), "--dump");
                break;
            case "--entry-size":
                entrySize = ParsePositiveInt(NextArg(args, ref i), "--entry-size");
                break;
            case "--index":
                index = ParsePositiveInt(NextArg(args, ref i), "--index", allowZero: true);
                break;
            case "--games":
                games = [.. NextArg(args, ref i).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
                foreach (var g in games)
                    if (!knownGames.Contains(g))
                        throw new UsageError($"unknown game '{g}'. Known: {string.Join(", ", knownGames)}");
                break;
            case "--corpus-dir":
                corpusDirOverride = NextArg(args, ref i);
                break;
            case "--artifacts-dir":
                artifactsDirOverride = NextArg(args, ref i);
                break;
            default:
                throw new UsageError($"unknown option '{args[i]}'.");
        }
    }

    string repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ScriptPath())!, "..", "..", "..", ".."));
    string corpusDir = corpusDirOverride ?? Path.Combine(repoRoot, "corpus");
    var (artifactsDir, triedPaths) = artifactsDirOverride is not null
        ? (Directory.Exists(artifactsDirOverride) ? artifactsDirOverride : null, new[] { artifactsDirOverride })
        : FindArtifactsDir(repoRoot);

    Console.WriteLine($"locate-asset — '{tag}'");
    bool anyHit = false;
    foreach (var game in games)
    {
        string corpusPath = Path.Combine(corpusDir, $"{game}.json");
        Console.WriteLine();
        if (!File.Exists(corpusPath))
        {
            Console.WriteLine($"[{game}] corpus/{game}.json not found");
            continue;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(corpusPath));
        var root = document.RootElement;
        if (!root.TryGetProperty("fields", out var fields)
            || !fields.TryGetProperty("AssetHeader.Type", out var typeField)
            || typeField.GetProperty("kind").GetString() != "set"
            || !typeField.GetProperty("values").TryGetProperty(tag, out var occurrence))
        {
            Console.WriteLine($"[{game}] not observed in corpus");
            continue;
        }

        anyHit = true;
        long count = occurrence.GetProperty("count").GetInt64();
        int buildCount = occurrence.GetProperty("builds").GetArrayLength();
        string exemplar = occurrence.GetProperty("exemplar").GetString()!;
        Console.WriteLine($"[{game}] corpus: {count} occurrence(s) across {buildCount} build(s)");
        Console.WriteLine($"  exemplar: {exemplar}");

        if (artifactsDir is null)
        {
            Console.WriteLine("  (artifacts/ not found - see below for the archive bytes)");
            continue;
        }

        string archivePath = Path.Combine(artifactsDir, exemplar.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(archivePath))
        {
            Console.WriteLine($"  archive not found on disk: {archivePath}");
            continue;
        }

        DumpArchive(archivePath, tag, index, dumpBytes, entrySize);
    }

    Console.WriteLine();
    if (!anyHit)
    {
        Console.WriteLine($"'{tag}' was not observed in any checked corpus.");
    }
    else if (artifactsDir is null)
    {
        Console.Error.WriteLine("error: artifacts/ not found at " + string.Join(" or ", triedPaths.Select(p => $"'{p}'")) + ".");
        Console.Error.WriteLine("  If you're in a worktree, artifacts/ isn't cloned into it. Point --artifacts-dir at your");
        Console.Error.WriteLine("  main checkout's artifacts/ folder, or tell the user it's missing rather than guessing further.");
        return 1;
    }

    return 0;
}
catch (UsageError ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 2;
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"error: a corpus inventory is not valid JSON: {ex.Message}");
    return 1;
}

static string ScriptPath([CallerFilePath] string path = "") => path;

static string NextArg(string[] args, ref int i) =>
    ++i < args.Length ? args[i] : throw new UsageError($"'{args[i - 1]}' requires a value.");

static int ParsePositiveInt(string raw, string flag, bool allowZero = false)
{
    if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || (value < 0) || (!allowZero && value == 0))
        throw new UsageError($"'{flag}' needs a positive integer, got '{raw}'.");
    return value;
}

// artifacts/ is gitignored, so a linked worktree - unlike the main checkout - never has its own
// copy. `git rev-parse --git-common-dir` resolves to the main checkout's .git directory regardless
// of which worktree you run it from, so its parent is where a shared artifacts/ actually lives.
static (string? Found, string[] Tried) FindArtifactsDir(string repoRoot)
{
    string primary = Path.Combine(repoRoot, "artifacts");
    if (Directory.Exists(primary))
        return (primary, [primary]);

    string? commonGitDir = RunGit(repoRoot, "rev-parse --git-common-dir");
    if (commonGitDir is null)
        return (null, [primary]);

    string mainRoot = Path.GetFullPath(Path.Combine(repoRoot, commonGitDir, ".."));
    string secondary = Path.Combine(mainRoot, "artifacts");
    return (Directory.Exists(secondary) ? secondary : null, [primary, secondary]);
}

static string? RunGit(string workingDirectory, string arguments)
{
    try
    {
        var info = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(info);
        if (process is null) return null;
        string output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return process.ExitCode == 0 && output.Length > 0 ? output : null;
    }
    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
    {
        return null;
    }
}

// ---------------------------------------------------------------------------------------------
// Archive reading - same AHDR field order and hex-dump shape as reading-hip-bytes' hipbytes.cs.
// ---------------------------------------------------------------------------------------------

static void DumpArchive(string archivePath, string tag, int index, int dumpBytes, int? entrySize)
{
    byte[] data = File.ReadAllBytes(archivePath);
    byte[] pattern = Encoding.ASCII.GetBytes(tag);

    var matches = new List<long>();
    for (long pos = 0; pos <= data.LongLength - pattern.Length;)
    {
        long m = IndexOf(data, pattern, pos);
        if (m < 0) break;
        matches.Add(m);
        pos = m + 1;
    }

    if (matches.Count == 0)
    {
        Console.WriteLine($"  '{tag}' bytes not found in {Path.GetFileName(archivePath)} - the corpus record may predate a re-generated archive.");
        return;
    }
    if (index >= matches.Count)
    {
        Console.WriteLine($"  --index {index} is out of range - only {matches.Count} occurrence(s) of '{tag}' found.");
        return;
    }

    long typeOffset = matches[index];
    long idOffset = typeOffset - 4;
    long tagOffset = idOffset - 8;
    bool confirmed = tagOffset >= 0 && Encoding.ASCII.GetString(data, (int)tagOffset, 4) == "AHDR";

    if (idOffset < 0 || typeOffset + 20 > data.LongLength)
    {
        Console.WriteLine($"  match {index} of {matches.Count} at 0x{typeOffset:X8} is too close to a file boundary to be an AHDR entry - probably a false positive (the tag appearing in a name or other data).");
        return;
    }

    uint id = ReadU32(data, idOffset);
    uint offset = ReadU32(data, typeOffset + 4);
    uint size = ReadU32(data, typeOffset + 8);
    uint plus = ReadU32(data, typeOffset + 12);
    uint flags = ReadU32(data, typeOffset + 16);

    string confirmation = confirmed ? "" : "  (could not confirm a preceding \"AHDR\" tag - this may be a false-positive match; verify manually)";
    Console.WriteLine($"  AHDR (match {index} of {matches.Count}): id=0x{id:X8} type={tag} offset=0x{offset:X8} size=0x{size:X8} ({size}) plus=0x{plus:X8} flags=0x{flags:X8}{confirmation}");

    if (offset >= data.LongLength)
    {
        Console.WriteLine($"  offset 0x{offset:X8} is outside the file - stopping here.");
        return;
    }

    int shown = (int)Math.Min(dumpBytes, Math.Min(size, data.LongLength - offset));
    Console.WriteLine($"  payload ({shown} of {size} bytes @ 0x{offset:X8}):");
    PrintHexDump(data, offset, shown);

    if (entrySize is int stride)
        DumpEntries(data, offset, size, stride);
}

static void DumpEntries(byte[] data, long payloadOffset, uint declaredSize, int stride)
{
    if (payloadOffset + 4 > data.LongLength)
    {
        Console.WriteLine("  --entry-size given, but the payload is too short to even hold a leading count.");
        return;
    }

    uint countRaw = ReadU32(data, payloadOffset);
    long expected = 4L + (long)countRaw * stride;
    string agreement = expected == declaredSize ? "matches" : $"does NOT match ({expected} != {declaredSize})";
    Console.WriteLine($"  leading int32 @ payload+0 = {(int)countRaw} (0x{countRaw:X8}) - as a count, 4 + count*{stride} {agreement} the declared size. Treat as a guess either way.");

    int shown = (int)Math.Clamp(countRaw, 0, 4);
    for (int e = 0; e < shown; e++)
    {
        long entryOffset = payloadOffset + 4 + (long)e * stride;
        if (entryOffset + stride > data.LongLength)
        {
            Console.WriteLine($"  entry[{e}] would read past EOF - the guessed stride is probably wrong.");
            break;
        }
        Console.WriteLine($"  entry[{e}] @ 0x{entryOffset:X8} ({stride} bytes):");
        PrintHexDump(data, entryOffset, stride);
    }
}

static uint ReadU32(byte[] data, long offset) => BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)offset, 4));

static long IndexOf(byte[] data, byte[] pattern, long start)
{
    for (long i = start; i <= data.LongLength - pattern.Length; i++)
    {
        bool match = true;
        for (int j = 0; j < pattern.Length; j++)
        {
            if (data[i + j] != pattern[j]) { match = false; break; }
        }
        if (match) return i;
    }
    return -1;
}

static void PrintHexDump(byte[] data, long offset, int length)
{
    for (int row = 0; row < length; row += 16)
    {
        int rowLen = Math.Min(16, length - row);
        var hex = new List<string>(rowLen);
        var ascii = new StringBuilder(rowLen);
        for (int col = 0; col < rowLen; col++)
        {
            byte b = data[offset + row + col];
            hex.Add(b.ToString("X2", CultureInfo.InvariantCulture));
            ascii.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
        }

        string left = string.Join(' ', hex.Take(8));
        string right = hex.Count > 8 ? string.Join(' ', hex.Skip(8)) : "";
        string hexCol = right.Length > 0 ? $"{left}  {right}" : left;
        Console.WriteLine($"    {offset + row:X8}  {hexCol,-47}  |{ascii}|");
    }
}

sealed class UsageError(string message) : Exception(message);

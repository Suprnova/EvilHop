#pragma warning disable
// hipbytes.cs — raw byte reader for .HIP/.HOP archives. See SKILL.md for usage and examples.
//
// This script knows nothing about the HIP block format. It reads bytes, and the common
// interpretations of those bytes (ASCII, big-endian integers/floats, null-terminated strings),
// at whatever offset it is pointed at. It does not parse blocks, walk children, or validate
// anything HIP-specific — that is the library's job.
//
// HUMAN WARNING: SLOP AHEAD! this was made solely for convenience and may not be the same quality
// as other files in this project. do not trust this script by default.
using System.Buffers.Binary;
using System.Globalization;
using System.Text;

const string Usage = """
hipbytes — read raw bytes from a .HIP/.HOP archive, one invocation, chainable commands.

Usage:
  dotnet run hipbytes.cs <file> [--max <n>] <command>...

Commands:
  seek <val>              Seek to an absolute or relative (+/-) offset.
  find <pat>              Search forward from the cursor for a byte/ASCII pattern.
                          Cursor lands just after the match.
  findall <pat>           Search the whole file for every occurrence. Cursor unchanged.
  tell                    Print the current cursor.
  bytes <val>             Hex-dump <val> bytes from the cursor.
  ascii <val>             Read <val> bytes as raw ASCII, no null handling (the Block.Tag case).
  str [count]             Read a null-terminated ASCII string. Repeats <count> times (default 1).
  u8|u16|u32|i32 [count]  Read a big-endian integer. Repeats <count> times (default 1).
                          Sets the $ register to the last value read.
  f32 [count]             Read a big-endian 32-bit float. Repeats <count> times (default 1).
                          Does not set $.

Values (<val>, anywhere a number is expected):
  120        decimal              0x78        hex
  +N / -N    relative to cursor (seek only; N may itself be $ or $+N/$-N)
  $          last value read by u8/u16/u32/i32
  $+N / $-N  that value, offset by a literal

Patterns (<pat>, for find/findall):
  AHDR        ASCII bytes
  0xFFFFFFFF  hex byte sequence (even digit count)

Options:
  --max <n>  Cap bytes shown by 'bytes' and matches listed by 'findall'. Default 4096.
""";

var arity = new Dictionary<string, (int Min, int Max)>
{
    ["seek"] = (1, 1),
    ["find"] = (1, 1),
    ["findall"] = (1, 1),
    ["tell"] = (0, 0),
    ["bytes"] = (1, 1),
    ["ascii"] = (1, 1),
    ["str"] = (0, 1),
    ["u8"] = (0, 1),
    ["u16"] = (0, 1),
    ["u32"] = (0, 1),
    ["i32"] = (0, 1),
    ["f32"] = (0, 1),
};

try
{
    if (args.Length == 0 || args[0] is "-h" or "--help")
    {
        Console.WriteLine(Usage);
        return 0;
    }

    string path = args[0];
    int i = 1;
    long maxBytes = 4096;
    while (i < args.Length && args[i] == "--max")
    {
        if (i + 1 >= args.Length)
            throw new UsageError($"[arg {i + 1}] '--max' requires a value");
        if (!long.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out maxBytes) || maxBytes < 1)
            throw new UsageError($"[arg {i + 2}] '--max' value must be a positive integer, got '{args[i + 1]}'");
        i += 2;
    }

    // Nothing executes until every command in the chain parses cleanly — a typo at argument
    // 20 should not first print 19 lines of output the agent might act on.
    var ops = ParseCommands(args, i, arity);

    if (!File.Exists(path))
        throw new UsageError($"file not found: '{path}'");

    byte[] data;
    try
    {
        data = File.ReadAllBytes(path);
    }
    catch (IOException ex)
    {
        throw new UsageError($"cannot read '{path}': {ex.Message}");
    }

    Console.WriteLine($"hipbytes — {Path.GetFileName(path)} ({data.LongLength:N0} bytes / 0x{data.LongLength:X})");
    Execute(ops, data, maxBytes);
    return 0;
}
catch (UsageError ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 2;
}
catch (ReadError ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 1;
}

// ---------------------------------------------------------------------------------------------
// Command parsing (validation pass — pure argv shape, no file I/O)
// ---------------------------------------------------------------------------------------------

static List<Op> ParseCommands(string[] args, int start, Dictionary<string, (int Min, int Max)> arity)
{
    var ops = new List<Op>();
    bool dollarAvailable = false;
    int i = start;

    if (i >= args.Length)
        throw new UsageError($"no commands given. Commands: {string.Join(' ', arity.Keys)}");

    while (i < args.Length)
    {
        string verb = args[i];
        int verbIndex = i + 1;
        if (!arity.ContainsKey(verb))
            throw new UsageError($"[arg {verbIndex}] unknown command '{verb}'. Commands: {string.Join(' ', arity.Keys)}");
        i++;

        var op = new Op { Verb = verb, VerbArgIndex = verbIndex };

        switch (verb)
        {
            case "seek":
            case "bytes":
            case "ascii":
            {
                if (i >= args.Length)
                    throw new UsageError($"[arg {verbIndex}] '{verb}' requires a value — one of N, 0xN, +N, -N, $, $+N/$-N");
                int valIndex = i + 1;
                var expr = ParseValueExpr(args[i], valIndex);
                if (expr.Relative && verb != "seek")
                    throw new UsageError($"[arg {valIndex}] relative values (+N/-N) are only valid for 'seek'");
                if (expr.UsesDollar && !dollarAvailable)
                    throw new UsageError($"[arg {valIndex}] '$' has no value yet; it is set by u8/u16/u32/i32 reads earlier in the chain");
                op.Value = expr;
                op.ValueArgIndex = valIndex;
                i++;
                break;
            }
            case "find":
            case "findall":
            {
                if (i >= args.Length)
                    throw new UsageError($"[arg {verbIndex}] '{verb}' requires a pattern");
                int patIndex = i + 1;
                op.Pattern = ParsePattern(args[i], patIndex);
                op.PatternArgIndex = patIndex;
                i++;
                break;
            }
            case "tell":
                break;
            default: // str, u8, u16, u32, i32, f32 — an optional repeat count
            {
                if (i < args.Length && TryParseCount(args[i], out long count))
                {
                    op.Count = count;
                    i++;
                }
                break;
            }
        }

        if (verb is "u8" or "u16" or "u32" or "i32")
            dollarAvailable = true;

        ops.Add(op);
    }

    return ops;
}

static bool TryParseCount(string token, out long count)
{
    count = 0;
    if (token.Length == 0 || token[0] is '+' or '-' or '$')
        return false;
    return token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        ? long.TryParse(token.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out count)
        : long.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out count);
}

static long ParseLiteral(string s, int argIndex, string originalToken)
{
    if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
    {
        if (s.Length == 2 || !long.TryParse(s.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
            throw new UsageError($"[arg {argIndex}] invalid hex literal '{originalToken}'");
        return hex;
    }

    if (!long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var dec))
        throw new UsageError($"[arg {argIndex}] invalid number '{originalToken}'");
    return dec;
}

static ValueExpr ParseValueExpr(string token, int argIndex)
{
    string s = token;
    bool relative = false;
    int sign = 1;
    if (s.Length > 0 && s[0] is '+' or '-')
    {
        relative = true;
        sign = s[0] == '-' ? -1 : 1;
        s = s[1..];
        if (s.Length == 0)
            throw new UsageError($"[arg {argIndex}] '{token}' is missing a value after the sign");
    }

    if (s.StartsWith('$'))
    {
        string rest = s[1..];
        long adjustment = 0;
        if (rest.Length > 0)
        {
            if (rest[0] is not ('+' or '-'))
                throw new UsageError($"[arg {argIndex}] invalid '$' expression '{token}' — expected $, $+N, or $-N");
            int adjSign = rest[0] == '-' ? -1 : 1;
            adjustment = adjSign * ParseLiteral(rest[1..], argIndex, token);
        }

        return new ValueExpr(relative, sign, true, adjustment, 0);
    }

    return new ValueExpr(relative, sign, false, 0, ParseLiteral(s, argIndex, token));
}

static byte[] ParsePattern(string token, int argIndex)
{
    if (token.Length == 0)
        throw new UsageError($"[arg {argIndex}] pattern cannot be empty");

    if (!token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        return Encoding.ASCII.GetBytes(token);

    string hex = token[2..];
    if (hex.Length == 0 || hex.Length % 2 != 0)
        throw new UsageError($"[arg {argIndex}] hex pattern '{token}' must have an even number of digits");

    var bytes = new byte[hex.Length / 2];
    for (int b = 0; b < bytes.Length; b++)
    {
        if (!byte.TryParse(hex.AsSpan(b * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[b]))
            throw new UsageError($"[arg {argIndex}] invalid hex pattern '{token}'");
    }

    return bytes;
}

// ---------------------------------------------------------------------------------------------
// Execution
// ---------------------------------------------------------------------------------------------

static void Execute(List<Op> ops, byte[] data, long maxBytes)
{
    long cursor = 0;
    long? register = null;

    foreach (var op in ops)
    {
        switch (op.Verb)
        {
            case "seek":
            {
                long target = ResolveValue(op.Value!.Value, cursor, register);
                CheckOffset(target, data.LongLength, op.ValueArgIndex, "seek");
                cursor = target;
                Print(cursor, "seek", "", "");
                break;
            }
            case "tell":
                Print(cursor, "tell", "", "");
                break;
            case "find":
            {
                long match = IndexOf(data, op.Pattern!, cursor);
                if (match < 0)
                    throw new ReadError($"[arg {op.PatternArgIndex}] find \"{Describe(op.Pattern!)}\": no match between 0x{cursor:X8} and EOF");
                cursor = match + op.Pattern!.Length;
                Print(match, "find", $"\"{Describe(op.Pattern)}\"", $"→ cursor 0x{cursor:X8}");
                break;
            }
            case "findall":
            {
                var matches = new List<long>();
                long pos = 0;
                while (pos <= data.LongLength - op.Pattern!.Length)
                {
                    long m = IndexOf(data, op.Pattern, pos);
                    if (m < 0) break;
                    matches.Add(m);
                    pos = m + Math.Max(1, op.Pattern.Length);
                }

                int shown = (int)Math.Min(matches.Count, maxBytes);
                Console.WriteLine($"0x{cursor:X8}  findall   \"{Describe(op.Pattern)}\"  found {matches.Count}");
                foreach (var m in matches.Take(shown))
                    Console.WriteLine($"    0x{m:X8}");
                if (shown < matches.Count)
                    Console.WriteLine($"    … truncated ({matches.Count - shown} more), raise with --max");
                break;
            }
            case "bytes":
            {
                long len = ResolveValue(op.Value!.Value, cursor, register);
                CheckLength(cursor, len, data.LongLength, op.ValueArgIndex, "bytes");
                long shown = Math.Min(len, maxBytes);
                PrintHexDump(cursor, data, (int)shown, len);
                cursor += len;
                break;
            }
            case "ascii":
            {
                long len = ResolveValue(op.Value!.Value, cursor, register);
                CheckLength(cursor, len, data.LongLength, op.ValueArgIndex, "ascii");
                string text = Encoding.ASCII.GetString(data, (int)cursor, (int)len);
                Print(cursor, $"ascii[{len}]", $"\"{text}\"", "");
                cursor += len;
                break;
            }
            case "str":
            {
                for (long n = 0; n < op.Count; n++)
                {
                    long start = cursor;
                    while (cursor < data.LongLength && data[cursor] != 0) cursor++;
                    if (cursor == data.LongLength)
                        throw new ReadError($"[arg {op.VerbArgIndex}] str at 0x{start:X8}: reached EOF before a null terminator");

                    long len = cursor - start;
                    string text = Encoding.ASCII.GetString(data, (int)start, (int)len);

                    long nullStart = cursor;
                    while (cursor < data.LongLength && data[cursor] == 0) cursor++;
                    long nulls = cursor - nullStart;

                    Print(start, "str", $"\"{text}\"", $"len={len} nulls={nulls}");
                }
                break;
            }
            case "u8": ReadIntegers(op, data, 1, signed: false, ref cursor, ref register); break;
            case "u16": ReadIntegers(op, data, 2, signed: false, ref cursor, ref register); break;
            case "u32": ReadIntegers(op, data, 4, signed: false, ref cursor, ref register); break;
            case "i32": ReadIntegers(op, data, 4, signed: true, ref cursor, ref register); break;
            case "f32": ReadFloats(op, data, ref cursor); break;
        }
    }
}

static void ReadIntegers(Op op, byte[] data, int size, bool signed, ref long cursor, ref long? register)
{
    for (long n = 0; n < op.Count; n++)
    {
        long start = cursor;
        CheckLength(cursor, size, data.LongLength, op.VerbArgIndex, op.Verb);

        long raw = size switch
        {
            1 => data[cursor],
            2 => BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan((int)cursor, 2)),
            4 => BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)cursor, 4)),
            _ => throw new InvalidOperationException($"unsupported integer size {size}"),
        };
        long value = signed ? unchecked((int)(uint)raw) : raw;

        register = value;
        cursor += size;
        string hex = raw.ToString("X" + (size * 2), CultureInfo.InvariantCulture);
        Print(start, op.Verb, value.ToString(CultureInfo.InvariantCulture), $"0x{hex}");
    }
}

static void ReadFloats(Op op, byte[] data, ref long cursor)
{
    for (long n = 0; n < op.Count; n++)
    {
        long start = cursor;
        CheckLength(cursor, 4, data.LongLength, op.VerbArgIndex, "f32");
        var span = data.AsSpan((int)cursor, 4);
        float value = BinaryPrimitives.ReadSingleBigEndian(span);
        uint bits = BinaryPrimitives.ReadUInt32BigEndian(span);
        cursor += 4;
        Print(start, "f32", value.ToString("G", CultureInfo.InvariantCulture), $"0x{bits:X8}");
    }
}

static long ResolveValue(ValueExpr expr, long cursor, long? register)
{
    long baseValue = expr.UsesDollar
        ? (register ?? throw new ReadError("'$' has no value yet")) + expr.DollarAdjustment
        : expr.Literal;
    return expr.Relative ? cursor + expr.Sign * baseValue : baseValue;
}

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

static string Describe(byte[] pattern) =>
    pattern.All(b => b is >= 0x20 and < 0x7F)
        ? Encoding.ASCII.GetString(pattern)
        : "0x" + Convert.ToHexString(pattern);

static void CheckLength(long cursor, long len, long dataLength, int argIndex, string op)
{
    if (len < 0)
        throw new ReadError($"[arg {argIndex}] {op} at 0x{cursor:X8}: length {len} is negative");
    long remaining = dataLength - cursor;
    if (len > remaining)
        throw new ReadError($"[arg {argIndex}] {op} at 0x{cursor:X8}: needs {len} bytes, only {remaining} remain before EOF (0x{dataLength:X8})");
}

static void CheckOffset(long target, long dataLength, int argIndex, string op)
{
    if (target < 0 || target > dataLength)
        throw new ReadError($"[arg {argIndex}] {op}: target {FormatOffset(target)} is outside the file (0x00000000–0x{dataLength:X8})");
}

static string FormatOffset(long value) => value < 0 ? value.ToString(CultureInfo.InvariantCulture) : $"0x{value:X8}";

static void Print(long offset, string op, string value, string note)
{
    string line = $"0x{offset:X8}  {op,-10}{value}";
    if (!string.IsNullOrEmpty(note))
        line += $"  {note}";
    Console.WriteLine(line);
}

static void PrintHexDump(long cursor, byte[] data, int shown, long total)
{
    Console.WriteLine($"0x{cursor:X8}  bytes[{total}]");
    for (int row = 0; row < shown; row += 16)
    {
        int rowLen = Math.Min(16, shown - row);
        var hex = new List<string>(rowLen);
        var ascii = new StringBuilder(rowLen);
        for (int col = 0; col < rowLen; col++)
        {
            byte b = data[cursor + row + col];
            hex.Add(b.ToString("X2"));
            ascii.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
        }

        string left = string.Join(' ', hex.Take(8));
        string right = hex.Count > 8 ? string.Join(' ', hex.Skip(8)) : "";
        string hexCol = right.Length > 0 ? $"{left}  {right}" : left;
        Console.WriteLine($"  {cursor + row:X8}  {hexCol,-47}  |{ascii}|");
    }

    if (shown < total)
        Console.WriteLine($"  … truncated ({total - shown} more), raise with --max");
}

sealed class UsageError(string message) : Exception(message);

sealed class ReadError(string message) : Exception(message);

sealed class Op
{
    public required string Verb { get; init; }
    public required int VerbArgIndex { get; init; }
    public ValueExpr? Value { get; set; }
    public int ValueArgIndex { get; set; }
    public byte[]? Pattern { get; set; }
    public int PatternArgIndex { get; set; }
    public long Count { get; set; } = 1;
}

readonly record struct ValueExpr(bool Relative, int Sign, bool UsesDollar, long DollarAdjustment, long Literal);

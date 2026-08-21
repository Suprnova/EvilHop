---
name: reading-hip-bytes
description: Use this skill to read the raw bytes of a .HIP or .HOP archive — hex dumps, big-endian integers, 4-character tags, null-terminated strings at a given offset, or searching for a byte/ASCII pattern.
---

# Reading raw HIP/HOP bytes

`hipbytes.cs` reads what is actually on disk in a `.HIP`/`.HOP` archive: hex dumps, ASCII, and
big-endian numbers, at any offset, in one invocation. It knows nothing about the HIP block format —
it does not walk children, validate anything, or know what a `Package` or `AssetHeader` is. Use it to
check what the bytes *are*; use the library (or the
[wiki](https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format))) for what they *mean*.

**Use this when you need to:**
- Confirm a block's tag/size at a known offset, or find a tag you don't know the offset of.
- Check whether `EvilString`'s 1-or-2-null padding rule holds on real data.
- Follow an offset field (e.g. `AHDR.Offset`) to the bytes it points at.
- Sanity-check a round-tripped archive against the original, byte for byte.

**Don't use this to:** reimplement block parsing (that belongs in the library, see
[§ Extending](#extending)), or write/patch bytes (this tool is read-only).

## Finding a file to read

If the user gave you an explicit path, use it and skip this section.

Otherwise, look for an `artifacts/` folder at the repository root. It holds real game builds used for
manual testing — several GB, gitignored, and **not guaranteed to exist**: anyone who clones the repo
without also fetching their own copy simply won't have it. Its layout, in full at `tests/README.md`, is:

```
artifacts/{game}/{build}/{platform}/{region}/{language}/
```

| Segment | Meaning |
| --- | --- |
| `{game}` | `n100f`, `bfbb`, `tssm`, `incredibles`, `rotu`, `rat` |
| `{build}` | `release` (optionally suffixed `_r{n}` for a revision) or `prototype_YYYY-MM-DD` |
| `{platform}` | `GC`, `PC`, `P2`, `XB` |
| `{region}` | `NTSC-U`, `PAL`, `NTSC-J` |
| `{language}` | language code(s) — `DE`, `FR`, `JP`, `NL`, `UK`, `US` — hyphen-joined and alphabetized for multi-language builds |

If `artifacts/` doesn't exist, **stop and tell the user** instead of guessing a path or substituting some
other file — this tool only ever reads the file it's pointed at, and a wrong guess here silently
answers a different question than the one asked.

### Quick reference: known files per release

Every `release/{platform}/{region}/{language}/` directory has the same shape: a handful of global
`.HIP` files sitting directly in it, plus one subdirectory per level (2-character code, casing varies)
holding that level's own `.HIP`/`.HOP` files. You don't need to `find`/`ls` any of this — guess the path
below and read it; a wrong guess just fails fast (see [Errors](#errors-and-exit-codes)), which is far
cheaper than listing a multi-GB tree first. Only fall back to listing once a specific guess 404s, and
even then scope it to the one subdirectory you were guessing in, not the whole archive.

`boot.HIP` is the one file guaranteed to exist for every game — it's the executable's entry point.
`font.HIP` and `plat.HIP` sit next to it (all three, except `plat.HIP` on `n100f`). Paths below are
relative to `artifacts/`, using the most complete locale on disk for each game (usually `NTSC-U/US`):

| Game | Path to boot | Notes |
| --- | --- | --- |
| `bfbb` | `bfbb/release/GC/NTSC-U/US/boot.HIP` | also `PAL/UK` |
| `incredibles` | `incredibles/release/GC/NTSC-U/US/BOOT.HIP` | also `NTSC-J/JP`, `PAL/NL`, `PAL/UK` |
| `n100f` | `n100f/release/GC/NTSC-U/US/boot.HIP` | also `NTSC-U/US-r1` (a revision), `PAL/UK`; no `plat.HIP` |
| `rat` | `rat/prototype_2006-01-18/GC/NTSC-U/US/boot.HIP` | never had a `release` build — Heavy Iron's prototype is the only build |
| `rotu` | `rotu/release/GC/NTSC-U/US/BOOT.HIP` | also `NTSC-J/JP`, `PAL/DE-FR`, `PAL/FR-UK` |
| `tssm` | `tssm/release/GC/NTSC-U/US/boot.HIP` | also `PAL/FR-NL`, `PAL/FR-UK` |

Level files live one directory down, named `{code}{NN}.HIP` (`{code}{NNN}.HIP` for `n100f`, which pads
to 3 digits) inside a directory named after the code — e.g. `bb/bb01.HIP` (bfbb), `BM/bm01.HIP`
(incredibles), `B0/b001.HIP` (n100f). The main menu is always the `mn`/`MN` directory. If you need a
specific level and don't already know its code, that's the one case worth a directory listing — just
scope it to that game's locale directory, not the whole tree.

Some `prototype_YYYY-MM-DD` directories exist as empty placeholders (reserved for artifacts not yet
supplied) — an empty dir isn't a bug, just don't expect files under it.

## Invocation

```
dotnet run --file .claude/skills/reading-hip-bytes/scripts/hipbytes.cs -- <file> [--max <n>] <command>...
```

Commands are plain tokens executed left to right against a cursor that starts at offset 0. Chain as
many as you need in one call — that's the point:

```
dotnet run --file .claude/skills/reading-hip-bytes/scripts/hipbytes.cs -- SB01.HIP seek 0 ascii 4 u32 ascii 4 u32
```

## Commands

| Command | Cursor after | Output |
| --- | --- | --- |
| `seek <val>` | at `val` | new cursor |
| `find <pat>` | just after the match | match offset, new cursor |
| `findall <pat>` | unchanged | total count + each match offset (capped by `--max`) |
| `tell` | unchanged | current cursor |
| `bytes <val>` | advanced by `val` | hex dump, 16 bytes/row, ASCII gutter (capped by `--max`) |
| `ascii <val>` | advanced by `val` | exactly `val` bytes as raw ASCII — no null handling (the `Block.Tag` case) |
| `str [count]` | past the string(s) and their nulls | text, `len=`, `nulls=` — repeats `count` times, default 1 |
| `u8` / `u16` / `u32` / `i32` `[count]` | advanced | decimal + hex; repeats `count` times; sets `$` to the last value read |
| `f32 [count]` | advanced | decimal + hex of the bit pattern; repeats `count` times; does **not** set `$` |

All integer/float reads are **big-endian**, unconditionally — that's what every HIP container and
every asset payload observed to date uses (see `EvilInt`).

`find` landing *after* the match means repeated `find AHDR` iterates matches instead of sticking, and
a tag search leaves the cursor exactly on the size field that follows it — which is the layout of
every block in the format.

## Values

Anywhere a number is expected (`seek`'s target, `bytes`/`ascii`'s length, a repeat `count`):

| Form | Meaning |
| --- | --- |
| `120` | decimal |
| `0x78` | hex |
| `+N` / `-N` | relative to the cursor — **`seek` only** |
| `$` | the last value read by `u8`/`u16`/`u32`/`i32` |
| `$+N` / `$-N` | that value, offset by a literal |

`N` inside a relative or `$`-adjustment form can itself be `$`: `seek +$` means "jump the cursor
forward by the value in `$`" (e.g. skip a block's data using its size field), while `seek $+8` means
"jump to the absolute value in `$`, plus 8". Repeat counts only accept plain decimal/hex literals — no
`$`, no sign.

## Patterns

For `find`/`findall`:

| Form | Meaning |
| --- | --- |
| `AHDR` | ASCII bytes |
| `0xFFFFFFFF` | hex byte sequence (must have an even digit count) |

## Options

| Option | Default | Effect |
| --- | --- | --- |
| `--max <n>` | `4096` | Caps bytes shown by `bytes` and matches listed by `findall`. The full read/search still happens (the cursor still advances by the real length; the reported match count is still the true count) — only the printed detail is capped, so a fat-fingered `bytes 2000000` can't flood your context. |

## Worked examples

Offsets below are illustrative — re-derive them against the archive you're actually looking at.

| Task | Chain |
| --- | --- |
| Confirm the archive header | `seek 0 ascii 4 u32 ascii 4 u32` |
| Walk one block header (tag + size) at the cursor | `ascii 4 u32` |
| Skip a block's data to reach its sibling | `ascii 4 u32 seek +$` |
| Find every asset header | `findall AHDR` |
| Read an `AHDR`'s id / type / offset / size | `find AHDR seek +4 u32 ascii 4 u32 u32` |
| Follow `AHDR.Offset` to the asset payload | `find AHDR seek +16 u32 seek $ bytes 64` |
| Check `EvilString`'s 1-vs-2-null rule on real data | `find ADBG seek +12 str 2` |
| Read a run of floats from asset data | `seek 0x12F40 f32 6` |
| Read 32 bytes as eight `u32`s | `seek 0x100 u32 8` |

## Reading the output

```
hipbytes — SB01.HIP (2,214,320 bytes / 0x21C3F0)
0x00000000  ascii[4]  "HIPA"
0x00000004  u32       0  0x00000000
0x00000008  ascii[4]  "PACK"
0x0000000C  u32       68  0x00000044
0x00000010  find      "ADBG"  → cursor 0x000004CC
0x000004CC  u32       24  0x00000018
0x000004D0  str       "trk_gate_01"  len=11 nulls=1
0x000004E0  bytes[24]
  000004E0  00 00 00 04 54 52 4B 47  61 74 65 00 00 00 00 01  |....TRKGate.....|
  000004F0  DE AD BE EF 00 00 00 00                           |........|
```

Every line is prefixed with the offset the operation *started* at, so any line can be turned back into
a `seek`. Integers always print decimal and hex side by side.

## Errors and exit codes

Two kinds of failure, both a single `error: ...` line on stderr — never a stack trace:

- **Exit `2`** — a usage problem (unknown command, wrong arity, malformed literal, `$` referenced
  before any value has been read, missing file). Caught during an up-front validation pass, before
  anything runs — a typo anywhere in the chain means **zero** lines of output, not a partial run.
- **Exit `1`** — a read failure during execution (out of bounds, pattern not found, unterminated
  string). Everything printed before the failing command stays on stdout — a chain that dies at step 9
  still leaves you steps 1–8.
- **Exit `0`** — success.

```
error: [arg 7] u32 at 0x0021C3EE: needs 4 bytes, only 2 remain before EOF (0x0021C3F0)
error: [arg 4] unknown command 'unit'. Commands: seek find findall tell bytes ascii str u8 u16 u32 i32 f32
error: [arg 9] 'seek' requires a value — one of N, 0xN, +N, -N, $, $+N/$-N
error: [arg 5] '$' has no value yet; it is set by u8/u16/u32/i32 reads earlier in the chain
error: [arg 2] find "ADBG": no match between 0x00001000 and EOF
error: file not found: 'SB01.HIP'
```

## Extending

If you needed two or more invocations to answer one question, consider adding a command:

1. Add a `case` to the `switch` in `Execute` (and `ParseCommands` if it needs its own argument shape).
2. Add its arity to the `arity` dictionary at the top of the script — this drives both validation and
   the `--help` text.
3. Add a row to the command table above.

Keep new commands generic byte operations — reading, seeking, searching. Anything that encodes HIP
structure (which blocks have children, what a tag means, asset layout) belongs in the library, not
here.

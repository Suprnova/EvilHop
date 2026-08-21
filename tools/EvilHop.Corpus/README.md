# EvilHop.Corpus

A maintainer-only tool that reads real HIP archives from a local corpus, observes what EvilHop's
assumptions actually look like against them, and emits a small committed inventory so CI can check
those assumptions without needing the corpus itself.

## Prerequisites

You supply your own corpus, from your own legal dumps - nothing here is distributed. The expected
layout is `<root>/<build>/<platform>/<region>/<language>/**/*.HIP`, e.g.:

```
artifacts/
  n100f/
    release/GC/NTSC-U/US/boot.HIP
    release/GC/NTSC-U/US/B0/b001.HIP
    ...
  bfbb/
    release/GC/NTSC-U/US/boot.HIP
    ...
```

`artifacts/` at the repo root is gitignored and never read by anything except this tool, run
manually.

## Usage

```
dotnet run --project tools/EvilHop.Corpus -- inventory --out corpus/n100f.json artifacts/n100f
dotnet run --project tools/EvilHop.Corpus -- verify artifacts/n100f artifacts/bfbb
```

- `<root>...` - one or more corpus roots. Pass only the games you want: `artifacts/n100f
  artifacts/bfbb` inventories those two and leaves the rest alone.
- `--out` - the inventory output path. Required for `inventory`.
- `--serializer` - which game to read with, a case-insensitive `GameVersion` key (`n100f`, `bfbb`,
  `incredibles`, `tssm`, `rotu`, `ratatouille`). Defaults to `n100f`. Every `GameVersion` has a
  serializer today.
- `--dump <path>` - also writes a full-fidelity, gitignored JSONL dump alongside the inventory (see
  below).
- `--round-trip` - `verify`-only. Additionally writes each parsed archive back out and diffs it
  against the original file's bytes, failing any archive that doesn't match exactly. The strongest
  available check of the library's round-trip fidelity claim, run against real archives instead of
  hand-built fixtures - see `docs/Serializer Writing Design.md` §1. Off by default: it roughly doubles
  per-archive memory and time, and plain `verify` still answers "does everything under this root
  parse" on its own.

`verify` parses every archive and reports failures with a non-zero exit code, without writing
anything (unless `--round-trip` is passed, which writes only to an in-memory buffer for comparison).
It's the fast way to check "does everything under this root still parse" before spending the time on
a full `inventory` run - and the only way to point the tool at a root whose game has no serializer
yet, or whose bytes don't match the serializer's assumptions, without it aborting the whole run
partway through.

Both a missing root and a root with no `.HIP`/`.HOP` files are hard errors, not silent skips - the
tool never runs unattended under a test suite, so there's no reason to make a bad argument quietly
succeed.

## Build profile overrides

`BuildProfiles.json`, committed alongside this tool, lists path-prefix-matched overrides to a game's
default `FormatProfile` - e.g. N100F's `prototype_2001-06-11` build, which omits `StreamData`'s
padding-amount field that every other N100F build has. Both `verify` and `inventory` apply it
automatically, matching each discovered archive's relative path against the manifest's entries
(first match wins) before resolving a serializer for it.

It's committed here, not derived from `artifacts/`, because `artifacts/` is gitignored and rebuilt
per contributor - a quirk discovered against one contributor's corpus would otherwise be lost the
next time someone else regenerates it. `src/EvilHop` itself has no equivalent lookup table; a library
consumer with one odd file constructs `new N100FSerializer(profile with { StreamDataHasPaddingField =
false })` directly instead.

## What is committed

`corpus/*.json` is committed and reviewed like any other source file - it's the small, shareable
statement of what the 12GB corpus actually contains. `--dump` output is gitignored and local; it
exists for tracing a surprising aggregated value back to every file it appears in.

## When to regenerate

Regenerate when the corpus itself changes (new builds added) or when extraction/invariant policy
changes in this tool. Routine changes to the `EvilHop` library do **not** require regeneration -
extraction is reflection-based, so it already reads whatever properties exist; only its *assertions*
against the inventory (in `EvilHop.Tests`) can start failing, which is the point.

## The governing rule

**This tool records observations. `EvilHop.Tests` asserts those observations against EvilHop's
current code.** Concretely: this tool writes the raw enum value `0x52575458`, never `"isDefined":
true`. Enum definitions, hash implementations, and property names are mutable code, so their
assertions belong where code changes are actually caught - CI - not baked into data that only
changes when a maintainer with a 12GB corpus feels like it. Do not add `Enum.IsDefined` or similar
code-dependent checks here.

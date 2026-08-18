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
dotnet run --project tools/EvilHop.Corpus -- inventory --out corpus/v1.json artifacts/n100f
dotnet run --project tools/EvilHop.Corpus -- verify artifacts/n100f artifacts/bfbb
```

- `<root>...` - one or more corpus roots. Pass only the games you want: `artifacts/n100f
  artifacts/bfbb` inventories those two and leaves the rest alone.
- `--out` - the inventory output path. Required for `inventory`.
- `--serializer` - which serializer to read with. Defaults to `v1`, currently the only one that
  exists; becomes an override once a `FileFormatFactory` can auto-detect a version per archive.
- `--dump <path>` - also writes a full-fidelity, gitignored JSONL dump alongside the inventory (see
  below).

`verify` parses every archive and reports failures with a non-zero exit code, without writing
anything. It's the fast way to check "does everything under this root still parse" before spending
the time on a full `inventory` run - and the only way to point the tool at archives no current
serializer can read (e.g. `artifacts/bfbb` under `--serializer v1`) without it aborting the whole run.

Both a missing root and a root with no `.HIP`/`.HOP` files are hard errors, not silent skips - the
tool never runs unattended under a test suite, so there's no reason to make a bad argument quietly
succeed.

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

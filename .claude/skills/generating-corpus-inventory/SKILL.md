---
name: generating-corpus-inventory
description: Use this skill to run the EvilHop.Corpus tool — regenerating a committed corpus inventory (corpus/*.json) from real archives in artifacts/, verifying that a set of archives still parses (optionally round-trips byte-for-byte), or producing a full-fidelity JSONL dump. Covers both the inventory and verify verbs and what to check before committing.
---

# Generating a corpus inventory

`tools/EvilHop.Corpus` reads real HIP archives from a local corpus and writes a small committed
JSON inventory recording what those archives actually contain. The corpus is multi-GB and cannot be
shared; the inventory can, which is how CI checks our assumptions against reality without the files.

To *read* an existing inventory, use `reading-corpus-inventory` instead — you almost never need to
regenerate one just to answer a question.

**Use this when you need to:**
- Refresh `corpus/n100f.json` after adding builds to the corpus or changing extraction/invariant policy.
- Check whether every archive under a path still parses, after touching serializer code.
- Check whether every archive under a path still **round-trips byte-for-byte** (`--round-trip`), after
  touching serializer read *or write* code — the strongest available check of the library's round-trip
  fidelity claim, run against real archives instead of hand-built fixtures.
- Produce a `--dump` to trace an aggregated value back to every file it appears in.

**Don't use this to:** answer a question the committed inventory already answers, or as part of a
build or test run — nothing in CI reads `artifacts/`, and the full test suite must pass without it.

## Prerequisites

`artifacts/` at the repository root holds the corpus. It is **gitignored, user-supplied from their
own legal dumps, and not guaranteed to exist** — anyone who clones without supplying their own copy
simply won't have it. Expected layout:

```
artifacts/{game}/{build}/{platform}/{region}/{language}/**/*.HIP
```

| Segment | Values |
| --- | --- |
| `{game}` | `n100f`, `bfbb`, `tssm`, `incredibles`, `rotu`, `rat` |
| `{build}` | `release` (optionally `_r{n}`) or `prototype_YYYY-MM-DD` |
| `{platform}` | `GC`, `PC`, `P2`, `XB` |
| `{region}` | `NTSC-U`, `PAL`, `NTSC-J` |
| `{language}` | `DE`, `FR`, `JP`, `NL`, `UK`, `US`, hyphen-joined and alphabetized when multiple |

**If `artifacts/` is absent, stop and tell the user** rather than generating from a partial or
substituted corpus — a quietly incomplete inventory is worse than none, because it looks authoritative.

Discovery is recursive and case-insensitive for `.HIP`/`.HOP` (Incredibles and ROTU ship `BOOT.HIP`
uppercase). Directories deeper than the five build segments — per-level folders like `B0/` — fold into
their nearest build key rather than becoming builds of their own.

## Invocation

```
dotnet run --project tools/EvilHop.Corpus -c Release -- verify [--serializer <game>] [--round-trip] <root>...
dotnet run --project tools/EvilHop.Corpus -c Release -- inventory --out <path> [--serializer <game>] [--dump <path>] <root>...
```

Use `-c Release`. The tool is I/O- and parse-bound over gigabytes, and a Debug build is meaningfully
slower for no benefit.

| Element | Behavior |
| --- | --- |
| `<root>...` | One or more corpus roots. Pass only what you want: `artifacts/n100f artifacts/bfbb` covers those two and ignores the rest. |
| `--out <path>` | Inventory output path. **Required** for `inventory`. |
| `--serializer <game>` | Which game reads the archives, a case-insensitive `GameVersion` key (`n100f`, `bfbb`, `incredibles`, `tssm`, `rotu`, `ratatouille`). Defaults to `n100f`. `n100f` and `bfbb` are implemented today. |
| `--dump <path>` | Also write full-fidelity JSONL, one record per archive. Gitignored. |
| `--round-trip` | `verify`-only. Also writes each parsed archive back out (to an in-memory buffer, nothing touches disk) and diffs it against the original file's bytes. Off by default — it roughly doubles per-archive memory and time. |

A missing root, and a root containing no archives, are both hard errors rather than silent skips —
the tool is always run deliberately by a human, so a bad argument should fail loudly.

## Verify first

`verify` parses everything and reports failures without writing anything. Run it before any
`inventory` run:

```
dotnet run --project tools/EvilHop.Corpus -c Release -- verify artifacts/n100f
  → 1038/1038 archives parsed successfully.
```

It matters because **`inventory` aborts on the first unparseable archive**, and discovering that
forty minutes into a multi-GB run wastes real time. `verify` also has no `--out`, so it is the only
safe way to point the tool at a root whose bytes don't match the profile in use, without it aborting
the whole run.

Exit codes: `0` when everything parsed, `1` when anything failed, with one `FAIL <path>: <reason>`
line per failure.

### Round-trip checking

`--round-trip` reads each archive, writes it back out to an in-memory buffer, and diffs that buffer
against the original file's bytes — nothing is written to disk. It's the real-corpus counterpart to
`SerializerContractTests.Read_ThenWrite_MinimalFixture_ProducesIdenticalBytes`
(`docs/Serializer Writing Design.md` §1, §7): that test proves the claim against one hand-built
fixture per serializer, this proves it against every real archive under a root.

```
dotnet run --project tools/EvilHop.Corpus -c Release -- verify --round-trip artifacts/n100f
  → 1038/1038 archives parsed successfully.
dotnet run --project tools/EvilHop.Corpus -c Release -- verify --serializer bfbb --round-trip artifacts/bfbb
  → 264/264 archives parsed successfully.
```

A round-trip mismatch reports as a normal `FAIL <path>: round-trip byte mismatch.` line alongside any
parse failures — the summary count and exit code don't distinguish "didn't parse" from "parsed but
didn't round-trip," so check the failure lines themselves to tell which happened. Per
[Serializer Writing Design §1](../../../docs/Serializer%20Writing%20Design.md#1-what-best-effort-round-trip-fidelity-actually-means),
a round-trip failure is never "we forgot to preserve original bytes" — there's no byte capture to have
forgotten — it's always either a bug (a field the model doesn't have a home for, or a writer that
encodes something differently than its reader decoded it) or a genuine modeling gap worth recording.

Off by default because it roughly doubles per-archive memory and time; plain `verify` still answers
"does everything under this root parse" on its own, and `--round-trip` only makes sense once you're
specifically checking a serializer's write path (or a read-path change you want to be sure didn't
introduce an asymmetry with its writer).

### Only N100F and BFBB have serializers today

`N100FSerializer` and `BFBBSerializer` are the only implemented `Serializer`s; `inventory --serializer
<game>` for anything else fails immediately with "No serializer exists for `<game>` yet". **In
practice this means `artifacts/n100f` and `artifacts/bfbb` are the only roots worth inventorying right
now.**

`verify` doesn't require `--serializer` to name the root's own game — it just reads every archive
under `<root>...` with whichever profile the flag resolves to (default `n100f`). Point `--serializer`
at the root's own game (`bfbb` for `artifacts/bfbb`) for a byte-for-byte correct reading; running a
later, still-unimplemented game's root under `n100f` or `bfbb`'s profile silently misreads that game's
own quirks (a different `PLAT` field order, `DPAK`'s padding switch) rather than catching them, since
nothing checks the bytes against a "right" answer. Treat a clean `verify` run against such a root as a
lead worth recording, not proof that game is supported. Widen the roots you actually inventory only
once that game gets its own serializer.

## Generating

```
dotnet run --project tools/EvilHop.Corpus -c Release -- inventory --out corpus/n100f.json artifacts/n100f
  → Processed 1038 archives.
  → Wrote inventory to corpus/n100f.json
```

Progress prints every 100 archives. Archives are parsed one at a time and discarded — only the
accumulators persist, because a parsed corpus cannot be held in memory.

Output is deterministic: sorted keys, sorted values, numeric ordering by magnitude. **The same corpus
always produces a byte-identical file**, which is what makes the committed diff meaningful.

Per-archive quirks (e.g. N100F's `prototype_2001-06-11` build, which omits `StreamData`'s
padding-amount field) are applied automatically from the committed
`tools/EvilHop.Corpus/BuildProfiles.json` manifest, matched by path prefix — nothing to pass on the
command line for a build already listed there.

### `--dump`

```
dotnet run --project tools/EvilHop.Corpus -c Release -- inventory --out corpus/n100f.json --dump dump/n100f.jsonl artifacts/n100f
```

One JSONL record per archive with every field occurrence, uncapped and unaggregated. `dump/` is
gitignored. Use it when the aggregated inventory says a value exists in three builds and you need
every file containing it; regenerate on demand rather than keeping it around.

## Before committing

1. **Re-run and diff.** Run the same command twice and confirm the output is byte-identical. A diff
   between two identical runs means non-determinism, which is a bug in the tool.
2. **Run the test suite.** `dotnet test EvilHop.slnx -c Release`. `EvilHop.Tests/Corpus/InventoryTests.cs`
   asserts the committed inventory against current code, and it is where a newly-recorded enum value
   with no matching member surfaces.
3. **Read the diff.** `corpus/*.json` is reviewed like source. A new asset type, a widened range, or a
   newly-nonzero violation count is a finding worth understanding before it lands.
4. **Don't commit `dump/`.** It's gitignored; keep it that way.

Regenerate when the corpus gains builds, or when extraction/invariant policy changes in the tool.
Routine library changes do **not** need a refresh — extraction is reflection-based and already reads
whatever properties exist. What can break is the *assertions* in `EvilHop.Tests`, and that breaking in
CI is the design working.

## The governing rule

**The tool records observations. `EvilHop.Tests` asserts those observations against current code.**

The inventory must contain no value whose correctness depends on EvilHop's source. It records the raw
value `RWTX`, never `"isDefined": true`; the `(name, id)` pair, never `"hashMatches": true`.

The reason is *when failures surface*. The tool could trivially call `Enum.IsDefined` — but that
failure would only appear when someone runs it with the full corpus on hand. Enum definitions are
mutable code and observed values are frozen data, so the assertion belongs where code changes are
actually caught: CI. **Do not add `Enum.IsDefined` or similar code-dependent checks to the tool.**

This is the design decision a newcomer is most likely to unknowingly violate, and the tool's structure
does not make it self-evident.

## Extending

`tools/EvilHop.Corpus/README.md` covers the same ground for humans. The layout:

| Path | Holds |
| --- | --- |
| `Program.cs`, `CorpusOptions.cs` | CLI entry and argument parsing. |
| `ArchiveWalker.cs` | Discovery and build-key derivation. |
| `Extraction/` | Reflection over public block properties, and the cardinality policy. |
| `Invariants/` | One file per invariant family, plus the registry that lists them all. |
| `Output/` | Deterministic inventory JSON and the JSONL dump. |

Adding an invariant means implementing `IInvariant` and registering it in `InvariantRegistry`. Adding
a *field* takes no work at all — extraction reflects over public properties, so a new block property
is picked up on the next run.

`EvilHop.Corpus` deliberately has **no** `InternalsVisibleTo` on `EvilHop`. Its public-only view is
the point: if something is awkward to write against public EvilHop, that is a real API finding the
test project structurally cannot surface. Don't "fix" an awkward call site by widening internals.

Cover changes with tests in `tests/EvilHop.Corpus.Tests`, which uses synthetic archives built through
the public API and never depends on the corpus.

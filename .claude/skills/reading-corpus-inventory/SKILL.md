---
name: reading-corpus-inventory
description: Use this skill to answer questions from a committed corpus inventory (corpus/*.json) — which values a block field actually takes in real archives, every observed asset type, whether a field is constant, which invariants pass or fail, or which real archive contains a given value. Query it with corpusq.cs rather than reading the file.
---

# Reading a corpus inventory

`corpus/*.json` is a committed record of what real HIP archives **actually contain** — every distinct
value observed per block field, attributed per build, plus the results of hand-written invariant
checks. It is generated from a local multi-GB corpus that is **not** in the repository, so this file
is the only way to answer "what do real archives do?" without the archives.

The archives are the authoritative statement of what the HIP format is. Where the wiki, our enums, or
our validation rules disagree with the inventory, **the inventory wins** — it is a recording of
reality, not an assertion about it.

**Use this when you need to:**
- Enumerate real values of a field — every `AssetType` in use, every observed `PackFlags` combination.
- Check whether an "Always X" claim in a block class actually holds corpus-wide.
- See which invariants pass, fail, or hit cases we cannot explain.
- Find a real archive containing a specific value, to then read its bytes.
- Sanity-check a proposed enum or validation rule against reality before writing it.

**Don't use this to:** learn the byte layout of the format (that's the
[wiki](https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)) and the block classes), read
one specific archive (that's `reading-hip-bytes`), or generate/refresh an inventory (that's
`generating-corpus-inventory`).

## Never read this file directly

**Do not `cat`, `Read`, or `grep` the inventory.** It is dense machine-generated JSON, most of it
irrelevant to any one question, and it grows with every build and asset type added — today's ~37KB
covers a single game's block layer, and it is designed to hold six games and ~70 asset types.
Reading it whole burns context on data you did not ask for.

Use `corpusq.cs`. Every command prints a compact line-oriented digest, so a question costs a handful
of lines instead of the whole file.

## Invocation

```
dotnet run --file .claude/skills/reading-corpus-inventory/scripts/corpusq.cs -- [options] <command> [args]
```

Paths are relative to the repository root, and the inventory defaults to `corpus/n100f.json`. The
first run takes a few seconds to compile; later runs are fast.

## Commands

| Command | Prints |
| --- | --- |
| `summary` | Builds, field counts, and which invariants need attention. **Start here.** |
| `builds` | Each build key and its archive count. |
| `fields [pattern]` | One line per field: kind, then values or ranges. Optional case-insensitive regex. |
| `field <key>` | Full JSON for one field — every value with counts, builds, and exemplar. |
| `values <key>` | Just the observed values of one set-kind field, with counts. |
| `constants [pattern]` | Fields with exactly one observed value corpus-wide. |
| `invariants [pattern]` | One line of health per invariant. |
| `invariant <name>` | Full JSON for one invariant, including violation samples. |
| `exemplar <key> <value>` | An archive path containing that value, plus its count and spread. |
| `grep <pattern>` | Search field names, recorded values, and invariant names at once. |

| Option | Default | Effect |
| --- | --- | --- |
| `-i`, `--inventory <path>` | `corpus/n100f.json` | Which inventory to read. |
| `-n`, `--limit <n>` | `60` | Cap printed lines. `0` means unlimited. |

## Cookbook

Every command below is verified working. Prefix each with
`dotnet run --file .claude/skills/reading-corpus-inventory/scripts/corpusq.cs --`.

| Question | Command |
| --- | --- |
| What's in this inventory at all? | `summary` |
| **Every asset type used in real archives** | `values AssetHeader.Type -n 0` |
| The 10 most common asset types | `values AssetHeader.Type -n 10` |
| Which fields never vary (confirms "Always X" claims) | `constants` |
| What values does `AssetDebug.Alignment` take? | `values AssetDebug.Alignment` |
| What's the range of `AssetDebug.Name` lengths? | `fields 'AssetDebug\.Name'` |
| Everything recorded about one field | `field AssetHeader.Flags` |
| All `AssetDebug` fields at a glance | `fields AssetDebug` |
| Are any invariants failing? | `invariants` |
| Why did an invariant fail, with samples | `invariant packageMaxSizesMatchTree` |
| Asset IDs we cannot derive from their name | `invariant assetIdMatchesNameHash` |
| Which builds does this cover? | `builds` |
| Find a real archive containing texture assets | `exemplar AssetHeader.Type RWTX` |
| Where does anything mention "Layer"? | `grep Layer` |
| Which field records the value `ANIM`? | `grep '^ANIM$'` |

Two habits worth keeping:

- **`summary` first** when you don't already know the inventory's shape. It is ~8 lines and tells you
  whether anything is broken.
- **`values` before `field`.** `values` gives the answer in one line each; `field` dumps full JSON
  with per-value build lists and exemplars, which is only worth it when you need provenance.

### Going from a value to real bytes

`exemplar` closes the loop between "this value exists" and "show me it on disk":

```
exemplar AssetHeader.Type RWTX
  → n100f/release/GC/NTSC-U/US-r1/B0/b001.HIP
```

That path is relative to `artifacts/`. If `artifacts/` exists locally, hand the full path to
`reading-hip-bytes` to inspect the actual bytes. If it doesn't, the path is still the record of
*which* file the value came from.

## What the file contains

Three top-level keys.

**`builds`** — one entry per build, keyed by the corpus-relative path prefix
`{game}/{build}/{platform}/{region}/{language}` (e.g. `n100f/release/GC/NTSC-U/US`), with the number
of archives observed. Build, not game: `PackFlags` and `PackagePlatform` values genuinely vary by
platform and region *within* one game.

**`fields`** — keyed `{BlockType}.{Property}`, e.g. `AssetHeader.Type`. Each has one of two shapes,
decided by how many distinct values were seen (the cap is 64):

```json
"AssetHeader.Type": {
  "kind": "set",
  "values": {
    "RWTX": { "count": 19517, "builds": ["n100f/..."], "exemplar": "n100f/.../b001.HIP" }
  }
}
```

```json
"AssetHeader.Id": { "kind": "summary", "distinct": 21323, "min": 68, "max": 4294802053 }
```

`kind: "set"` means every distinct value is listed — the field stayed under the cap, so the recorded
set is exhaustive. `kind: "summary"` means it blew the cap and degraded to a range: `min`/`max` for
numbers, `minLength`/`maxLength` for strings, collections, and `byte[]`. **A summary field has no
value list**, and `values` will tell you so rather than printing nothing.

Enum-backed fields render as their ASCII form when every byte is printable — `AssetType` values are
on-disk FourCCs, so `Texture` appears as `RWTX` and `Animation` as `ANIM`, four characters including
any trailing space (`BSP `, `CAM `, `UI  `). Values with unprintable bytes fall back to hex
(`0x00000000`). Non-enum numbers print as plain decimal.

**`invariants`** — results of hand-written cross-field checks. **Three different shapes live here**,
which is the one real trap in the format:

| Shape | Looks like | Examples |
| --- | --- | --- |
| Usual | `checked`, `outcomes`, optional `violated` samples | most of them |
| Structural table | keyed by *block type*, each an invariant result | `structural` |
| Bare observation | a value summary (`kind`/`values`), never passes or fails | `createdDateStringTrailingWhitespace` |

Code that assumes every invariant has `checked`/`outcomes` will break on the other two. `corpusq.cs`
handles all three; hand-written JSON walking must too.

`assetIdMatchesNameHash` is classified rather than boolean, because most non-matches are expected. Its
outcomes are `direct`, `anim-suffix`, `mpht-replace`, `mpht-append`, `truncated` (name stored at the
31-character limit, so the hashed input is unrecoverable), and `unexplained`. **`unexplained > 0` is
the interesting signal** — it means a derivation rule exists that we don't know about.

## What the file deliberately does not contain

**The inventory records observations. Tests assert those observations against EvilHop's code.**

So it holds the raw value `RWTX`, never `"isDefined": true`; the `(name, id)` pair, never
`"hashMatches": true`. Enum definitions and hash implementations are mutable code, so assertions about
them live in `EvilHop.Tests` where CI catches a change — not baked into data that only refreshes when
a maintainer with the full corpus regenerates it.

Two consequences when reading:

- **Invariant `checked`/`violated` counts are as-of-generation.** They describe the code at the moment
  the inventory was written, so don't treat them as a live test result.
- **Field summaries are documentation, not assertions.** No code change can make a range wrong.

## Glossary

| Term | Meaning |
| --- | --- |
| Archive | One HIP file — a tree of blocks. |
| Block | One unit of data: a tag, a size, its fields, and its children. |
| Tag | A block's 4-character identifier on disk (`AHDR`, `PACK`, `DPAK`). |
| Asset | A logical game object assembled from blocks; has an ID, type, and name. |
| Layer | A grouping of assets by category. |
| Build key | `{game}/{build}/{platform}/{region}/{language}` — the unit observations are attributed to. |
| Field key | `{BlockType}.{Property}` — how the inventory names a field. |
| Cardinality cap | 64. Under it, every distinct value is kept; over it, the field degrades to a range. |
| Exemplar | One archive path where a value was seen, for tracing it back to bytes. |
| Invariant | A hand-written cross-field check that reflection cannot infer. |
| FourCC | A four-byte ASCII identifier stored as an integer (`ANIM` = `0x414E494D`). |

## Errors and exit codes

Errors are a single `error: ...` line on stderr, never a stack trace.

- **Exit `2`** — usage: unknown command, wrong argument count, missing inventory, unknown field or
  invariant. Unknown names suggest near matches, and asking for `values` of a summary-kind field
  reports its range instead, so the failure usually still answers the question.
- **Exit `1`** — the inventory exists but is not valid JSON.
- **Exit `0`** — success.

```
error: no field 'AssetHeader.Typ'. Did you mean: AssetHeader.Type?
error: 'AssetHeader.Id' is a summary field — it blew the cardinality cap, so no value list was kept. Its range: AssetHeader.Id  summary  distinct=21323  68..4294802053
error: inventory not found: 'corpus/nope.json'. Generate one with the generating-corpus-inventory skill.
```

## Multiple inventories

One file per corpus, identified by filename. `corpus/n100f.json` is the official-archive inventory
today; a `corpus/community.json` may exist later for community-made archives. Strictness differs by
which test asserts against which file — an official-corpus failure breaks the build, a
community-corpus one is informational. Point `--inventory` at whichever you mean.

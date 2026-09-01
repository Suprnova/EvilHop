# EvilHop

EvilHop is a C# .NET library for reading, writing, and modifying HIP archive files used in Heavy Iron Studios games. It uses .NET 10 and xUnit 3 for testing.

The library is in alpha and is not used in any production applications. Breaking changes are normal and expected. Do not worry about maintaining backward compatibility.

## Repository Layout

| Path | What it is |
|---|---|
| `src/EvilHop/` | The library. `Blocks/` (block layer), `Assets/` (asset layer), `Serialization/` (per-game serializers and `FormatProfile`), `Common/`, `Primitives/`, `Validation/`. |
| `tests/EvilHop.Tests/` | Library tests. Fixtures live in `TestData/<game>/`; nothing here reads `artifacts/`. `Inventory/` asserts today's library against yesterday's `corpus/*.json` instead. |
| `tools/EvilHop.Corpus/` | The corpus tool. Turns `artifacts/` into the committed `corpus/*.json` inventories. Carries no format knowledge of its own - only what `EvilHop.Validation` declares. |
| `tools/EvilHop.Corpus.Tests/` | The tool's own tests: map/reduce, caching, and JSON determinism. |
| `artifacts/` | Local, gitignored corpus of real game archives. |
| `corpus/` | Committed inventories generated from `artifacts/`, plus the hand-authored `manifest.json`. |
| `docs/` | Committed, durable architecture documents. The first place to look for how the project works. |

Build and test with `dotnet build` / `dotnet test` from the repository root.

## Living Documents

Start here for broad information before reading source files one by one. `docs/` holds committed,
durable architecture documents - not design docs or implementation plans, which stay local per the
global `AGENTS.md` convention. Each document describes a subsystem as it currently stands, using
relative links to the source files that back each claim, so a stale document is a broken link rather
than a plausible-sounding lie. Update the relevant document in the same change that moves the code
it describes.

- [`docs/overview.md`](docs/overview.md) - a source map of the project: the whole library, one
  level deep. Drill into it for specific details instead of exploring each file.
- [`docs/glossary.md`](docs/glossary.md) - the project's uncommon jargon, block by block and term
  by term.
- [`docs/architecture.md`](docs/architecture.md) - the design decisions behind how the library is
  built, and why.

The set is small today but will grow until the library's design is clear from `docs/` alone, without
reading code and comments.

## Rules

The foundational concepts behind these rules, and their justifications, live in
[`docs/architecture.md`](docs/architecture.md). These lines stay in `AGENTS.md` because getting
them wrong breaks the library.

- **The two layers are mutually exclusive.** Never mix the block layer (raw blocks) and the asset
  layer (logical assets) in the same operation. Asset mode is a session: `OpenAssets()` detaches and
  locks the asset-describing blocks until `Commit()`, explicit or on `Dispose()`, and a failed parse
  degrades to a diagnostic - it never throws.

- **Blocks and assets are dumb data holders.** They do not self-serialize, self-validate, derive one
  fact from another (an asset's `Id` must never be recomputed from its `Name`), or reference each
  other directly.

- **Write anything, validate optionally.** Never prevent a state that can still be serialized to a
  HIP file; surface invalidity through `Validate()` instead. Only what is physically unrepresentable
  on disk is enforced.

- **Where an asset's fields go.** Every asset has a logical surface (public properties) and a
  physical surface (`asset.Physical`); the placement criteria in
  [`docs/architecture.md`](docs/architecture.md) are prescriptive, not a per-field judgement.

- **Codec support has three states.** An asset type is **Typed**, **Payload**, or **Untyped** -
  never a binary "done or not".

- **Hand-bump revisions when you change imperative logic.** Declared fingerprints cannot see inside
  a hand-written `Check` or `Map`/`Reduce`; when you change one, bump `RuleRevision` on the rule or
  `Revision` on the facet, or stale corpus output goes silently uncorrected.

## Further Reference
- [Heavy Iron Modding Wiki](https://heavyironmodding.org)
    - [HIP Archive Format](https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format))
    - [Assets](https://heavyironmodding.org/wiki/EvilEngine/Assets)
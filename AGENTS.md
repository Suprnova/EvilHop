# EvilHop

EvilHop is a C# .NET library for reading, writing, and modifying HIP archive files used in Heavy Iron Studios games. It uses .NET 10 and xUnit 3 for testing.

The library is in alpha and is not used in any production applications. Breaking changes are normal and expected. Do not worry about maintaining backward compatibility.

## Repository Layout

| Path | What it is |
|---|---|
| `src/EvilHop/` | The library. `Blocks/` (block layer), `Assets/` (asset layer), `Serialization/` (per-game serializers and `FormatProfile`), `Common/`, `Primitives/`, `Validation/`. |
| `tests/EvilHop.Tests/` | Library tests. Fixtures live in `TestData/<game>/`; nothing here reads `artifacts/`. |
| `tools/EvilHop.Corpus/` | Console tool that reads `artifacts/` and writes `corpus/`. Depends on `EvilHop`; `EvilHop` never depends on it. |
| `tests/EvilHop.Corpus.Tests/` | Tests for the tool itself. |
| `corpus/` | Committed per-game inventories generated from real archives. |
| `artifacts/` | Local, gitignored corpus of real game archives. |
| `docs/` | Living architecture documents. See below. |

Build and test with `dotnet build` / `dotnet test` from the repository root.

## Living Documents

`docs/` holds committed, durable architecture documents - not design docs or implementation plans,
which stay local. Each document describes a subsystem as it currently stands, using relative links
to the source files that back each claim, so a stale document is a broken link rather than a
plausible-sounding lie. Update the relevant document in the same change that moves the code it
describes.

- [`docs/overview.md`](docs/overview.md) - the whole library, one level deep.
- [`docs/architecture.md`](docs/architecture.md) - the design decisions behind how the library is built, and why.
- [`docs/glossary.md`](docs/glossary.md) - every block and the asset/serialization jargon built on top of them.

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

## Corpus and Real Archives

`artifacts/` is a local, gitignored corpus of real game archives - never a build or test dependency, and the full test suite must pass without it. `tools/EvilHop.Corpus` reads it to generate small committed inventories under `corpus/`, which hermetic tests assert against.

Governing rule: **the Corpus tool records observations; tests assert them against current code.** An inventory must never contain a value whose correctness depends on EvilHop's source.

The tool and `corpus/` are currently frozen: leave them as they are rather than extending or
redesigning them until the asset layer (codecs, validation) is far enough along to make an informed
call on their shape instead of a guess.

## Further Reference
- [Heavy Iron Modding Wiki](https://heavyironmodding.org)
    - [HIP Archive Format](https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format))
    - [Assets](https://heavyironmodding.org/wiki/EvilEngine/Assets)
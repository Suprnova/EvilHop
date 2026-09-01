# EvilHop

EvilHop is a C# .NET library for reading, writing, and modifying HIP archive files used in Heavy Iron Studios games. It uses .NET 10 and xUnit 3 for testing.

The library is in alpha and is not used in any production applications. Breaking changes are normal and expected. Do not worry about maintaining backward compatibility.

## Repository Layout

| Path | What it is |
|---|---|
| `src/EvilHop/` | The library. `Blocks/` (block layer), `Assets/` (asset layer), `Serialization/` (per-game serializers and `FormatProfile`), `Common/`, `Primitives/`, `Validation/`. |
| `tests/EvilHop.Tests/` | Library tests. Fixtures live in `TestData/<game>/`; nothing here reads `artifacts/`. |
| `tools/EvilHop.Corpus/` | The corpus tool. Turns `artifacts/` into the committed `corpus/*.json` inventories. Carries no format knowledge of its own - only what `EvilHop.Validation` declares. |
| `tools/EvilHop.Corpus.Tests/` | The tool's own tests: map/reduce, caching, and JSON determinism. |
| `artifacts/` | Local, gitignored corpus of real game archives. |
| `corpus/` | Committed inventories generated from `artifacts/`, plus the hand-authored `manifest.json`. |
| `docs/` | Living architecture documents. See below. |

Build and test with `dotnet build` / `dotnet test` from the repository root.

## Living Documents

`docs/` holds committed, durable architecture documents - not design docs or implementation plans,
which stay local per the global `AGENTS.md` convention. Each document describes a subsystem as it
currently stands, using relative links to the source files that back each claim, so a stale
document is a broken link rather than a plausible-sounding lie. Update the relevant document in the
same change that moves the code it describes.

- [`docs/overview.md`](docs/overview.md) - the whole library, one level deep.
- [`docs/glossary.md`](docs/glossary.md) - every block and the asset/serialization jargon built on top of them.

## Foundational Concepts

### Two-Layer Architecture

We support two layers of access to the HIP format:

- **Block Layer**: Direct access to the raw HIP format. Provides low-level manipulation of blocks and data.
- **Asset Layer**: Higher-level API for working with game assets. Provides logical objects composed of multiple blocks.

The two layers are mutually exclusive - you cannot mix them in the same operation.

This also means we support two tiers of consumers, and we treat both of them as first-class citizens in their respective domains.

### Dumb Data Holders

Blocks and Assets are dumb by default. They do not self-serialize, self-validate, derive one fact from another, or maintain direct references to each other.

"Derive one fact from another" is the precise form of the rule, and it matters: an Asset's `Id` must never be recomputed from its `Name`, because roughly 2% of real assets have an ID that is not the hash of the name stored alongside it. Two on-disk copies of *the same* fact are a different case - see "Two Surfaces" below.

Two exceptions:

- The children of a Block. Blocks maintain a list of children, and that collection enforces a single-parent, no-cycles architecture.
- An Asset's physical surface defaults to its logical counterpart where the format stores one value twice, so the two cannot silently disagree unless the file itself did.

### Write Anything, Validate Optionally

We do not prevent the user from creating an invalid state, so long as it can still be serialized to a HIP file. We still maintain information about what states are invalid and expose them to the user via an optional `Validate()` method, but we do not prohibit the user from serializing it.

Examples of permitted invalid states:

- A block missing a required child.
- A block's field containing invalid (not null) data.

Examples of prohibited invalid states:

- A block's field being absent (enforced by not-null, would cause game to serialize the wrong fields).
- A multi-parent relationship (enforced by single-parent rule in children collection, physically unrepresentable on disk).
- A cycle in the block tree (enforced by no-cycles rule in children collection, physically unrepresentable on disk).

### The Block Layer Is Where You Write Anything

The block layer is where you write anything. The asset layer is where consistency is maintained for you.

A consumer who genuinely needs a corrupt offset or a wrong checksum - to reproduce a shipped bug, or to test a loader - drops to the block layer and writes it directly. The asset layer offers no override for those, because an override would reintroduce the ambiguity the split removes.

### Asset Mode Is A Session

The two layers are mutually exclusive because a session owns the blocks that describe assets while it is open. `archive.OpenAssets()` returns an `AssetSession`, which detaches `ATOC`/`LTOC`/`DPAK` from the block tree and locks their fields; `Commit()` - explicit, or on `Dispose()` - rebuilds them from the assets and reattaches.

Committing from `Dispose()` is only safe because **commit is total**: every asset serializes unconditionally, typed assets writing their fields and untyped ones writing the bytes they were given. Nothing a consumer can set makes serialization impossible. If that ever stops being true, commit-on-dispose has to go with it.

Failure to parse an asset degrades it to its untyped form and records a diagnostic. It never throws - one malformed asset must not make an entire archive unopenable.

### Two Surfaces: Logical And Physical

An asset's fields do not all serve the same reader, so every asset class has two surfaces:

- **Logical** - ordinary public properties, one per fact, named for what it means to the game.
- **Physical** - an `IPhysical*` interface implemented explicitly and reached through `asset.Physical`, carrying every value the format stores, including ones the logical surface derives or omits. For byte-exact reproduction, deliberately malformed data, and the codecs themselves.

Deciding where a field goes, in order:

1. **Is it already determined by something else on the object?** Then it is physical and must not also be logical. It defaults to whatever determines it, and its setter clears that override when assigned a matching value, so a codec can assign from disk unconditionally.
2. **Otherwise, is it of real interest to someone editing the game object?** Yes means logical, no means physical as a plain stored field. This half is ergonomics and cheap to reverse.

Fields the layout reserves for every type but only *some* types use are physical, and concrete types opt into them through trait interfaces (`IHasModel`, `IGrabbable`, ...). A trait projects onto the physical storage; it never stores a copy.

### Support Is Three States, Not Two

An asset type's codec support is one of three states, not a binary "done or not":

- **Typed**: Fields are modelled. Read and write fields.
- **Payload**: A file embedded in the archive, native fields not modelled yet. Import/export as a file today; fields may follow.
- **Untyped**: Structured, but not modelled yet. Fields may appear in a future version.

### Hand-Bump Revisions When You Change Imperative Logic

`EvilHop.Corpus` decides whether a cached observation is stale by fingerprinting a
`ValidationRule`'s or `IFacetGenerator`'s declared dependencies - observable IDs, rule IDs, enum
members - and hashing their digests. That fingerprint is automatic for anything declared through a
`ValidationAttribute`, because the digest comes straight from the attribute's own arguments.

It **cannot** see inside a hand-written `Check` or `Map`/`Reduce` method. If you change what one of
those does without changing anything the fingerprint reads, bump `RuleRevision` on the
`ValidationRule` or `Revision` on the `IFacetGenerator` you touched. Skipping this leaves stale
cached output silently uncorrected, with nothing else to catch it.

### Put The Developer First

Every aspect of the library puts the developer first. We prioritize rich documentation, clear error messages, and a simple API. We never sacrifice developer experience for performance or code complexity.

All architectural and design decisions should be approached from the perspective of developers using both layers of the library.

## Further Reference
- [Heavy Iron Modding Wiki](https://heavyironmodding.org)
    - [HIP Archive Format](https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format))
    - [Assets](https://heavyironmodding.org/wiki/EvilEngine/Assets)
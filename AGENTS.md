# EvilHop

EvilHop is a C# .NET library for reading, writing, and modifying HIP archive files used in Heavy Iron Studios games. It uses .NET 10 and xUnit 3 for testing.

The library is in alpha and is not used in any production applications. Breaking changes are normal and expected. Do not worry about maintaining backward compatibility.

## Foundational Concepts

### Two-Layer Architecture

We support two layers of access to the HIP format:

- **Block Layer**: Direct access to the raw HIP format. Provides low-level manipulation of blocks and data.
- **Asset Layer**: Higher-level API for working with game assets. Provides logical objects composed of multiple blocks.

The two layers are mutually exclusive - you cannot mix them in the same operation.

This also means we support two tiers of consumers, and we treat both of them as first-class citizens in their respective domains.

### Dumb Data Holders

Blocks and Assets are dumb by default. They do not self-serialize, self-validate, derive values, or maintain direct references to each other.

The children of a Block are the exception to this rule - Blocks maintain a list of children, and that collection enforces a single-parent, no-cycles architecture.

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

### Support Is Three States, Not Two

An asset type's codec support is one of three states, not a binary "done or not":

- **Typed**: Fields are modelled. Read and write fields.
- **Payload**: A file embedded in the archive, native fields not modelled yet. Import/export as a file today; fields may follow.
- **Untyped**: Structured, but not modelled yet. Fields may appear in a future version.

### Put The Developer First

Every aspect of the library puts the developer first. We prioritize rich documentation, clear error messages, and a simple API. We never sacrifice developer experience for performance or code complexity.

All architectural and design decisions should be approached from the perspective of developers using both layers of the library.

## Corpus and Real Archives

`artifacts/` is a local, gitignored corpus of real game archives - never a build or test dependency, and the full test suite must pass without it. `tools/EvilHop.Corpus` reads it to generate small committed inventories under `corpus/`, which hermetic tests assert against.

Governing rule: **the Corpus tool records observations; tests assert them against current code.** An inventory must never contain a value whose correctness depends on EvilHop's source.

## Glossary
- Archive: A single HIP file made up of a tree of blocks.
- Block: A single unit of data, containing a block's Tag, Size, Data, and Children.
- Tag: A 4-character identifier for a block, physically read from and written to the Archive.
- Asset: A high-level logical object composed of multiple blocks. Represents a single object in the game world. All assets contain an ID, a Type, and a Name.
- Layer: A grouping of assets based of the "category" of their type.

## Further Reference
- [Heavy Iron Modding Wiki](https://heavyironmodding.org)
    - [HIP Archive Format](https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format))
    - [Assets](https://heavyironmodding.org/wiki/EvilEngine/Assets)
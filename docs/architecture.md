# Architecture

The design decisions that shape EvilHop, for maintainers and contributors. See
[`../AGENTS.md`](../AGENTS.md) for the repo-wide rules these decisions reduce to, and
[`overview.md`](overview.md) for a source map of the code. Terms used here are defined in
[`glossary.md`](glossary.md).

## Two Layers, Two Tiers Of Consumers

We support two layers of access to the HIP format:

- **Block Layer**: Direct access to the raw HIP format. Provides low-level manipulation of blocks and data.
- **Asset Layer**: Higher-level API for working with game assets. Provides logical objects composed of multiple blocks.

This means we support two tiers of consumers, and we treat both of them as first-class citizens in
their respective domains. The two layers are mutually exclusive - you cannot mix them in the same
operation - because a session owns the blocks that describe assets while it is open.
[`Archive.OpenAssets()`](../src/EvilHop/Archive.cs) returns an
[`AssetSession`](../src/EvilHop/Assets/Session/AssetSession.cs), which detaches `ATOC`/`LTOC`/`DPAK` from
the block tree and locks their fields; `Commit()` - explicit, or on `Dispose()` - rebuilds them from
the assets and reattaches.

Committing from `Dispose()` is only safe because **commit is total**: every asset serializes
unconditionally, typed assets writing their fields and untyped ones writing the bytes they were
given. Nothing a consumer can set makes serialization impossible. If that ever stops being true,
commit-on-dispose has to go with it.

Failure to parse an asset degrades it to its untyped form and records a diagnostic. It never throws -
one malformed asset must not make an entire archive unopenable.

## Dumb Data Holders

Blocks and Assets are dumb by default. They do not self-serialize, self-validate, derive one fact
from another, or maintain direct references to each other.

"Derive one fact from another" is the precise form of the rule, and it matters: an asset's `Id` must
never be recomputed from its `Name`, because roughly 2% of real assets have an ID that is not the
hash of the name stored alongside it. Two on-disk copies of *the same* fact are a different case -
see "Two Surfaces" below.

Two exceptions:

- The children of a [`Block`](../src/EvilHop/Blocks/Block.cs). Blocks maintain a list of children,
  and that collection enforces a single-parent, no-cycles architecture.
- An asset's physical surface defaults to its logical counterpart where the format stores one value
  twice, so the two cannot silently disagree unless the file itself did.

## Write Anything, Validate Optionally

We do not prevent the user from creating an invalid state, so long as it can still be serialized to
a HIP file. We still maintain information about what states are invalid and expose them to the user
via an optional `Validate()` method, but we do not prohibit the user from serializing it.

Examples of permitted invalid states:

- A block missing a required child.
- A block's field containing invalid (not null) data.

Examples of prohibited invalid states:

- A block's field being absent (enforced by not-null, would cause the game to serialize the wrong
  fields).
- A multi-parent relationship (enforced by single-parent rule in children collection, physically
  unrepresentable on disk).
- A cycle in the block tree (enforced by no-cycles rule in children collection, physically
  unrepresentable on disk).

## The Physical Surface Is Where You Write Anything

The asset layer is where consistency is maintained for you; the physical surface (`asset.Physical`)
is where you write anything that consistency would otherwise correct. A consumer who genuinely needs
a corrupt offset or a wrong checksum - to reproduce a shipped bug, or to test a loader - sets it on
the physical surface and it serializes byte-exactly. The logical surface offers no override for
those, because an override would reintroduce the ambiguity the split removes.

`Physical.Checksum` works the way `Physical.Type` does: derived from the asset's own data until you
assign something the data doesn't hash to, at which point the assignment is an override and is what
serializes. Reading an archive whose `ADBG` disagrees with its asset data records that disagreement
as an override too, so a load and save that edited nothing reproduces it - shipped archives with a
wrong checksum exist, and correcting one silently would cost byte fidelity. Assign the derived value
back to drop the override and let the checksum track the data again.

What the asset model cannot express at all - structure, tags, children - is what the block layer is
for.

## Two Surfaces: Logical And Physical

An asset's fields do not all serve the same reader, so every asset class has two surfaces:

- **Logical** - ordinary public properties, one per fact, named for what it means to the game.
- **Physical** - an `IPhysical*` interface implemented explicitly and reached through `asset.Physical`,
  carrying every value the format stores, including ones the logical surface derives or omits. For
  byte-exact reproduction, deliberately malformed data, and the codecs themselves.

Deciding where a field goes, in order:

1. **Is it already determined by something else on the object?** Then it is physical and must not
   also be logical. It defaults to whatever determines it, and its setter clears that override when
   assigned a matching value, so a codec can assign from disk unconditionally.
2. **Otherwise, is it of real interest to someone editing the game object?** Yes means logical, no
   means physical as a plain stored field. This half is ergonomics and cheap to reverse.

Fields the layout reserves for every type but only *some* types use are physical, and concrete types
opt into them through trait interfaces (`IHasModel`, `IGrabbable`, ...) in
[`Traits.cs`](../src/EvilHop/Assets/Traits.cs). A trait projects onto the physical storage; it never
stores a copy.

## An Asset In Memory Is Game-Agnostic

An asset carries every field EvilHop models for its type, regardless of which game it is destined
for. Nothing about a game reaches the object itself: assets have no
[`FormatProfile`](../src/EvilHop/Serialization/FormatProfile.cs), and their constructors take no
game. The profile is applied at write time and nowhere else.

Per-game variance is about which fields exist on disk, not what they should hold. A codec gates a
field's presence on `profile.Game` - `EnvironmentAsset` skips `LoldHeight` under N100F,
`BoulderAsset` reads `StaticFriction` only under BFBB - so a field that is meaningful in one game is
simply dropped for the others. Its value elsewhere is unobservable, which is what lets a single
all-games default be correct everywhere.

Where a *value* genuinely differs per game, it is translated at the boundary rather than stored
per-game: N100F and BFBB shift their on-disk `LHDR` numbering, and
[`Serializer.Dictionary.cs`](../src/EvilHop/Serialization/Blocks/Serializer.Dictionary.cs) maps it
to and from [`LayerType`](../src/EvilHop/Common/LayerType.cs) on the way through. `Layer.Type` never
knows which game it belongs to.

Concrete asset classes fix their own [`AssetType`](../src/EvilHop/Common/AssetType.cs) through a
primary constructor, so `new SurfaceAsset()` is written by the surface codec with nothing further
supplied. The shape-generic classes serve many types and take theirs as an argument instead.

## Support Is Three States, Not Two

An asset type's codec support is one of three states, not a binary "done or not":

- **Typed**: Fields are modelled. Read and write fields.
- **Payload**: A file embedded in the archive, native fields not modelled yet. Import/export as a
  file today; fields may follow.
- **Untyped**: Structured, but not modelled yet. Fields may appear in a future version.

## Put The Developer First

Every aspect of the library puts the developer first. We prioritize rich documentation, clear error
messages, and a simple API. We never sacrifice developer experience for performance or code
complexity.

All architectural and design decisions should be approached from the perspective of developers using
both layers of the library.

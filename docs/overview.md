# EvilHop Architecture Overview

A map of EvilHop for maintainers and contributors. See [`../AGENTS.md`](../AGENTS.md) for the
project's conventions, [`architecture.md`](architecture.md) for how it is designed, and
[`glossary.md`](glossary.md) for its jargon.

## Entry Point

[`Archive`](../src/EvilHop/Archive.cs) is the root object: a list of root
[`Block`](../src/EvilHop/Blocks/Block.cs)s plus the [`Serializer`](../src/EvilHop/Serialization/Serializer.cs)
that read them. `Archive.Load`/`Save` round-trip a stream through the block layer, and
`Archive.Create` builds a new empty TSSM archive stamped for the serializer's platform - the only
place EvilHop stamps a `PLAT` block; archives read from disk are never restamped.
`Archive.OpenAssets()` enters the asset layer to edit by returning an
[`AssetSession`](../src/EvilHop/Assets/Session/AssetSession.cs), and `Archive.ReadAssets()` reads it
by returning an [`AssetView`](../src/EvilHop/Assets/Session/AssetView.cs).

## Block Layer

[`Block`](../src/EvilHop/Blocks/Block.cs) is the abstract base for every block type. Each block has
a 4-character `Tag`, a `Parent`, and a [`BlockChildren`](../src/EvilHop/Blocks/BlockChildren.cs)
collection enforcing single-parent, no-cycles. Concrete blocks live in
[`src/EvilHop/Blocks/`](../src/EvilHop/Blocks/): [`Package`](../src/EvilHop/Blocks/Package/Package.cs) (the
`PACK` header), [`Dictionary`](../src/EvilHop/Blocks/Dictionary/Dictionary.cs) (`DICT`, holding the asset and
layer tables), and [`AssetStream`](../src/EvilHop/Blocks/AssetStream/AssetStream.cs) (`STRM`, holding the raw
asset bytes in `DPAK`), plus [`HIPA`](../src/EvilHop/Blocks/HIPA.cs) and the unofficial
[`HIPB`](../src/EvilHop/Blocks/HIPB.cs) compatibility block. Blocks that belong to a
managed structure (`AHDR`, `ADBG`, `LHDR`, `DPAK`) can have their fields locked by `Archive` while
an `AssetSession` owns them; `Block.SetManagedBlockField`/`EnsureFieldsUnlocked` enforce this.

[`Serializer`](../src/EvilHop/Serialization/Serializer.cs) reads and writes the shared block
envelope (tag, size, fields, children) and dispatches per-tag field handlers registered via
`RegisterBlock<T>`. It is abstract; one subclass per game lives alongside it -
[`BFBBSerializer`](../src/EvilHop/Serialization/Games/BFBBSerializer.cs),
[`IncrediblesSerializer`](../src/EvilHop/Serialization/Games/IncrediblesSerializer.cs),
[`N100FSerializer`](../src/EvilHop/Serialization/Games/N100FSerializer.cs),
[`ROTUSerializer`](../src/EvilHop/Serialization/Games/ROTUSerializer.cs),
[`RatatouilleSerializer`](../src/EvilHop/Serialization/Games/RatatouilleSerializer.cs), and
[`TSSMSerializer`](../src/EvilHop/Serialization/Games/TSSMSerializer.cs) - created through
`Serializer.Create(game)` for block-only work or `Serializer.Create(game, platform)` when assets will
be opened. [`FormatProfile`](../src/EvilHop/Serialization/FormatProfile.cs) is the record of per-game
quirks (platform, endianness, field order, padding) a serializer reads with; its platform is null
until stated, and the asset layer refuses to open an archive without one.

## Asset Layer

[`Asset`](../src/EvilHop/Assets/Asset.cs) is the abstract base for every asset, exposing `Id`,
`Type`, `Name`, and a `Physical` surface (`IPhysicalAsset`) for on-disk values with no logical
counterpart. The hierarchy deepens through
[`BaseAsset`](../src/EvilHop/Assets/BaseAsset.cs) (adds `Links`),
[`EntityAsset`](../src/EvilHop/Assets/EntityAsset.cs), and
[`DynamicAsset`](../src/EvilHop/Assets/DynamicAsset.cs); [`PayloadAsset`](../src/EvilHop/Assets/PayloadAsset.cs)
is the separate shape for file-embedding types. Types with no concrete codec yet fall back to the
`Generic*` classes in [`GenericAssets.cs`](../src/EvilHop/Assets/Fallbacks/GenericAssets.cs), preserving
their bytes unparsed. Fields reserved for a family but used by only some of its types are exposed
through trait interfaces in [`Traits.cs`](../src/EvilHop/Assets/Traits.cs) (`IHasModel`,
`IGrabbable`, ...). [`Layer`](../src/EvilHop/Assets/Layer.cs) groups assets the way the archive
does; [`Link`](../src/EvilHop/Assets/Link.cs) and [`Parameter`](../src/EvilHop/Assets/Parameter.cs)
model a `BaseAsset`'s connections to other assets.

[`AssetSession`](../src/EvilHop/Assets/Session/AssetSession.cs) is the scope that owns an archive's assets:
opening detaches and locks `Dictionary`/`AssetStream`'s managed blocks and parses `Layer`s of
`Asset`s from them; `Commit()` (explicit, or via `Dispose()`) rebuilds those blocks unconditionally
from the current assets and reattaches them. A per-asset parse failure degrades that asset to its
generic form and is recorded on `Diagnostics` rather than thrown. The archive knows its open
session, and refuses a second one or a view until it commits.

[`AssetView`](../src/EvilHop/Assets/Session/AssetView.cs) is the read-only counterpart: it parses the
same `Layer`s, `Asset`s and `Diagnostics` but leaves the blocks attached and unlocked and never
commits, and it keeps a copy of the asset data so `GetBytes` and `GetLayerImage` answer with the
bytes as read. Both parse through the internal
[`AssetReader`](../src/EvilHop/Assets/Session/AssetReader.cs), which also resolves the platform.
A view's `Index`, built on first use, is an [`AssetIndex`](../src/EvilHop/Assets/Session/AssetIndex.cs):
lookups by ID, name and type, and `AssetReference`s out of and into each asset, found by
[`AssetReferenceWalker`](../src/EvilHop/Assets/Session/AssetReferenceWalker.cs). `GetLayout` gives a
layer's [`LayerLayout`](../src/EvilHop/Assets/Session/LayerLayout.cs), built on first use from the
placements as read: its asset and gap entries in offset order, and `Locate(offset)`.

[`AssetCodecs`](../src/EvilHop/Assets/Serialization/AssetCodecs.cs) maps each
[`AssetType`](../src/EvilHop/Common/AssetType.cs) to the reader/writer that (de)serializes it.
Every type is seeded at static construction with a generic handler for its shape
(`BaseAsset`/`EntityAsset`/`DynamicAsset`/`Payload`, per `ShapesByType`); a concrete codec calling
`Register<T>` overwrites that entry. A writer whose asset doesn't match its registered shape falls
back to the generic writer for the asset's actual runtime shape (`Guarded<T>`).
[`AssetFields`](../src/EvilHop/Assets/Serialization/AssetFields.cs) and
[`AssetPrefixes`](../src/EvilHop/Assets/Serialization/AssetPrefixes.cs) hold the field-level
read/write helpers each shape's codec is built from.

`DYNA` dispatches a second time.
[`DynamicAsset`](../src/EvilHop/Assets/DynamicAsset.cs) reads the shared prefix and the trailing
`Links`, then asks [`DynamicCodecs`](../src/EvilHop/Assets/Serialization/DynamicCodecs.cs) for the
subclass registered for its [`DynamicKind`](../src/EvilHop/Assets/Dynamics/DynamicKind.cs), game,
and version, which reads only the fields between the two. Any combination with nothing registered
reads as a [`GenericDynamicAsset`](../src/EvilHop/Assets/Dynamics/GenericDynamicAsset.cs) whose
fields stay unparsed while its links are still modelled. Typed dynamics live under
`Assets/Dynamics/<category>/` (the game's own name prefix: `Effect/`, `GameObject/`, `UI/`, ...) and
are named `<Kind>DynamicAsset`.

## Primitives and Common

[`src/EvilHop/Primitives/`](../src/EvilHop/Primitives/) holds format-agnostic building blocks:
[`EndianReader`](../src/EvilHop/Primitives/EndianReader.cs)/[`EndianWriter`](../src/EvilHop/Primitives/EndianWriter.cs)
for endian-aware I/O, [`EvilString`](../src/EvilHop/Primitives/EvilString.cs) for the format's string
encoding, and [`Rgb`](../src/EvilHop/Primitives/Rgb.cs)/[`Rgba`](../src/EvilHop/Primitives/Rgba.cs) for
colors with normalized (0–1) channels. `EndianReader`/`EndianWriter` natively read/write both on-disk
forms found across assets: one float per channel (`ReadRgb`/`ReadRgba`), and one byte per channel,
scaled to/from 0-255 (`ReadRgb24`/`ReadRgba32`).
[`src/EvilHop/Common/`](../src/EvilHop/Common/) holds shared enums and hashing/checksum types used
by both layers: [`GameVersion`](../src/EvilHop/Common/GameVersion.cs),
[`Platform`](../src/EvilHop/Common/Platform.cs), [`AssetType`](../src/EvilHop/Common/AssetType.cs),
[`LayerType`](../src/EvilHop/Common/LayerType.cs),
[`BKDRHash`](../src/EvilHop/Common/BKDRHash.cs) (asset ID hashing), [`AssetId`](../src/EvilHop/Common/AssetId.cs), and
[`Crc32Mpeg2`](../src/EvilHop/Common/Crc32Mpeg2.cs) (asset checksums).

## Corpus Tooling

[`tools/EvilHop.Corpus/`](../tools/EvilHop.Corpus/) is a separate console tool, depended on by
nothing in `src/EvilHop/`, that reads real archives from a local `artifacts/` directory and
generates the committed inventories under [`corpus/`](../corpus/) that hermetic tests assert
against.

## Planned Documents

This overview will be supplemented by dedicated documents for the asset layer and serialization as
they're written. See [`glossary.md`](glossary.md) for terminology.

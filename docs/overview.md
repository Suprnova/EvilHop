# EvilHop Architecture Overview

A map of EvilHop for maintainers and contributors. See [`../AGENTS.md`](../AGENTS.md) for the
project's conventions, [`architecture.md`](architecture.md) for how it is designed, and
[`glossary.md`](glossary.md) for its jargon.

## Entry Point

[`Archive`](../src/EvilHop/Archive.cs) is the root object: a list of root
[`Block`](../src/EvilHop/Blocks/Block.cs)s plus the [`Serializer`](../src/EvilHop/Serialization/Serializer.cs)
that read them. `Archive.Load`/`Save` round-trip a stream through the block layer.
`Archive.OpenAssets()` enters the asset layer by returning an
[`AssetSession`](../src/EvilHop/Assets/AssetSession.cs).

## Block Layer

[`Block`](../src/EvilHop/Blocks/Block.cs) is the abstract base for every block type. Each block has
a 4-character `Tag`, a `Parent`, and a [`BlockChildren`](../src/EvilHop/Blocks/BlockChildren.cs)
collection enforcing single-parent, no-cycles. Concrete blocks live in
[`src/EvilHop/Blocks/`](../src/EvilHop/Blocks/): [`Package`](../src/EvilHop/Blocks/Package.cs) (the
`PACK` header), [`Dictionary`](../src/EvilHop/Blocks/Dictionary.cs) (`DICT`, holding the asset and
layer tables), and [`AssetStream`](../src/EvilHop/Blocks/AssetStream.cs) (`STRM`, holding the raw
asset bytes in `DPAK`), plus [`HIPA`](../src/EvilHop/Blocks/HIPA.cs). Blocks that belong to a
managed structure (`AHDR`, `ADBG`, `LHDR`, `DPAK`) can have their fields locked by `Archive` while
an `AssetSession` owns them; `Block.SetManagedBlockField`/`EnsureFieldsUnlocked` enforce this.

[`Serializer`](../src/EvilHop/Serialization/Serializer.cs) reads and writes the shared block
envelope (tag, size, fields, children) and dispatches per-tag field handlers registered via
`RegisterBlock<T>`. It is abstract; one subclass per game lives alongside it -
[`BFBBSerializer`](../src/EvilHop/Serialization/BFBBSerializer.cs),
[`IncrediblesSerializer`](../src/EvilHop/Serialization/IncrediblesSerializer.cs),
[`N100FSerializer`](../src/EvilHop/Serialization/N100FSerializer.cs),
[`ROTUSerializer`](../src/EvilHop/Serialization/ROTUSerializer.cs),
[`RatatouilleSerializer`](../src/EvilHop/Serialization/RatatouilleSerializer.cs), and
[`TSSMSerializer`](../src/EvilHop/Serialization/TSSMSerializer.cs) - each exposing a
`DefaultProfile`. [`FormatProfile`](../src/EvilHop/Serialization/FormatProfile.cs) is the record of
per-game quirks (platform, endianness, field order, padding) a serializer reads with.

## Asset Layer

[`Asset`](../src/EvilHop/Assets/Asset.cs) is the abstract base for every asset, exposing `Id`,
`Type`, `Name`, and a `Physical` surface (`IPhysicalAsset`) for on-disk values with no logical
counterpart. The hierarchy deepens through
[`BaseAsset`](../src/EvilHop/Assets/BaseAsset.cs) (adds `Links`),
[`EntityAsset`](../src/EvilHop/Assets/EntityAsset.cs), and
[`DynaAsset`](../src/EvilHop/Assets/DynaAsset.cs); [`PayloadAsset`](../src/EvilHop/Assets/PayloadAsset.cs)
is the separate shape for file-embedding types. Types with no concrete codec yet fall back to the
`Generic*` classes in [`GenericAssets.cs`](../src/EvilHop/Assets/GenericAssets.cs), preserving
their bytes unparsed. Fields reserved for a family but used by only some of its types are exposed
through trait interfaces in [`Traits.cs`](../src/EvilHop/Assets/Traits.cs) (`IHasModel`,
`IGrabbable`, ...). [`Layer`](../src/EvilHop/Assets/Layer.cs) groups assets the way the archive
does; [`Link`](../src/EvilHop/Assets/Link.cs) and [`Parameter`](../src/EvilHop/Assets/Parameter.cs)
model a `BaseAsset`'s connections to other assets.

[`AssetSession`](../src/EvilHop/Assets/AssetSession.cs) is the scope that owns an archive's assets:
opening detaches and locks `Dictionary`/`AssetStream`'s managed blocks and parses `Layer`s of
`Asset`s from them; `Commit()` (explicit, or via `Dispose()`) rebuilds those blocks unconditionally
from the current assets and reattaches them. A per-asset parse failure degrades that asset to its
generic form and is recorded on `Diagnostics` rather than thrown.

[`AssetCodecs`](../src/EvilHop/Serialization/AssetCodecs.cs) maps each
[`AssetType`](../src/EvilHop/Common/AssetType.cs) to the reader/writer that (de)serializes it.
Every type is seeded at static construction with a generic handler for its shape
(`BaseAsset`/`EntityAsset`/`DynaAsset`/`Payload`, per `ShapesByType`); a concrete codec calling
`Register<T>` overwrites that entry. A writer whose asset doesn't match its registered shape falls
back to the generic writer for the asset's actual runtime shape (`Guarded<T>`).
[`AssetFields`](../src/EvilHop/Assets/Serialization/AssetFields.cs) and
[`AssetPrefixes`](../src/EvilHop/Assets/Serialization/AssetPrefixes.cs) hold the field-level
read/write helpers each shape's codec is built from.

## Primitives and Common

[`src/EvilHop/Primitives/`](../src/EvilHop/Primitives/) holds format-agnostic building blocks:
[`EndianReader`](../src/EvilHop/Primitives/EndianReader.cs)/[`EndianWriter`](../src/EvilHop/Primitives/EndianWriter.cs)
for endian-aware I/O, [`EvilString`](../src/EvilHop/Primitives/EvilString.cs) for the format's
string encoding, and [`AssetId`](../src/EvilHop/Primitives/AssetId.cs).
[`src/EvilHop/Common/`](../src/EvilHop/Common/) holds shared enums and hashing/checksum types used
by both layers: [`GameVersion`](../src/EvilHop/Common/GameVersion.cs),
[`Platform`](../src/EvilHop/Common/Platform.cs), [`AssetType`](../src/EvilHop/Common/AssetType.cs),
[`LayerType`](../src/EvilHop/Common/LayerType.cs),
[`BKDRHash`](../src/EvilHop/Common/BKDRHash.cs) (asset ID hashing), and
[`Crc32Mpeg2`](../src/EvilHop/Common/Crc32Mpeg2.cs) (asset checksums).

## Validation

[`src/EvilHop/Validation/`](../src/EvilHop/Validation/) implements the `Validate()` surface described
in the project glossary. `Archive`, `Block`, and `Asset` implement
[`IValidatable`](../src/EvilHop/Validation/ValidationContext.cs), taking a
[`ValidationContext`](../src/EvilHop/Validation/ValidationContext.cs) (the archive's `FormatProfile`
plus its `Origin`/`Role`, which only a caller can supply) and yielding
[`ValidationIssue`](../src/EvilHop/Validation/ValidationIssue.cs)s: a rule ID, a
[`Severity`](../src/EvilHop/Validation/Severity.cs), an
[`IssueSite`](../src/EvilHop/Validation/IssueSite.cs) locating the violation (built from
[`BlockPath`](../src/EvilHop/Validation/BlockPath.cs) for block-rooted sites), a message, and an
optional known-violation classification.

Most rules are declared next to the field they constrain, via the attribute family in
[`ValidationAttribute.cs`](../src/EvilHop/Validation/ValidationAttribute.cs) (`ConstantValue`,
`AllowedValues`, `ClosedEnum`, `DefinedBits`, `RequiredBits`, `RequiredChild`, `RepeatableChild`,
`NoChildren`, `Observed`). [`ValidationCatalogue`](../src/EvilHop/Validation/ValidationCatalogue.cs)
reflects over these once, materializing each into a
[`ValueRule`](../src/EvilHop/Validation/ValueRule.cs) - a
[`ValidationRule`](../src/EvilHop/Validation/ValidationRule.cs) whose `Holds` is a predicate over one
field's value - and into an [`Observable`](../src/EvilHop/Validation/Observable.cs): a named,
primitive-valued projection over the same field, read by `ValidationCatalogue.Observe` independently
of whether the rule holds. Rules conditional on more than one member stay hand-written
`ValidationRule<T>` subclasses instead of attributes.

`ValidationCatalogue.DigestOf` hashes one observable's declaration, so a consumer that fingerprints
its dependencies on that declaration - such as a corpus tool reducing observed values into a
committed inventory - can tell exactly when it goes stale.

[`tests/EvilHop.Tests/Inventory/`](../tests/EvilHop.Tests/Inventory/) closes the loop: it reads the
committed `corpus/*.json` inventories and replays every `ValueRule` in the catalogue against the
values recorded for it, entirely offline. A rule that changed definition, or a value a real archive
exposed that no rule accounts for, shows up as a test failure with no archive required to reproduce
it.

## Planned Documents

This overview will be supplemented by dedicated documents for the asset layer and serialization as
they're written. See [`glossary.md`](glossary.md) for terminology.

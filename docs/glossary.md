# Glossary

Terms used throughout EvilHop's code and documentation, for maintainers and contributors. Each
entry links to a source file that backs it.

## Blocks

Every block is a 4-character tag followed by a size and then its content - see
[`Block`](../src/EvilHop/Blocks/Block.cs) and [`Serializer`](../src/EvilHop/Serialization/Serializer.cs).
These are the twenty registered by every game.

| Tag | Type | What it holds |
|---|---|---|
| `HIPA` | [`HIPA`](../src/EvilHop/Blocks/HIPA.cs) | Marker at the very start of every archive. No data, no children. |
| `PACK` | [`Package`](../src/EvilHop/Blocks/Package.cs) | Root of the archive's metadata blocks (version, flags, counts, timestamps, platform). |
| `PVER` | [`PackageVersion`](../src/EvilHop/Blocks/Package.cs) | The archive format version and client version. |
| `PFLG` | [`PackageFlags`](../src/EvilHop/Blocks/Package.cs) | A bitmask of platform, region, and language flags. |
| `PCNT` | [`PackageCount`](../src/EvilHop/Blocks/Package.cs) | Asset and layer counts, plus the largest asset/layer sizes. |
| `PCRT` | [`PackageCreated`](../src/EvilHop/Blocks/Package.cs) | The archive's creation timestamp. |
| `PMOD` | [`PackageModified`](../src/EvilHop/Blocks/Package.cs) | The archive's last-modified timestamp. |
| `PLAT` | [`PackagePlatform`](../src/EvilHop/Blocks/Package.cs) | Platform, region, and language strings. Introduced in Battle for Bikini Bottom; absent from N100F. |
| `DICT` | [`Dictionary`](../src/EvilHop/Blocks/Dictionary.cs) | Root of the asset table and layer table. |
| `ATOC` | [`AssetTable`](../src/EvilHop/Blocks/Dictionary.cs) | The list of every asset's `AHDR` entry. |
| `AINF` | [`AssetInf`](../src/EvilHop/Blocks/Dictionary.cs) | A single unknown scalar on `ATOC`. |
| `AHDR` | [`AssetHeader`](../src/EvilHop/Blocks/Dictionary.cs) | One asset's ID, type, offset, size, and flags. |
| `ADBG` | [`AssetDebug`](../src/EvilHop/Blocks/Dictionary.cs) | One asset's alignment, name, source filename, and checksum. |
| `LTOC` | [`LayerTable`](../src/EvilHop/Blocks/Dictionary.cs) | The list of every layer's `LHDR` entry. |
| `LINF` | [`LayerInf`](../src/EvilHop/Blocks/Dictionary.cs) | A single unknown scalar on `LTOC`. |
| `LHDR` | [`LayerHeader`](../src/EvilHop/Blocks/Dictionary.cs) | One layer's type and the asset IDs it lists. |
| `LDBG` | [`LayerDebug`](../src/EvilHop/Blocks/Dictionary.cs) | A single unknown scalar on `LHDR`. |
| `STRM` | [`AssetStream`](../src/EvilHop/Blocks/AssetStream.cs) | Root of the asset data blocks. |
| `DHDR` | [`StreamHeader`](../src/EvilHop/Blocks/AssetStream.cs) | A single unknown scalar on `STRM`. |
| `DPAK` | [`StreamData`](../src/EvilHop/Blocks/AssetStream.cs) | The raw bytes of every asset back to back, plus the padding that aligns them. |

## Concepts

- **Archive** - a single HIP file, represented as an ordered list of root blocks (typically `HIPA`,
  `PACK`, `DICT`, `STRM`). See [`Archive`](../src/EvilHop/Archive.cs).
- **Block** - one node of the archive's tree: a tag, a size, optional fields, and children. See
  [`Block`](../src/EvilHop/Blocks/Block.cs).
- **Tag** - a block's 4-character identifier (`PACK`, `AHDR`, ...), read and written literally.
  See [`Block.Tag`](../src/EvilHop/Blocks/Block.cs).
- **Managed / locked fields** - fields of `AHDR`, `ADBG`, `LHDR`, and `DPAK` that an
  [`AssetSession`](../src/EvilHop/Assets/AssetSession.cs) locks while it owns those blocks, so an
  Asset Mode consumer can't edit a block that a session is about to rebuild. See
  [`Block.EnsureFieldsUnlocked`](../src/EvilHop/Blocks/Block.cs).
- **Asset** - a logical game object assembled from an `AHDR`/`ADBG` pair and its slice of `DPAK`.
  Every asset has an `Id`, a `Type`, and a `Name`. See [`Asset`](../src/EvilHop/Assets/Asset.cs).
- **Layer** - a named grouping of assets, mirroring one `LHDR`. See
  [`Layer`](../src/EvilHop/Assets/Layer.cs).
- **AssetId** - a 32-bit reference to an asset, normally the BKDR hash of its name. See
  [`AssetId`](../src/EvilHop/Primitives/AssetId.cs).
- **AssetType** - the closed set of known asset kinds (`Model`, `Trigger`, `Sound`, ...), read from
  `AHDR.Type`. See [`AssetType`](../src/EvilHop/Common/AssetType.cs).
- **Link** - a `BaseAsset`'s connection to another asset: a source event, a destination event, a
  destination `AssetId`, and four `Parameter` slots. See [`Link`](../src/EvilHop/Assets/Link.cs).
- **Parameter** - one 4-byte slot of a `Link`, whose real meaning (float, int, or asset reference)
  depends on which event it's paired with. See [`Parameter`](../src/EvilHop/Assets/Parameter.cs).
- **Session** - the scoped [`AssetSession`](../src/EvilHop/Assets/AssetSession.cs) that owns an
  archive's asset-describing blocks while Asset Mode is active, opened by `Archive.OpenAssets()`.
- **Diagnostic** - a non-fatal problem recorded on `AssetSession.Diagnostics` when an asset fails to
  parse and is degraded to its generic form instead of throwing. See
  [`AssetDiagnostic`](../src/EvilHop/Assets/AssetSession.cs).
- **Codec** - the reader/writer pair for one `AssetType`, registered with
  [`AssetCodecs`](../src/EvilHop/Serialization/AssetCodecs.cs).
- **Shape** - which level of the asset hierarchy (`BaseAsset`, `EntityAsset`, `DynaAsset`, or
  `Payload`) a type's bytes are known to follow before a real codec is written for it. See
  `AssetShape` in [`AssetCodecs`](../src/EvilHop/Serialization/AssetCodecs.cs).
- **Trait** - an interface (`IHasModel`, `IGrabbable`, ...) a concrete asset type implements to
  expose a field its whole family reserves but only some types use. See
  [`Traits.cs`](../src/EvilHop/Assets/Traits.cs).
- **Logical surface** - an asset's ordinary public properties, one per fact, named for what it means
  to the game. See [`Asset`](../src/EvilHop/Assets/Asset.cs).
- **Physical surface** - an asset's `IPhysical*` interface, reached through `asset.Physical`,
  carrying every on-disk value including ones the logical surface derives or omits. See
  [`IPhysicalAsset`](../src/EvilHop/Assets/Asset.cs).
- **Serializer** - the abstract reader/writer for the shared block envelope; one subclass per game
  (`BFBBSerializer`, `TSSMSerializer`, ...). See
  [`Serializer`](../src/EvilHop/Serialization/Serializer.cs).
- **Profile** - a [`FormatProfile`](../src/EvilHop/Serialization/FormatProfile.cs), the per-game
  (sometimes per-build) quirks - platform, endianness, field order, padding - a serializer reads
  with.
- **GameVersion** - the enum identifying which Heavy Iron Studios game an archive or profile
  targets. See [`GameVersion`](../src/EvilHop/Common/GameVersion.cs).
- **Platform** - the enum identifying which console a build targets, which also determines its byte
  order. See [`Platform`](../src/EvilHop/Common/Platform.cs).
- **BKDR hash** - the hashing algorithm EvilEngine uses to turn an asset's name into its `AssetId`.
  See [`BKDRHash`](../src/EvilHop/Common/BKDRHash.cs).
- **EvilString** - the format's null-terminated, even-padded string encoding used throughout block
  fields. See [`EvilString`](../src/EvilHop/Primitives/EvilString.cs).
- **Fill byte / gap** - the byte value (sampled from the archive's own padding, or a default) used
  to fill the space `DPAK` leaves between two assets for alignment. See
  [`AssetSession.GapBytesFor`](../src/EvilHop/Assets/AssetSession.cs).

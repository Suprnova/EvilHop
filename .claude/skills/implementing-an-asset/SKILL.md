---
name: implementing-an-asset
description: Use this skill when implementing a single concrete HIP asset type in EvilHop — turning a Heavy Iron Modding wiki page (or any provided layout doc) into the asset class, its two surfaces (logical + IPhysical*), its codec registration, and its tests. Self-contained: every shared pattern (prefixes, traits, links, generic shapes, per-game profiles) is inlined below, and a script locates and dumps an asset type's real bytes across every game in one command.
---

# Implementing a single Asset

You are turning one asset type's layout into a typed EvilHop asset. This skill is the standardized
procedure for doing that once, for one type, from a wiki page or layout doc supplied with the request
(pasted text, an attachment, or a link).

The goal is a byte-exact round trip for the type **and** a clean, discoverable public API. Everything
below is either restated from [`docs/architecture.md`](../../../docs/architecture.md) (short; worth a
real read once) or new information specific to this workflow, and §
[Reference](#reference-everything-a-codec-needs) inlines what you'd otherwise need to open several
source files to find. Anything this skill doesn't cover is answered faster by reading the two or three
source files §[Where things live](#where-things-live) points at.

**Workflow at a glance:** get the layout → run `locate-asset.cs` to see the type's real bytes across
every game → write the asset class + codec using the patterns in §Reference → wire up traits and
`IPhysical*` → build + test → (later) run the EvilHop.Corpus invariants. The middle piece — validating
against bytes before writing code — is the step almost everyone skips, and it is where the wiki's
mistakes come out.

## When to use / not use this

- **Use** for a type that has a wiki page or provided layout describing its fields (e.g. an
  `EntityAsset`-shaped type like `BOUL`/`BUTN`, a `BaseAsset`-shaped type like `CAM`/`CNTR`, a
  `DynaAsset` subtype, or a plain `Asset`-shaped type with no header at all like `MRKR`/`LODT`).
- **Don't** use for the RenderWare/payload types (`MODL`, `RWTX`, `JSP`, `BINK`, `SND`, ...). Those
  are `PayloadAsset`s whose body is an embedded file, already served by `SaveTo()`/`LoadFrom()` unless
  a native field model is specifically being scoped. Adding a *payload* type is: seed its `AssetType`
  in `ShapesByType` (see below) and stop — there's no field model to write.
- **Don't** use to implement a whole layer/session or the codec registry plumbing — that exists.

## Where things live

| Thing | Path |
|---|---|
| Asset base classes | `src/EvilHop/Assets/Asset.cs`, `BaseAsset.cs`, `EntityAsset.cs`, `DynaAsset.cs` |
| Trait interfaces (`IHasModel`, `IGrabbable`, ...) | `src/EvilHop/Assets/Traits.cs` |
| Shared header readers/writers | `src/EvilHop/Assets/Serialization/AssetPrefixes.cs` |
| Header-sourced field copy | `src/EvilHop/Assets/Serialization/AssetFields.cs` |
| Link read/write | `src/EvilHop/Assets/Serialization/LinkSerialization.cs`, `src/EvilHop/Assets/Link.cs`, `src/EvilHop/Assets/Parameter.cs` |
| Generic shape fallbacks | `src/EvilHop/Assets/Fallbacks/GenericAssets.cs` |
| Codec registry | `src/EvilHop/Assets/Serialization/AssetCodecs.cs` (incl. `ShapesByType` table) |
| Per-game quirks | `src/EvilHop/Serialization/FormatProfile.cs`, `src/EvilHop/Serialization/Games/*Serializer.cs` (one per game) |
| Raw read/write primitives | `src/EvilHop/Primitives/EndianReader.cs`, `EndianWriter.cs` |
| Id / type primitives | `src/EvilHop/Common/AssetId.cs`, `src/EvilHop/Common/AssetType.cs` |
| Tests | `tests/EvilHop.Tests/Assets/<Family>/`, the family-folder mirror of the asset |
| Byte-locating script (this skill) | `.claude/skills/implementing-an-asset/scripts/locate-asset.cs` |

§[Reference](#reference-everything-a-codec-needs) below inlines everything a normal implementation
needs from these files. Open them directly only when you need something that section doesn't cover,
or to see a fuller example than the condensed one given here.

## The three-part decision that precedes any code

Before you write a single property, settle three things.

### 1. Which parent does it derive from?

This is not a guess. The wiki's type classifier (`Binary`/`Base`/`Entity`/`RenderWare`) says, and the
archive's `baseType` verifies it (see §[Validate the layout](#validate-the-layout-before-writing-the-class)):

- **`Asset`** — no shared header at all; the payload starts directly with the type's own fields. Not
  just a "not modelled yet" fallback — genuinely correct for some types, e.g. `MRKR` (just a
  `Vector3`) and `LODT` (a leading count + an array of records). The wiki's `Binary` classifier with an
  empty `basetype` is the tell.
- **`BaseAsset`** — has the 8-byte header (`BaseId`, `BaseType`, `LinkCount`, `BaseFlags`). E.g. `CAM`,
  `CNTR`.
- **`EntityAsset : BaseAsset`** — `BaseAsset` header + the entity prefix (flags, `Angle`, `Position`,
  `Scale`, color multiplier, `ModelId`, `AnimListId`, ...). E.g. `BOUL`, `BUTN`.
- **`DynaAsset : BaseAsset`** — `BaseAsset` header + `uint DynaType, short Version, short Handle`, then
  the dyna's own fields. `DYNA` subtypes dispatch twice (see `AssetCodecs` remarks).

Concrete types live in family folders grouped by shape and domain under `src/EvilHop/Assets/`
(`Cameras/`, `Animation/`, `Audio/`, `Models/`, `Objects/`), not all in the root — tests mirror the
same families under `tests/EvilHop.Tests/Assets/`. Match the naming: an asset that
is a `BaseAsset` gets a `FooAsset : BaseAsset` class; one that is an `EntityAsset` gets
`FooAsset : EntityAsset`; a plain `Asset` gets `FooAsset : Asset`. The class name mirrors the
`AssetType` enum member exactly (`AssetType.LODTable` → `LODTableAsset`, `AssetType.Hangable` →
`HangableAsset`).

### 2. Which fields are logical, which are physical, which are traits?

Applied in order:

1. **Is the value already determined by something else on the object?** → physical, and *must not*
   also be logical. Known members: `BaseId` (a second copy of `Id`), `Type` (implied by the class),
   `LinkCount` (implied by `Links`), `DynaAsset`'s `DynaType`/`Version` where a concrete dyna class
   implies them, and — for a plain-`Asset` type holding a collection with its own on-disk leading count
   (`LODT`'s `numextra`) — that count, implied by the collection's length the same way `LinkCount` is.
2. **Otherwise, is it of real interest to someone editing the game object?** → logical if yes;
   physical as a plain stored field if no. `Alignment`, `PFlags`, `Subtype`, `SeeThroughSpeed` sit here
   today — real, independent, rarely touched.

Fields the shared entity layout reserves for a whole family but only some types use (`SurfaceId`,
`ModelId`, `AnimListId`, and the `CollisionFlags` bits) are **physical**, and a type that genuinely
uses one opts in through a trait interface (`IHasSurface`, `IHasModel`, `IHasAnimList`, `IGrabbable`)
that projects onto the physical storage — it never stores a copy. See §Reference for the full trait
list.

**Do not trust the wiki's "Used by" lists** for deciding which traits a type gets. Those lists are
plausible but unverified — EvilHop.Corpus doesn't extract asset fields yet, so nothing has actually
checked them against real archives (see §[EvilHop.Corpus validation](#evilhopcorpus-validation--deferred)).
In the meantime, see §[etiquette for IPhysical* and unknowns](#etiquette-what-to-expose-vs-ask).

A field with a substructure of its own (an array of records, like `LODT`'s entries) is *not* itself an
`Asset` and gets no `Physical` split — `Physical` only exists on `Asset` and its subclasses. Model the
substructure as a plain class/struct with ordinary public properties (see `Link`, or `LODTableEntry`
once it exists) and put only the top-level asset's own determined-vs-real fields through the
physical/logical test.

### 3. Which games does it diverge across? (and which is "the" game to write first)

The wiki pages are inconsistent here: some are a single struct (`ATBL`, `CNTR`, `CAM`), some are a
`<tabber>` with a different struct per game group (`BOUL`), and one (`BUTN`) mixes per-game inline
notes into a single table. An asset type is **one class across all games** — per-game differences are
one class with nullable/conditional fields or `FormatProfile.Game` switches, never a per-game
subclass. But you do not implement all games at once.

**Write the first codec for exactly one game** (prefer BFBB, or the game the wiki page's primary
struct targets), match that game's `FormatProfile` exactly, and get its byte-exact round trip green.
Then, if the wiki shows other games differing, add the conditional fields/switches and validate each.
A round-trip across every game the class claims to support is what actually proves the divergence
handling — a codec that over-claims games will silently misread on disk. If a game is missing source
or corpus evidence for a field (e.g. `LODT`'s `flags`, present from TSSM onward with no known
meaning), model it as an opaque stored value rather than guessing at semantics — see
§[Etiquette](#etiquette-what-to-expose-vs-ask).

## Reference: everything a codec needs

This section exists so you do not need to open `Asset.cs`, `BaseAsset.cs`, `EntityAsset.cs`,
`DynaAsset.cs`, `Traits.cs`, `AssetPrefixes.cs`, `AssetFields.cs`, `Link.cs`/`Parameter.cs`,
`GenericAssets.cs`, `FormatProfile.cs`, `EndianReader.cs`/`EndianWriter.cs`, or `AssetId.cs` just to
learn their shape. It is a condensed index, not a replacement for reading the one or two files your
specific type actually touches.

### `Asset` (every type has this)

`Id` (`AssetId`), `Type` (`AssetType`, `internal set`), `Name`/`FileName` (`string`), `Layer`
(`internal set`), `Physical` (`IPhysicalAsset`, override per subclass), `GetUnparsedTail()`/
`SetUnparsedTail(byte[])`, `CalculateId()`. `IPhysicalAsset` adds `Type`, `Alignment` (`int`), `Flags`
(`AssetFlags`) — all header-sourced, never touched by a codec directly (see `AssetFields.Populate`).

### `BaseAsset : Asset` (has the 8-byte header)

Logical: `BaseFlags` (`BaseAssetFlags`), `Links` (`Collection<Link>`).
Physical (`IPhysicalBaseAsset : IPhysicalAsset`): `BaseId` (`AssetId`), `BaseType` (`byte`),
`LinkCount` (`byte`).

`BaseId`/`LinkCount` follow the same **override-clears-on-match** shape everywhere a physical field
derives from a logical one — this is the pattern to copy for any new derived field (e.g. `LODT`'s
count):

```csharp
private byte? _overriddenLinkCount;
byte IPhysicalBaseAsset.LinkCount
{
    get => _overriddenLinkCount ?? (byte)Links.Count;
    // prevents equivalent count assignments from being interpretted as an "override"
    set => _overriddenLinkCount = value == (byte)Links.Count ? null : value;
}
```

A codec that parses the real collection sets the physical field back to the derived value once done
(`asset.Physical.LinkCount = (byte)asset.Links.Count;`) so it reads as "agrees, therefore derives" —
copy this exact line shape, substituting your own collection/count pair.

### `EntityAsset : BaseAsset` (the 0x54-ish entity prefix)

Logical: `EntityFlags` (`EntityFlags`), `Angle`/`Position`/`Scale` (`Vector3`), `ColorMultiplier`
(`RgbaColor`, R/G/B/A floats).
Physical (`IPhysicalEntityAsset : IPhysicalBaseAsset`): `Subtype` (`byte`), `PFlags` (`byte`, "always
0" per every sample checked so far), `CollisionFlags` (`CollisionFlags`), `SurfaceId`/`ModelId`/
`AnimListId` (`AssetId`), `SeeThroughSpeed` (`float`, "always 255" per every sample checked so far).
Backing fields are `private protected`, so a derived class can project a `CollisionFlags` bit or an
`AssetId` straight into a trait without going through `Physical`.

### `DynaAsset : BaseAsset` (the `DYNA` dispatch prefix)

Physical only (`IPhysicalDynaAsset : IPhysicalBaseAsset`): `DynaType` (`uint`, selects the concrete
subtype), `Version` (`short`), `Handle` (`short`, runtime-only).

### Traits (`Traits.cs`) — opt-in projections onto reserved `EntityAsset` fields

| Trait | Property | Projects onto |
|---|---|---|
| `IGrabbable` | `bool IsGrabbable` | a `CollisionFlags` bit (`Grabbable`) |
| `IHasSurface` | `AssetId SurfaceId` | `IPhysicalEntityAsset.SurfaceId` |
| `IHasModel` | `AssetId ModelId` | `IPhysicalEntityAsset.ModelId` |
| `IHasAnimList` | `AssetId AnimListId` | `IPhysicalEntityAsset.AnimListId` |

Each trait member is implemented explicitly and one line long: `AssetId IHasModel.ModelId { get =>
Physical.ModelId; set => Physical.ModelId = value; }`. The XML doc "Used by" lists on these interfaces
are wiki-sourced guesses — do not use them to decide whether *your* type gets a trait; decide from
what the bytes actually show (see §[Validate the layout](#validate-the-layout-before-writing-the-class)) or from a clear wiki
field name.

### `AssetPrefixes.cs` — shared byte layout, in exact field order

- `BaseAssetPrefix.Read/Write`: `BaseId` (`AssetId`, 4B) → `BaseType` (`byte`) → `LinkCount` (`byte`)
  → `BaseFlags` (`short`, 2B). 8 bytes total.
- `EntityAssetPrefix.Read/Write(asset, reader/writer, hasPadding)`: `EntityFlags` (`byte`) →
  `Subtype` (`byte`) → `PFlags` (`byte`) → `CollisionFlags` (`byte`) → *(4 bytes of zero padding, only
  when `hasPadding` — see `FormatProfile.EntityHasPadding` below)* → `SurfaceId` (`AssetId`, 4B) →
  `Angle` (`Vector3`, 12B) → `Position` (`Vector3`, 12B) → `Scale` (`Vector3`, 12B) →
  `ColorMultiplier` (4 floats R/G/B/A, 16B) → `SeeThroughSpeed` (`float`, 4B) → `ModelId` (`AssetId`,
  4B) → `AnimListId` (`AssetId`, 4B). 72 bytes total, 76 with padding.
- `DynaAssetPrefix.Read/Write`: `DynaType` (`uint`, 4B) → `Version` (`short`) → `Handle` (`short`).
  8 bytes total.

Call these; do not re-read their bytes field-by-field yourself.

### `AssetFields.Populate(asset, header, debug)` — every codec's first line

Copies `Id`, `Type`, `Name`, `FileName` (from `header`/`debug`) and `Physical.Type`, `Physical.Flags`,
`Physical.Alignment` (header-sourced physical fields). Nothing about a type's own payload — that
starts immediately after this call.

### Links — `Link.cs`, `Parameter.cs`, `LinkSerialization.cs`

One `Link` is exactly 32 bytes: `SourceEvent` (`short`) → `DestinationEvent` (`short`) →
`DestinationAssetId` (`AssetId`, 4B) → `Params` (exactly 4 × `Parameter`, 4B each — `RawParameter`
for unknown bytes, `FloatParameter`/`IntParameter`/`AssetIdParameter` where the meaning is known) →
`ParamWidgetAssetId` (`AssetId`, 4B) → `CheckAssetId` (`AssetId`, 4B).
`LinkSerialization.Read(asset, reader, count)`/`.Write(asset, writer)` handle the whole array; call
`Read` wherever your type's layout actually places its links (see
[Wire up the codec](#wire-up-the-codec) — not necessarily right after `LinkCount`).

### Generic shape fallbacks & `ShapesByType` (`src/EvilHop/Assets/Fallbacks/GenericAssets.cs`, `src/EvilHop/Assets/Serialization/AssetCodecs.cs`)

Every `AssetType` without a concrete codec (or whose concrete codec declares a game unsupported) falls
back to a generic reader for its **shape**: `GenericAsset` (plain `Asset`, whole slice unparsed),
`GenericBaseAsset`, `GenericEntityAsset`, `GenericDynaAsset`, `GenericPayloadAsset`. `ShapesByType`
is the table that says which shape a not-yet-typed member should degrade to; it only needs an entry
for `BaseAsset`/`EntityAsset`/`DynaAsset`/`Payload`-shaped types. **A plain-`Asset`-shaped type needs
no `ShapesByType` entry at all** — the ungated `Fallback` handler (`ReadPlain`/`WritePlain`, which is
exactly what `GenericAsset` does anyway) already produces the right degraded behavior. `MarkerAsset`
and `LODTableAsset` are both examples of this: neither appears in `ShapesByType`.

### `FormatProfile` — per-game quirks (`FormatProfile.cs`, `*Serializer.cs`)

| Game | `PlatformFieldOrder` | `EntityHasPadding` | Notes |
|---|---|---|---|
| `BFBB` | `PlatformNameRegionLanguage` | **true** | the one game with entity padding |
| `N100F` | `PlatformNameRegionLanguage` | false | |
| `TSSM` | `LanguageRegion` | false | |
| `Incredibles` | `LanguageRegion` | false | |
| `ROTU` | `LanguageRegion` | false | |
| `Ratatouille` | `LanguageRegion` | false | |

Every `DefaultProfile` targets `Platform.GameCube` with `StreamDataHasPaddingField: true`. Access a
profile in a test via `{Game}Serializer.DefaultProfile` (e.g. `BFBBSerializer.DefaultProfile`).
`profile.Endianness` is `Big` on GameCube and `Little` otherwise — `EndianReader`/`EndianWriter` read
this from the profile automatically, you never branch on it yourself. Branch on `profile.Game` (an
equality check, same shape in `Read` and `Write` — see `DestructibleObjectAsset`) for genuine per-game
field presence differences; branch on `profile.EntityHasPadding` only via `EntityAssetPrefix`, which
already does it for you.

### Raw I/O — `EndianReader`/`EndianWriter`, `AssetId`

Reader: `ReadInt16`/`ReadInt32`/`ReadUInt32`/`ReadSingle`/`ReadVector3`/`ReadAssetId`/`ReadBytes(n)`/
`ReadRemainingBytes()`. Writer: `Write(short/int/uint/float/Vector3/AssetId value)`. All big-endian on
every `DefaultProfile` seen so far (GameCube). `AssetId` is a `readonly record struct` wrapping a
`uint Value`; `AssetId.None` is the zero value; `AssetId.FromName(name)`/`FromName(name, type)`
compute one from a name (only relevant to `Asset.CalculateId()`, not to a codec).

### Worked precedents, condensed

Each of these is the *entire* pattern for its case — you should not need to open the source file to
use it, only to see more context if something here doesn't match your case.

- **Plain `Asset`, no header at all** (`MarkerAsset`): `AssetFields.Populate`, then the type's own
  fields directly, then `SetUnparsedTail(reader.ReadRemainingBytes())`. No `ShapesByType` entry.
- **A stored count deriving from a collection, physical only** (`BaseAsset.LinkCount`, and the same
  shape for `LODTableAsset`'s leading `int32`): override-clears-on-match property (shown above under
  `BaseAsset`), codec reads the count, builds the collection, then sets `Physical.Count` back to the
  collection's length so it re-derives.
- **Proven-always-zero padding, discarded rather than modelled** (`CounterAsset`, 2 bytes after
  `InitialValue`): `reader.ReadInt16(); // 2 bytes of padding, always zero` on read, `writer.Write(
  (short)0); // padding` on write. No property at all — there is nothing here worth exposing.
  Reach for this only once you've confirmed the bytes are genuinely constant (see
  §[Validate the layout](#validate-the-layout-before-writing-the-class)); until then, keep the field and mark it unknown
  instead (next bullet).
- **An always-present field with no known meaning, kept physical** (`HangableAsset.HangFlags`,
  `LODTableEntry.Flags`): a plain `uint`/similar property doc-commented `Unknown.`, with no attempt at
  semantics. On an `Asset` subclass this goes through a dedicated `IPhysicalFooAsset` interface,
  exactly like `HangFlags`; on a plain nested record with no `Physical` split of its own (an
  `LODTableEntry`), it is simply a public property — see §2 above.
- **Per-game field presence** (`DestructibleObjectAsset`'s six BFBB-only trailing `AssetId`s): the
  exact same `if (profile.Game == GameVersion.BFBB) { ... }` block, field-for-field symmetric, in both
  `Read` and `Write`. Never branch on `profile.EntityHasPadding` or any other derived quirk for this —
  branch on `Game` directly so the intent reads as "this game's layout has different fields," not
  "this incidental flag happens to correlate."

## Validate the layout (before writing the class)

The wiki is allowed to be wrong. Any layout claim you can **prove from the file** — offsets, field
sizes, constant values, `baseType` — must be checked against real bytes before you commit to a model.

### Run `locate-asset.cs` first

```
dotnet run --file .claude/skills/implementing-an-asset/scripts/locate-asset.cs -- LODT --entry-size 32
```

For every game whose committed `corpus/{game}.json` records the tag, this prints in one shot: how many
archives/builds carry it, an exemplar archive path, that archive's real `AHDR` fields (`id`/`offset`/
`size`/`plus`/`flags`) read directly from the bytes, and a hex dump of the payload at that offset. Pass
`--entry-size <n>` once you have a guess for a fixed record size — it also decodes the payload's
leading 4 bytes as a big-endian count and checks `4 + count × n` against the declared size, which is
exactly the arithmetic you'd otherwise do by hand to confirm a stride. This one command replaces
manually chaining `reading-corpus-inventory`'s `exemplar` and `reading-hip-bytes`' `findall`/`seek`/
`bytes` per game — run `--help` for every option (restricting to specific games, picking a later
occurrence, dumping more bytes, pointing at a non-default `corpus/`/`artifacts/`).

It finds `artifacts/` for you, including from a **worktree** — `artifacts/` is gitignored, so a linked
worktree checkout never has its own copy the way the main checkout does. The script tries `<repo>/
artifacts` first, then asks git for the shared `.git` common directory (`git rev-parse
--git-common-dir`, which resolves to the main checkout regardless of which worktree you run it from)
and tries `artifacts/` next to that. If neither exists, it says so plainly rather than guessing — pass
`--artifacts-dir` at your real copy, or tell the user it's missing.

Reach for `reading-hip-bytes`/`reading-corpus-inventory` directly only when you need something this
script doesn't give you: a specific build's archive rather than the recorded exemplar, more than one
occurrence in the same file (`--index` gets you *which* occurrence, not a full list with context),
free-form offset walking once you already know roughly where you're going, or a field the
leading-count guess doesn't fit (e.g. no leading count at all).

### Sweeping every occurrence

One exemplar per game proves a layout exists, not that it holds everywhere. Before relying on "always
zero" or "always the same size", sweep every occurrence: a throwaway `dotnet run --file` script in
`dump/` (gitignored) with `#:project ../src/EvilHop/EvilHop.csproj` can `Archive.Load` each archive,
`OpenAssets()`, and read every asset of the type. A type without a codec yet parses as its generic
shape, so `GetUnparsedTail()` on the `EntityAsset`/`BaseAsset` is exactly the bytes after the shared
prefix - tabulate sizes, discriminator bytes, and which regions are ever nonzero from that.

Two corpus quirks to account for when you do:

- **N100F's platform isn't sniffable.** Every N100F build's `PACK` flags carry no platform bits, so
  `Serializer.Sniff` falls back to GameCube and Xbox/PS2 archives read their asset fields with the
  wrong byte order. Round trips stay byte-exact, but field values don't. Derive the platform from the
  archive's `artifacts/` path (`GC`/`PS2`/`XBOX`) and override it -
  `Serializer.Create(sniffed with { Platform = ... })` - before interpreting any N100F field value.
- **Leftover developer archives.** BFBB's `gl/Working/` and `gl/New Folder/` directories, and some of
  its Xbox `db` archives, are developer leftovers the game never loads, and may predate format changes
  (e.g. BFBB's entity padding). They're low priority: note what they do, but don't bend a model to fit
  them - an asset that fails to parse degrades to its generic shape with a diagnostic, which is fine.

### Proven-from-file facts vs. gameplay-only facts

Treat these differently:

- **Check on disk.** Offsets, sizes, field order, padding, `baseType` bytes, whether a region is
  constant (usually zero), whether a field is genuinely present in a given build. These are layout
  facts; the bytes decide them.
- **Do not check on disk, and do not build a model on.** What a flag *does* in-game ("Can Hit Walls"),
  which event a link "really" maps to, which types a given flag "is used by". These are behavior, only
  clear through gameplay, and the wiki is the only (unverified) source. Keep them as XML doc remarks on
  the property, not as facts your model depends on.

### What the validation usually turns up

- **Offsets that don't line up.** E.g. a wiki `struct` that lists a field without counting padding a
  preceding tab could. Where a `<tabber>` shows per-game structs with *different* offsets for the same
  field (see `BOUL` — gravity is `0x54` in BFBB but `0x50` elsewhere, because the 4-byte entity
  padding differs), confirm which offset the game you're writing for actually uses.
- **"Always" values that aren't.** `EntityAsset`'s entity padding is BFBB-release only; the profile
  carries the `EntityHasPadding` switch for exactly this. Treat a wiki "usually X" as a hypothesis,
  not a fact, until `locate-asset.cs`'s dump confirms it across more than one game.
- **A real layout the wiki models with a guess.** `ATBL` marks its `Effects` count `unknown`; look at
  actual files (`--entry-size` on the guessed record size) before modelling the count. A discriminated
  union — `CAM`'s per-`Cam-Type` region is a real example — can only be decoded once you've located and
  read the discriminator byte; a wiki that lists every possibility without saying which is active is
  telling you to find that byte first.
- **A field the wiki records that only shows up in some games** (`LODT`'s `flags`, absent pre-TSSM):
  run `locate-asset.cs` against every game the wiki claims and let the declared-size-vs-stride check
  tell you which games actually carry the extra bytes, rather than trusting the wiki's game list at
  face value.

## Write the asset class

Concrete example for an `EntityAsset`-shaped type (adapt for `BaseAsset`/`DynaAsset`/plain `Asset` per
§[Reference](#reference-everything-a-codec-needs) — drop the entity bits if base, drop the base bits
too if a plain `Asset`, and see `DynaAsset`'s two-level dispatch if dyna):

```csharp
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>XML docs pulling in any wiki-provided behavioral description.</summary>
/// <remarks><seealso href="...wiki URL..."/></remarks>
public sealed class FooAsset : EntityAsset, IHasModel
{
    /// <summary>The logical meaning of the first type-specific field.</summary>
    public float Gravity { get; set; }

    // Determined values stay physical only (on the EntityAsset/BaseAsset interfaces), not here.

    // A trait the type genuinely uses:
    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
}
```

Rules to follow:

- **Derive from the right base.** Do not reach for traits or `IPhysical*` members unless the type
  genuinely is entity/base-shaped (validated above).
- **Determined fields stay physical.** Do not re-expose `BaseId`, `Type`, `LinkCount` as logical
  properties. They live on the `IPhysical*` interfaces already.
- **A trait projects, never stores.** `IHasModel.ModelId` reads/writes `Physical.ModelId`; it does not
  hold its own `AssetId`. (The `EntityAsset` backing fields are `private protected` so a derived type
  can project them directly.)
- **Negative flags are inverted at the trait boundary** if you ever add one (physical keeps the stored
  polarity; the trait exposes the positive sense).
- **Unknown / unparsed bytes are a preserved region, not a class family.** Your new class automatically
  inherits `GetUnparsedTail()`/`SetUnparsedTail()` from `Asset`. If your codec stops reading partway
  (e.g. an `unknown` tail you can't model yet), the remainder goes in the unparsed tail — byte-exact
  round trip is preserved, and you can model more later without breaking fidelity.
- **`Validate()`-able facts are `Validate()` findings, not exceptions.** The library has no
  `Validate()` on assets yet — the `EvilHop.Validation` namespace (and its `src/EvilHop/Validation/`
  folder) is reserved for it but does not exist yet. If you find a rule worth recording
  (a field the wiki says should always be 0, a `BaseFlags.Valid` check), note it as a TODO for the
  validation layer / `Validate()` rather than throwing at read or write time. Do not add a
  `Validate()` override unless one already exists to extend.

## Wire up the codec

`AssetCodecs.cs` registers a codec per `AssetType` (a reader + a writer), seeded at static init with a
generic per-shape handler. Your concrete codec **overwrites the seed** for your type. The `Read`/`Write`
logic itself lives as `internal static` methods on the asset class, not inline in `AssetCodecs`:

```csharp
// FooAsset.cs
internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion> { GameVersion.BFBB };

internal static FooAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
{
    var asset = new FooAsset();
    AssetFields.Populate(asset, header, debug); // header-sourced Id/Type/Name/...
    BaseAssetPrefix.Read(asset, reader);        // shared 8-byte header
    EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding); // if entity
    // ... read FooAsset's own fields with reader, e.g. reader.ReadSingle()
    // ... call the shared link reader where links appear in THIS type's layout
    asset.SetUnparsedTail(reader.ReadRemainingBytes()); // anything left over
    return asset;
}

internal static void Write(FooAsset asset, EndianWriter writer, FormatProfile profile)
{
    BaseAssetPrefix.Write(asset, writer);
    EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);
    // ... write FooAsset's own fields
    writer.Write(asset.GetUnparsedTail()); // byte-exact for unparsed remainder
}
```

```csharp
// AssetCodecs.cs, in RegisterConcreteCodecs()
Register(AssetType.Foo, FooAsset.Read, FooAsset.Write, FooAsset.SupportedGames);
```

Key points:

- **`AssetFields.Populate` first, always.**
- **The prefix helpers are there for you** (§Reference) — do not re-read those bytes by hand.
- **Links are placed by the codec, not peeled off the end.** Links sit *near* the end, not at it —
  `PLYR` has a field after them — so your codec reads its own fields and calls the link reader where
  your layout puts the links. If your type's wiki page shows an `Event[]`/`links[linkCount]` trailer at
  a specific offset, that is where to read them. If you can't locate them, leave `Links` empty and set
  `Physical.LinkCount` (the "cannot locate them" half of its contract); if you do parse them, leave
  `LinkCount` alone and let it derive.
- **Register in the static constructor**, alongside (or just after) `RegisterGenericShapes()`. The
  `ShapesByType` table still seeds every type; your `Register` call overwrites your type's entry. You
  do **not** remove it from `ShapesByType` — the table is the default, the codec is the override. A
  plain-`Asset`-shaped type needs no `ShapesByType` entry at all (§Reference).
- **`Register` takes an optional `IReadOnlySet<GameVersion> games`.** Pass it whenever the type is only
  known to be read by some games — reading under any other game then falls back to the shape handler
  (or `Fallback` for a plain `Asset`), degrading gracefully instead of misreading. Expose it as
  `internal static IReadOnlySet<GameVersion> SupportedGames { get; }` on the asset class itself (every
  precedent does this), not as a literal at the call site.
- **Big codecs split into a `.Serialization.cs` partial.** Once a codec outgrows ~250 lines, move the
  `Read`/`Write`/`SupportedGames` members into a `FooAsset.Serialization.cs` partial beside `FooAsset.cs`
  in the same family folder — see `CameraAsset.Serialization.cs`, `AnimationTableAsset.Serialization.cs`,
  `CutsceneAsset.Serialization.cs`.
- **Write what you read, and nothing else.** A codec that reads a field it doesn't write (or writes one
  it doesn't read) breaks the round trip. The round-trip test below is what catches this.

## Tests

Follow the pattern in `tests/EvilHop.Tests/Assets/<Family>/{Foo}AssetTests.cs`, mirroring the asset's
family folder (e.g. `tests/EvilHop.Tests/Assets/Objects/HangableAssetTests.cs`,
`DestructibleObjectAssetTests.cs`, `MarkerAssetTests.cs`). The minimum:

1. **Read produces your concrete type.** `Assert.IsType<FooAsset>(Read(AssetType.Foo, bytes))`.
2. **Read populates your fields** from a hand-built byte array (decode the bytes you put in).
3. **Read-then-write reproduces the input bytes.** `Assert.Equal(data, Write(Read(type, data)))` for a
   representative payload, and for one with an unparsed tail. This is your byte-exact round trip.
4. **Profile variation** where the wiki shows per-game differences (e.g. run the same bytes under a
   BFBB `EntityHasPadding=true` profile and a non-BFBB one, asserting the shift is real; or, for a
   `SupportedGames`-gated type, assert an unsupported game degrades to its generic shape).
5. If you add a trait, assert it round-trips into the physical storage (and, for negative/bit-mapped
   ones, that setting the trait toggles exactly the stored bit).

Also look at `tests/EvilHop.Tests/Assets/` for unit tests of a concrete asset's properties. Run:
`dotnet build` and `dotnet test` from the repo root. Fix every analyzer message — the project is
turned up and treats them as findings.

## Etiquette: what to expose vs. ask

Deciding which fields are meaningful and deserve helper properties is genuinely hard from a wiki
alone, and **it is cheap to change a Physical-only field to a full tier later** — so err on the side
of *not* promoting a field to logical until you have reason to.

Concretely, when a field's purpose is unclear from the wiki:

- **Default: physical, with a clear XML doc.** Put an independent-but-obscure field (padding, an
  `unknown`, a rarely-touched value) on the physical surface as a plain stored field with a doc comment
  that says what the wiki claims and that it's unverified. This keeps the round trip honest and the
  public surface small.
- **Ambiguous-but-plausibly-meaningful: promote to logical, and say why.** If the wiki names a field
  with a real meaning a modder would want (e.g. `Gravity`, `FOV`, `count`), make it logical. A doc
  comment noting the uncertainty is better than hiding it.
- **Genuinely stuck** (can't tell a real field from padding, or two mutually exclusive wiki layouts
  with no winner the bytes resolve): expose your best guess per the above defaults, and flag it to the
  user for confirmation. When in doubt, ask *before* promoting an ambiguous field to logical, or note
  after implementing that it's a candidate — both are acceptable; the cost of reversing is small either
  way. Prefer asking when the field would change the public API surface; prefer "physical + doc note"
  when it wouldn't.

The decision rule that keeps this moving: **promote when you have evidence (bytes or a clear wiki
meaning); keep physical when you only have a name.** Don't block on perfection — physical↔logical
promotion is meant to be cheap.

## EvilHop.Corpus validation — deferred

The strongest verification — "which types actually use which traits," "is this field always zero for
this type," "does the minimum observed size match the shared prefix," and the per-type "which
`CollisionFlags` bits are ever set" — needs **asset-field extraction in EvilHop.Corpus, which does not
exist yet.** Today's `corpus/*.json` only records block-layer facts (`AssetHeader.Type` occurrences,
which is what `locate-asset.cs` and `reading-corpus-inventory` query) — it has no notion of a typed
asset's individual fields.

Until then, and for the type you just implemented, `locate-asset.cs`'s dump against a real archive
(see §[Validate the layout](#validate-the-layout-before-writing-the-class)) is your validation. At minimum confirm:

- The `baseType` byte and shape are what you modeled.
- Each "usually constant" field you relied on (`pflags` = 0, padding, `SeeThroughSpeed` = 255, ...) is
  in fact constant in the archives you checked.
- Which `SurfaceId`/`ModelId`/`AnimListId` are non-zero for your type — this is the evidence that
  decides whether it earns the corresponding trait, in the absence of the corpus query.

Once asset extraction exists, run these as invariants instead (leave this section as the checklist):

- **`entityFieldIsUnusedForType`** — per `EntityAsset`-shaped type, whether `SurfaceId`/`ModelId`/
  `AnimListId` are always zero and which `CollisionFlags` bits ever appear. This is what *replaces*
  the wiki-sourced "Used by" trait lists — the lists you read today are unverified guesses.
- **Size-floor check** — every `EntityAsset`-shaped type's minimum observed `AHDR.Size` ≥ shared prefix
  (72 bytes, 76 in BFBB) + `LinkCount × 32` for its smallest `LinkCount`. A violation means a wrong
  `ShapesByType` entry or a real per-game divergence.
- **Per-type constant-field check** — the "always 0 / always 255" claims you relied on, asserted as
  invariants across the whole corpus rather than the handful of archives you hand-checked.

Do not treat the hand-check as a substitute for these; the corpus query is the actual proof and it is
out of scope until extraction lands.

## Checklist

- [ ] Got the wiki page or layout doc from the request (pasted, attached, or linked) — there is
  nothing to look up locally.
- [ ] Determined which base class (`Asset`/`BaseAsset`/`EntityAsset`/`DynaAsset`) from the wiki +
  validated `baseType` byte.
- [ ] Ran `locate-asset.cs` for every game the wiki claims, and reconciled any discrepancy against its
  output (off-by-padding offsets, per-game field presence, a stride that doesn't match the declared
  size).
- [ ] Wrote the asset class deriving from the correct base, with logical + physical + traits correct
  (§[Reference](#reference-everything-a-codec-needs) patterns applied, not re-derived from scratch).
- [ ] Registered the codec in `AssetCodecs` (read + write, `SupportedGames` if not every game), calling
  the prefix helpers, placing links correctly, preserving the unparsed tail.
- [ ] Built and passed `dotnet build` / `dotnet test`; fixed analyzers; added the round-trip + field
  tests.
- [ ] Ran an `EvilHop.Corpus` sweep across every claimed game/platform confirming the type actually
  parses (not just round-trips) with zero diagnostics; checked off or deferred the invariants above.
- [ ] Made the physical↔logical / ambiguity calls per §[Etiquette](#etiquette-what-to-expose-vs-ask).

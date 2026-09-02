---
name: implementing-an-asset
description: Use this skill when implementing a single concrete HIP asset type in EvilHop — turning a Heavy Iron Modding wiki page (or any provided layout doc) into the asset class, its two surfaces (logical + IPhysical*), its codec registration, and its tests. Covers validating the wiki's field claims against real archive bytes with reading-hip-bytes, and what to leave for EvilHop.Corpus-based validation once that exists.
---

# Implementing a single Asset

You are turning one asset type's layout into a typed EvilHop asset. This skill is the standardized
procedure for doing that once, for one type, against one wiki page like the ones in `Docs/wiki/Assets/`
(`BOUL`, `BUTN`, `CAM`, `CNTR`, `ATBL`, ...).

The goal is a byte-exact round trip for the type **and** a clean, discoverable public API. The two
halves that follow — "read the layout" and "decide what belongs on which surface" — both rest on the
design rules in `Docs/Asset Layer Design.md` (read it once before starting; §4 "Two surfaces" is the
section this skill operationalizes).

**Workflow at a glance:** read the wiki → validate every provable field claim against real bytes →
write the asset class + codec → wire up traits and `IPhysical*` → build + test → (later) run the
EvilHop.Corpus invariants. The middle piece — validating against bytes before writing code — is the
step almost everyone skips, and it is where the wiki's mistakes come out.

## When to use / not use this

- **Use** for a type that has a wiki page or provided layout describing its fields (e.g. an
  `EntityAsset`-shaped type like `BOUL`/`BUTN`, a `BaseAsset`-shaped type like `CAM`/`CNTR`, a
  `DynaAsset` subtype).
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
| Generic shape fallbacks | `src/EvilHop/Assets/GenericAssets.cs` |
| Codec registry | `src/EvilHop/Serialization/AssetCodecs.cs` (incl. `ShapesByType` table) |
| Type enum | `src/EvilHop/Common/AssetType.cs` |
| Tests | `tests/EvilHop.Tests/Assets/`, `tests/EvilHop.Tests/Serialization/AssetCodecsTests.cs` |

## The three-part decision that precedes any code

Before you write a single property, settle three things. The first two are `Docs/Asset Layer Design.md`
§4 rules restated; the third is the honest "we don't know yet" answer.

### 1. Which parent does it derive from?

This is not a guess. The wiki's type classifier (`Binary`/`Base`/`Entity`/`RenderWare`) says, and the
archive's `baseType` verifies it (see §[Validate the layout](#validate-the-layout)):

- **`Asset`** — no known shape, everything is unparsed. Almost never the final state.
- **`BaseAsset`** — has the 8-byte header (`BaseId`, `BaseType`, `LinkCount`, `BaseFlags`). E.g. `CAM`,
  `CNTR`.
- **`EntityAsset : BaseAsset`** — `BaseAsset` header + the 0x54 entity prefix (flags, `Angle`,
  `Position`, `Scale`, color multipliers, `ModelId`, `AnimListId`, ...). E.g. `BOUL`, `BUTN`.
- **`DynaAsset : BaseAsset`** — `BaseAsset` header + `uint Type, short Version, short Handle`, then
  the dyna's own fields. `DYNA` subtypes dispatch twice (see `AssetCodecs` remarks).

Concrete types live right next to their base (`src/EvilHop/Assets/`). Match the naming: an asset that
is a `BaseAsset` gets a `FooAsset : BaseAsset` class; one that is an `EntityAsset` gets
`FooAsset : EntityAsset`.

### 2. Which fields are logical, which are physical, which are traits?

The two-question test from §4 of the design doc, applied in order:

1. **Is the value already determined by something else on the object?** → physical, and *must not*
   also be logical. Known members: `BaseId` (a second copy of `Id`), `Type` (implied by the class),
   `LinkCount` (implied by `Links`), and `DynaAsset`'s `DynaType`/`Version` where a concrete dyna class
   implies them.
2. **Otherwise, is it of real interest to someone editing the game object?** → logical if yes;
   physical as a plain stored field if no. `Alignment`, `PFlags`, `Subtype`, `SeeThroughSpeed` sit here
   today — real, independent, rarely touched.

Fields the shared entity layout reserves for a whole family but only some types use (`SurfaceId`,
`ModelId`, `AnimListId`, and the `CollisionFlags` bits) are **physical**, and a type that genuinely
uses one opts in through a trait interface (`IHasSurface`, `IHasModel`, `IHasAnimList`, `IGrabbable`)
that projects onto the physical storage — it never stores a copy.

**Do not trust the wiki's "Used by" lists** for deciding which traits a type gets. Those lists are
plausible but unverified (the design doc says so explicitly, §9). The *decision* is deferred to
EvilHop.Corpus once asset extraction exists; in the meantime, see §[etiquette for IPhysical* and
unknowns](#etiquette-what-to-expose-vs-ask).

### 3. Which games does it diverge across? (and which is "the" game to write first)

The wiki pages are inconsistent here: some are a single struct (`ATBL`, `CNTR`, `CAM`), some are a
`<tabber>` with a different struct per game group (`BOUL`), and one (`BUTN`) mixes per-game inline
notes into a single table. Per `Docs/Asset Layer Design.md` §7 rule 5, an asset type is **one class
across all games** — per-game differences are one class with nullable/conditional fields or
`FormatProfile` switches, never a per-game subclass. But you do not implement all games at once.

**Write the first codec for exactly one game** (prefer BFBB, or the game the wiki page's primary
struct targets), match that game's `FormatProfile` exactly, and get its byte-exact round trip green.
Then, if the wiki shows other games differing, add the conditional fields/switches and validate each.
A round-trip across every game the class claims to support is what actually proves the divergence
handling — a codec that over-claims games will silently misread on disk.

## Validate the layout (before writing the class)

The wiki is allowed to be wrong. `Docs/Divergences from Community Documentation.md` documents places it
already is. Any layout claim you can **prove from the file** — offsets, field sizes, constant values,
`baseType` — must be checked against real bytes before you commit to a model. Use
`reading-hip-bytes` for this; it reads the raw archive without the library getting in the way.

### Proven-from-file facts vs. gameplay-only facts

Treat these differently:

- **Check on disk.** Offsets, sizes, field order, padding, `baseType` bytes, whether a region is
  constant (usually zero), whether a field is genuinely present in a given build. These are layout
  facts; the bytes decide them.
- **Do not check on disk, and do not build a model on.** What a flag *does* in-game ("Can Hit Walls"),
  which event a link "really" maps to, which types a given flag "is used by". These are behavior, only
  clear through gameplay, and the wiki is the only (unverified) source. Keep them as XML doc remarks on
  the property, not as facts your model depends on.

### The validation loop

1. Pick a real archive that contains the type. Use `reading-corpus-inventory` `exemplar` to find one
   (e.g. `exemplar AssetHeader.Type CAM`), or guess a known path and fall back to listing.
2. Find an `AHDR` for the type. The type is a big-endian FourCC, **not** ASCII, so search for the raw
   bit pattern: `findall 0x43414D20` for `CAM `, `findall 0x434E5452` for `CNTR`, etc. (`0x41324...`
   — see `AssetType.cs` for each enum's value.)
3. Read the `AHDR` entry: `id` (u32), `type` (u32, the FourCC), `offset` (u32), `size` (u32). Follow
   `offset` to the payload with `seek $ bytes N`.
4. Walk the wiki's offsets on the real bytes with `u8`/`u16`/`u32`/`f32`/`ascii`/`bytes`. Confirm each:
   the `baseType` byte at `+4` matches the wiki's base-type table; each field lands where the wiki
   says; each "usually constant" value (`pflags` = 0, `SeeThroughSpeed` = 255, `Valid Flags` bytes)
   actually holds.
5. Purge the payload to its declared `size` and confirm that's where the next thing starts (or that
   the trailer links/events fit).

Worked example — a `CNTR` in `bfbb/prototype_2003-10-01/GC/NTSC-U/US/b1/b101.HIP`:

```
hipbytes -- b101.HIP findall 0x434E5452          # → AHDR with the CNTR type FourCC
hipbytes -- b101.HIP seek 0xD18 u32 5            # id=0x1EF6C987 type=CNTR offset=0xB3A0 size=0x14C plus=4
hipbytes -- b101.HIP seek 0xB3A0 bytes 16
  0000B3A0  1E F6 C9 87 16 0A 00 1D  00 0A 00 00 01 9A 00 04
```

Decoded: `BaseId`=0x1EF6C987 (matches AHDR ✓), `BaseType`=0x16 (= CNTR's wiki base type ✓),
`LinkCount`=10, `BaseFlags`=0x001D, then `count`=10 (u16 at +8 ✓), 2 padding bytes ✓, events from `+0xC`.
A `CAM ` worked the same way and corroborated its full table — position/forward/up/left vectors, the
`Offset Start Frames`=30 / `Offset End Frames`=45 shorts, `FOV`=85, and the `Valid Flags` byte `0x8F`
exactly as the wiki's "byte 4 usually 0x8F" says.

### What the validation usually turns up

- **Offsets that don't line up.** E.g. a wiki `struct` that lists a field without counting padding a
  preceding tab could. Where a `<tabber>` shows per-game structs with *different* offsets for the same
  field (see `BOUL` — gravity is `0x54` in BFBB but `0x50` elsewhere, because the 4-byte entity
  padding differs), confirm which offset the game you're writing for actually uses.
- **"Always" values that aren't.** `EntityAsset`'s entity padding is BFBB-release only; the wiki flags
  it and the profile carries the `EntityHasPadding` switch. Treat a "usually X" in an authoritative
  tone as a hypothesis.
- **A real layout the wiki models with a guess.** `ATBL` marks its `Effects` count `unknown`; look at
  actual files before modelling the count. `CAM`'s union-of-camera-types region can only be decoded
  from the `Cam Type` byte at `+0x84` — a wiki that lists every possibility ("Follow only"/"Shoulder
  only"/...) without saying which is active is telling you to read the discriminator first.

## Write the asset class

Concrete example for an `EntityAsset`-shaped type (adapt for `BaseAsset`/`DynaAsset` — drop the entity
bits if base, and see `DynaAsset`'s two-level dispatch if dyna):

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
  `Validate()` on assets yet (`src/EvilHop/Validation/` is empty). If you find a rule worth recording
  (a field the wiki says should always be 0, a `BaseFlags.Valid` check), note it as a TODO for the
  validation layer / `Validate()` rather than throwing at read or write time. Do not add a
  `Validate()` override unless one already exists to extend.

## Wire up the codec

`AssetCodecs.cs` registers a codec per `AssetType` (a reader + a writer), seeded at static init with a
generic per-shape handler. Your concrete codec **overwrites the seed** for your type.

```csharp
static AssetCodecs()
{
    // RegisterGenericShapes();  // (already runs)
    Register<FooAsset>(
        AssetType.Foo,                      // your AssetType member
        (reader, header, debug, profile) =>
        {
            var asset = new FooAsset();
            AssetFields.Populate(asset, header, debug); // header-sourced Id/Type/Name/...
            BaseAssetPrefix.Read(asset, reader);        // shared 8-byte header
            EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding); // if entity
            // ... read FooAsset's own fields with reader, e.g. reader.ReadSingle()
            // ... call the shared link reader where links appear in THIS type's layout
            asset.SetUnparsedTail(reader.ReadRemainingBytes()); // anything left over
            return asset;
        },
        (asset, writer, profile) =>
        {
            BaseAssetPrefix.Write(asset, writer);
            EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);
            // ... write FooAsset's own fields
            writer.Write(asset.GetUnparsedTail()); // byte-exact for unparsed remainder
        });
}
```

Key points:

- **`AssetFields.Populate` first, always** — it copies `Id`, `Type`, `Name`, `FileName`, `Flags`,
  `Alignment` from the `AHDR`/`ADBG` blocks and is what every codec starts with.
- **The prefix helpers are there for you** — `BaseAssetPrefix`, `EntityAssetPrefix`, `DynaAssetPrefix`
  in `AssetPrefixes.cs`. Do not re-read those bytes by hand.
- **Links are placed by the codec, not peeled off the end.** `Docs/Asset Layer Design.md` §4 ("Parse
  order, and where links actually are") is explicit: links sit *near* the end, not at it — `PLYR` has
  a field after them — so your codec reads its own fields and calls the link reader where your layout
  puts the links. If your type's wiki page shows an `Event[]`/`links[linkCount]` trailer at a specific
  offset, that is where to read them. If you can't locate them, leave `Links` empty and set
  `Physical.LinkCount` (the "cannot locate them" half of its contract); if you do parse them, leave
  `LinkCount` alone and let it derive.
- **Register in the static constructor**, alongside (or just after) `RegisterGenericShapes()`. The
  `ShapesByType` table still seeds every type; your `Register` call overwrites your type's entry. You
  do **not** remove it from `ShapesByType` — the table is the default, the codec is the override.
- **Write what you read, and nothing else.** A codec that reads a field it doesn't write (or writes one
  it doesn't read) breaks the round trip. The round-trip test below is what catches this.

## Tests

Follow the pattern in `tests/EvilHop.Tests/Serialization/AssetCodecsTests.cs`. The minimum:

1. **Read produces your concrete type.** `Assert.IsType<FooAsset>(Read(AssetType.Foo, bytes))`.
2. **Read populates your fields** from a hand-built byte array (decode the bytes you put in).
3. **Read-then-write reproduces the input bytes.** `Assert.Equal(data, Write(Read(type, data)))` for a
   representative payload, and for one with an unparsed tail. This is your byte-exact round trip.
4. **Profile variation** where the wiki shows per-game differences (e.g. run the same bytes under a
   BFBB `EntityHasPadding=true` profile and a non-BFBB one, asserting the shift is real).
5. If you add a trait, assert it round-trips into the physical storage (and, for negative/bit-mapped
   ones, that setting the trait toggles exactly the stored bit).

Also look at `tests/EvilHop.Tests/Assets/` for unit tests of a concrete asset's properties. Run:
`dotnet build` and `dotnet test` from the repo root. Fix every analyzer message — the project is
turned up and treats them as findings.

## Etiquette: what to expose vs. ask

`Docs/Asset Layer Design.md` §4 and the task notes agree: deciding which fields are meaningful and
deserve helper properties is genuinely hard from a wiki alone, and **it is cheap to change a
Physical-only field to a full tier later** — so err on the side of *not* promoting a field to logical
until you have reason to.

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
meaning); keep physical when you only have a name.** Don't block on perfection — the design explicitly
treats physical↔logical promotion as cheap.

## EvilHop.Corpus validation — deferred

The strongest verification — "which types actually use which traits," "is this field always zero for
this type," "does the minimum observed size match the shared prefix," and the per-type "which
`CollisionFlags` bits are ever set" — is meant to come from **asset-field extraction in EvilHop.Corpus,
which does not exist yet.** `Docs/Asset Layer Design.md` §9 lists these explicitly and they all depend
on that extraction landing.

Until then, and for the type you just implemented, validate once by hand with `reading-hip-bytes`
against a real archive (see §[Validate the layout](#validate-the-layout)) to at least confirm:

- The `baseType` byte and shape are what you modeled.
- Each "usually constant" field you relied on (`pflags` = 0, padding, `SeeThroughSpeed` = 255, ...) is
  in fact constant in the files you checked.
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
  invariants across the whole corpus rather than the handful of files you hand-checked.

Do not treat the hand-check as a substitute for these; the corpus query is the actual proof and it is
out of scope until extraction lands.

## Checklist

- [ ] Read `Docs/Asset Layer Design.md` (§3 session, §4 two surfaces, §7 divergence rules).
- [ ] Determined which base class (`Asset`/`BaseAsset`/`EntityAsset`/`DynaAsset`) from the wiki +
  validated `baseType` byte.
- [ ] Validated every provable field claim against real bytes with `reading-hip-bytes`; reconciled any
  wiki discrepancy (off-by-padding offsets, per-game `<tabber>` differences).
- [ ] Wrote the asset class deriving from the correct base, with logical + physical + traits correct.
- [ ] Registered the codec in `AssetCodecs` (read + write), calling the prefix helpers, placing links
  correctly, preserving the unparsed tail.
- [ ] Built and passed `dotnet build` / `dotnet test`; fixed analyzers; added the round-trip + field
  tests.
- [ ] Checked off or deferred the EvilHop.Corpus invariants above.
- [ ] Made the physical↔logical / ambiguity calls per §[Etiquette](#etiquette-what-to-expose-vs-ask).

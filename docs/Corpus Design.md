# Corpus Design

A ground-up design for the corpus subsystem: the `EvilHop.Validation` rule model in the library,
the `EvilHop.Corpus` tool that generates committed inventories from real archives, and the
`EvilHop.Tests.Inventory` suite that asserts the library against those inventories on CI.

---

## 1. What this subsystem is for

We have a large, private, gitignored pile of real game archives. They are the only ground truth we
have about a format nobody documented. Three things need to come out of them:

1. **A durable record of what the format actually contains**, small enough to commit, so that a
   claim about the format can be checked against evidence instead of memory.
2. **A regression net on CI**, so that a change to EvilHop that would break real archives fails a
   build on a machine that has never seen an archive.
3. **A place to measure hypotheses** — which archives are always loaded, what the per-type
   alignment table is, whether ATOC is really sorted — before they harden into library code.

Everything below follows from those three, plus one hard constraint the project already committed
to: *the tool records observations; tests assert them against current code.*

---

## 2. Four owners, one litmus test

| Owner | Owns | Never contains |
|---|---|---|
| `EvilHop` (`Validation/`) | What the format is. Every rule, every legal value, every known-violation tag, every observable. Declarations only. | Any knowledge of `artifacts/`, `corpus/`, file paths, or the shape of an inventory. |
| `EvilHop.Corpus` | Which archives exist, what each one is, how to read it, how to reduce many of them into a committed file. Plumbing only. | Any format knowledge or rule logic. |
| `corpus/*.json` | What was observed, expressed in primitives. | Verdicts, except two explicitly fenced sections (§7.4, §7.5). |
| `EvilHop.Tests.Inventory` | Asserting today's EvilHop against yesterday's observations. | Any read of `artifacts/`. |

Two test suites sit near each other and are easy to confuse, so they are named for what they read:
**`EvilHop.Tests.Inventory`** (a namespace under `EvilHop.Tests`, at
`tests/EvilHop.Tests/Inventory/`) reads `corpus/*.json` and asserts the library against it.
**`EvilHop.Corpus.Tests`** is the tool's own unit-test project and tests the generator's plumbing.

**The litmus test for where a piece of knowledge goes:** *would a user opening their own archive
want this?* A user wants to be told their checksum is wrong, that their `AHDR.type` isn't a type the
game knows, that a `Stackable` `SIMP` will crash. They do not want to know that BFBB ships 412
archives. The first kind is library knowledge. The second kind is corpus knowledge.

This test resolves the cases that would otherwise be arguable. Link parsing, for example: the
corpus needs parsed links to build an event table, and the tool *could* parse them itself, since the
layout is known. It must not — a user wants their links parsed too. The link facet stays empty until
EvilHop parses links, and that is recorded as a dependency, not worked around (§10.3).

---

## 3. The central problem, and the taxonomy that solves it

The requirement says rules should live in EvilHop so they aren't duplicated. The governing rule says
an inventory must never contain a value whose correctness depends on EvilHop's source. Taken
naively these collide: if the tool calls `archive.Validate()` and writes the verdicts, the inventory
is a recording of EvilHop's opinion, and a test that reads it is EvilHop grading its own homework.

The resolution is to split every rule into an **observation** (a fact about bytes, library-agnostic)
and a **judgement** (a rule, library-owned), and then classify rules by *where the judgement can be
re-run*.

The classifier is **the cardinality of the rule's distinct input space**, not the size of any one
input. This distinction is load-bearing and is easy to get wrong: `AHDR.id` versus the BKDR hash of
`ADBG.name` takes an input of `(name, type)`, which is a few dozen bytes — but BFBB alone holds on
the order of 777,000 assets, and asset names are near-unique by construction. Recording every
distinct input is recording the whole corpus. Per-item smallness buys nothing; only boundedness
does.

### 3.1 Replayable — bounded distinct inputs

The rule's input space is small and closed: a type code, a flag word, a version constant, a platform
string. The inventory records the distinct observed values with counts; the test re-runs the rule
against them, offline, on CI.

> `PVER.subVersion` is recorded as `{2: 412 occurrences}`. The rule "always 2" is never written to
> the file. The test pulls the rule from the library and asserts it holds for every recorded value.

Covers essentially all of invariant categories 2 (constants) and 3 (per-game constants), plus every
closed-enum, closed-set, and defined-bits rule, plus the reference-target-type rules of §10.2.
**Aim to put rules here.** A rule that is replayable costs nothing to change: correcting the
definition of an invariant changes only test-side code, and no archive is re-read (§8.3).

### 3.2 Reducible — unbounded input, bounded verdict

The rule ranges over per-asset or per-byte data the inventory cannot hold, but its outcome collapses
to a tiny ledger.

> `ADBG.checksum` versus the CRC-32/MPEG-2 of the asset data. The inventory records
> `{checked: 190233, held: 190226, violations: [7 rows]}`.
>
> `AHDR.id` versus `AssetId.FromName(name, type)`. The inventory records a distribution over which
> transform reproduced the stored ID, plus the unclassified rows (§10.4).

This bucket *is* a recorded judgement, so it needs a guard against the library silently changing
underneath it. That guard is **anchors**: alongside the ledger, the inventory carries a handful of
self-contained vectors that inline their own input — for checksums, a few assets small enough to
embed base64 alongside their stored checksum; for `AssetId`, a few `(name, type, storedId)` triples.
The test recomputes over the anchors, which pins the algorithm; the ledger covers scale. Anchor byte
budget is capped per rule (256 bytes per anchor, 16 anchors per rule), and anchors are chosen
deterministically (§7.4), so the set is stable across regenerations.

Anchors are the deliberate trade for the §3.1 guarantee: a bounded, reviewable amount of embedded
input in exchange for keeping the algorithm itself under test on a machine with no archives.

### 3.3 Non-replayable — nothing smaller than the archive will do

Round-trip fidelity, and gap-byte uniformity across a whole stream. These cannot be CI tests under
any encoding. They belong to the tool as an on-demand verb, and their *provenance* is recorded in a
fenced section so a regression shows up in a committed diff rather than vanishing (§7.5).

---

## 4. Observables: the mechanism that makes §3.1 mechanical

An **observable** is a named, primitive-valued projection over an archive. It is the single place
that declares where a value lives, used by both the runtime validator and the corpus recorder.

```csharp
namespace EvilHop.Validation;

public enum ObservableScope { Archive, Block, Asset, Layer, Link }
public enum ObservableCardinality { Enumerated, Summarized, Bitmask }
public enum ObservablePresentation { Number, Hex, Fourcc, Text, Bytes }

public sealed record Observable(
    string Id,                                  // "PVER.subVersion", "PLAT.platformId+platformName"
    ObservableScope Scope,
    ObservableCardinality Cardinality,
    ObservablePresentation Presentation,
    Func<ObservationSource, IEnumerable<object>> Select);
```

Most observables are declared by attribute on the member they read (§5.4); the rest — composites
spanning members or blocks — are declared explicitly. `ValidationCatalogue.Observables` is the union
of both and the single list every consumer reads.

Four constraints make this work, and each is enforced by a test in `EvilHop.Corpus.Tests` over the
catalogue via reflection:

1. **Primitives only.** `Select` may yield `uint`, `int`, `string`, `bool`, `byte[]`, or a tuple of
   those. Never a library enum, never a record, never an `AssetId`. `AHDR.type` yields the raw
   `uint`, never `AssetType`. This is the mechanical enforcement of the governing rule.
2. **Cardinality is declared, not discovered.** `Enumerated` writes a value list;
   `Summarized` writes `{min, max, count, distinct}` and no values; `Bitmask` writes the OR of every
   observed value.
3. **Asset-scoped observables default to `Summarized`.** `Enumerated` is opt-in and only for a
   provably closed value space — a type code, a flag word, an enum, a fixed-vocabulary string.
   `AHDR.id`, `AHDR.size`, `AHDR.offset`, `AHDR.plus` are `Summarized`; `ADBG.name` is projected.
   The writer additionally **caps** an `Enumerated` list at 512 distinct values and *fails
   generation* past it, so a mis-declared cardinality is a loud error rather than a 40 MB commit.
4. **High-cardinality fields are projected, not recorded.** `PCRT.createdDateString` has hundreds of
   distinct values and zero signal in any of them; its observable yields a *shape*
   (`"Www Mmm dd hh:mm:ss yyyy"`, `"…\n"`, `"other"`). `ADBG.name` yields `(length, charsetClass)`.
   The projection is a pure function of the bytes, so the recorded value stays library-agnostic.

Composite observables handle every correspondence rule in the invariant list without new machinery:
`PLAT.platformId+platformName` yields a tuple, and the recorded value set *is* the correspondence
table. Same for `game+PVER.clientVersion`, `AHDR.type+ADBG.alignment`, `AHDR.type+BASE.baseType`,
and `PFLG.flags+PLAT.platformId+PLAT.region+PLAT.language` (§11).

`Bitmask` cardinality collapses every "unused bits are never used" rule (`EntityFlags`,
`CollisionFlags`, `BaseAssetFlags`, `AssetFlags`, `PackFlags`) into one recorded `uint` per field
per game — a stronger statement than the wiki's, because it says which bits are *really* used rather
than which bits are named.

---

## 5. The library-side rule model

`src/EvilHop/Validation/` — currently an empty placeholder, and the home for all of this.

### 5.1 Severity

Severity is a statement about the *game*, not about our confidence. It is graded by what happens
when the archive is loaded by the engine it targets.

| Severity | Meaning |
|---|---|
| `Info` | Purely observational. Known to have no effect on the game. The value is odd, unexpected, or outside what shipped archives do, and it demonstrably does not matter. |
| `Warning` | Could cause problems, or the consequences are undocumented. Not expected to crash. This is also where states the retail engine ignores but a modified build honours belong — an asset type that only loads with an AR code is a `Warning`, not an `Error`. |
| `Error` | Known-unrecoverable. The game will fail to load the archive or will crash. Invalid block structure, a missing required block, a type the engine will dereference and fault on. |

The whole ladder gets used for one recurring shape: a field EvilHop models in memory that the
serializer does not write, or writes unconditionally. Whether that is `Info`, `Warning`, or `Error`
depends on how dangerous the discarded state is, so the rule picks per case rather than the field
having one blanket severity.

Severity is never recorded in an inventory. The inventory records the classification tag and the
count; the test recomputes severity from the library (§6).

### 5.2 `ValidationIssue` and where an issue happened

```csharp
public readonly record struct ValidationIssue(
    string RuleId,
    Severity Severity,
    IssueSite Site,
    string Message,
    string? Classification = null,           // non-null when a known violation matched (§6)
    IReadOnlyList<IssueSite> Related = null); // defaults to empty, never null
```

`Site` says exactly where the violation is. The property is named `Site` rather than `Context` to
keep it distinct from `ValidationContext` (§5.3), which describes the *archive* rather than the
*location*.

The set of places a violation can occur is closed and small, so `IssueSite` is a closed hierarchy of
records that **nest** rather than a single record with a field per possibility. Nesting is what
keeps it null-free: an asset-field site *contains* an asset site instead of carrying an unused
`AssetId`.

```csharp
public abstract record IssueSite
{
    public abstract string Describe();   // stable, human-readable locator
}

public sealed record ArchiveSite                                        : IssueSite;
public sealed record BlockSite(BlockPath Path)                          : IssueSite;
public sealed record BlockFieldSite(BlockPath Path, string Member)      : IssueSite;
public sealed record LayerSite(int Index, uint LayerTypeRaw)            : IssueSite;
public sealed record AssetSite(AssetId Id, uint TypeRaw, string? Name)  : IssueSite;
public sealed record AssetFieldSite(AssetSite Asset, string Member)     : IssueSite;
public sealed record LinkSite(AssetSite Owner, int Index)               : IssueSite;
public sealed record AssetGapSite(AssetSite Preceding)                  : IssueSite;
public sealed record StreamRegionSite(long Offset, long Length)         : IssueSite;
```

`BlockPath` is a small value type holding a sequence of `(tag, ordinal)` pairs, rendering as
`PACK/PLAT` or `LTOC/LHDR[3]`. It is what makes a block-level issue actionable in an archive with
sixty `AHDR`s.

The mapping from the invariant catalogue is total:

| Invariant category | Site |
|---|---|
| Block structure (required children, leaf childlessness, root sequence) | `BlockSite`, or `BlockFieldSite` on the parent's child property when a required child is absent |
| Constant and per-game constant fields | `BlockFieldSite` |
| Cross-block reconciliations (`PCNT` counts, `Plus` arithmetic) | `BlockFieldSite` on the field holding the wrong value, with the counterpart in `Related` |
| Asset object-model claims | `AssetSite` / `AssetFieldSite` |
| Link and event claims | `LinkSite` |
| Layer claims | `LayerSite` |
| Padding modelled as a field | `BlockFieldSite` |
| Gap bytes between assets in the stream | `AssetGapSite` |
| Alignment and offsets the model retains | `StreamRegionSite` |
| Archive-wide facts with no narrower home | `ArchiveSite` |

`AssetSite.Name` is the only nullable member, and legitimately so: `ADBG` can be absent, in which
case the asset genuinely has no name.

`Related` captures the relationship half of cross-block rules — `PCNT.assetCount` disagreeing with
the `AHDR` count reports its site on `PCNT.AssetCount` and lists the `DICT/ATOC` block as related —
and defaults to empty, so a rule with a single site carries no unused field.

Inventories record `Site.Describe()` as a witness string in violation ledgers. That string is a
*locator*, not an asserted value: it exists for a human reading a diff, exactly like
`sources[].path`, and no test ever asserts against it.

### 5.3 `ValidationContext`, and how origin and role get set

```csharp
public enum ArchiveOrigin { Unknown, Official }
public enum ArchiveRole   { Unknown, Level, Paired, Localized, Global }

public sealed record ValidationContext(
    FormatProfile Profile,
    ArchiveOrigin Origin = ArchiveOrigin.Unknown,
    ArchiveRole Role = ArchiveRole.Unknown,
    string? BuildId = null)
{
    public GameVersion Game => Profile.Game;
    public Platform Platform => Profile.Platform;
}

public interface IValidatable
{
    IEnumerable<ValidationIssue> Validate(ValidationContext context);
}
```

The context carries the whole `FormatProfile`, not just a `GameVersion`, because `GameVersion` is
too coarse to scope every rule (§5.4) and because several invariants are per-platform rather than
per-game.

`Archive`, `Block`, and `Asset` implement `IValidatable`. A container's `Validate` yields its own
issues then recurses. `Validate` never throws and never mutates.

The library cannot derive `Origin` or `Role`. It reads streams; it has no filename, no sibling
directory, and no way to know whether a byte sequence came off a retail disc or out of a level
editor. So it does not try. It **owns the vocabulary and the entry point**, and the caller supplies
the value:

- **The entry point** is the context parameter. `Archive.Validate()` — no argument — builds a
  context from what the archive itself knows: `Game` comes from its own `FormatProfile`, `Origin`
  and `Role` stay `Unknown`. `Archive.Validate(context)` takes a caller-built context.
- **`Unknown` is a first-class value, not a fallback.** A rule that needs a role it does not have
  either declines via `AppliesTo` or emits the `insufficient-context` classification (§6). It never
  guesses. This is what makes the defaults safe: the *worst* case for an unlabelled archive is that
  role-dependent rules abstain.
- **`EvilHop.Corpus` supplies the labels**, because it is the only component that knows the full
  path, the manifest, and the neighbouring files. Role classification is filename convention (§8.5),
  and `Origin.Official` is true by definition of being under `artifacts/` and named by the manifest.

A library consumer with the same knowledge can pass the same labels; a consumer that doesn't, gets
the conservative behaviour. This asymmetry is the point of §6's demotion rule.

### 5.4 Attributes: rules declared next to the field they constrain

Nearly every invariant in categories 2 and 3 is a predicate over a single member with a constant
parameter. Those belong on the member, where IntelliSense shows them next to the value they
describe and where they cannot drift away from it.

```csharp
public class PackageVersion : Block
{
    [ConstantValue(2u)]
    public uint SubVersion { get; set; }

    [ConstantValue(ClientVersion.N100FPrototype, Quirks = FormatQuirks.PrototypeBuild)]
    [ConstantValue(ClientVersion.N100FRelease,   Games = [GameVersion.N100F])]
    [ConstantValue(ClientVersion.Default,        From = GameVersion.BFBB)]
    public ClientVersion ClientVersion { get; set; }

    [ConstantValue(1u)]
    public uint CompatVersion { get; set; }
}

public class PackageFlags : Block
{
    [RequiredBits(PackFlags.Default)]
    [DefinedBits]
    public PackFlags Flags { get; set; }
}

public class Package : Block
{
    [RequiredChild] public PackageVersion Version { get; set; }
    [RequiredChild] public PackageCount Counts { get; set; }
    [RequiredChild(From = GameVersion.BFBB)] public PackagePlatform? Platform { get; set; }
}
```

The base carries the scoping and the severity override; every concrete attribute inherits it.

```csharp
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Field,
                AllowMultiple = true)]
public abstract class ValidationAttribute : Attribute
{
    public GameVersion[] Games { get; init; } = [];             // empty means "every game"
    public GameVersion From { get; init; } = GameVersion.N100F;
    public GameVersion To { get; init; } = GameVersion.Ratatouille;
    public FormatQuirks Quirks { get; init; } = FormatQuirks.None;
    public Platform[] Platforms { get; init; } = [];            // empty means "every platform"
    public Severity Severity { get; init; } = Severity.Error;
}
```

#### Scoping granularity: game, quirk, platform

`GameVersion` names a *game*, one member per shipped title. It must not grow prototype members:
§5.4's `From`/`To` ranges depend on the enum being chronological and one-per-game, and a
`N100FPrototype` member would silently break every range that spans it. Build-level divergence
belongs in `FormatProfile`, which is what `FormatProfile` is already for.

So the scope axes are:

| Axis | Expresses | Attribute property |
|---|---|---|
| Game | Which title | `Games`, `From`, `To` |
| Quirk | Which kind of build within a title | `Quirks` |
| Platform | Which console | `Platforms` |

`FormatQuirks` is a `[Flags]` enum on `FormatProfile` naming the build-shaped divergences the corpus
has actually shown — `PrototypeBuild`, and in time the N100F prototype layout quirks that §11 says
should arrive as evidence rather than speculation. A rule scoped to a quirk applies only when the
profile carries it; unscoped rules apply regardless, which is why `Quirks` defaults to `None`
meaning "don't care" rather than "no quirks."

The N100F `ClientVersion` case is the worked example. Two values are legal for one game
(`0x00000001` in the prototype, `0x00040006` in release), which under game-only scoping forces the
rule down to `[AllowedValues(N100FPrototype, N100FRelease)]` — a true statement, but a weaker one
than we can make. `Quirks = FormatQuirks.PrototypeBuild` recovers the precision without inventing a
`GameVersion`.

**Until a quirk flag exists, the rule degrades to the closed set of two and the split is an
observation, not an assertion.** The `Coverage` record answers "which client versions appear in
which build" (§7.2), and a clean per-build partition is the evidence that promotes a quirk into
`FormatProfile`. That is the same promotion path as §10.2's globals, applied to format quirks: the
corpus measures the divergence, and the library declares it once it's settled.

The starter set, deliberately small:

| Attribute | Declares |
|---|---|
| `[Observed]` | Record this member in inventories. No rule. Carries `Cardinality`, `Presentation`, and an optional named projection. |
| `[ConstantValue(v)]` | Always exactly `v` in scope. Multiple instances with disjoint game scopes give the per-game table. |
| `[AllowedValues(...)]` | Member is one of a closed set. |
| `[ClosedEnum]` | Raw value maps to a defined member of the property's enum type. |
| `[DefinedBits]` | No bit outside the property's `[Flags]` enum is ever set. |
| `[RequiredBits(v)]` | Every bit in `v` is always set. |
| `[RequiredChild]` / `[OptionalChild]` | Child multiplicity. `[RequiredChild]` means exactly one, which is what "required" has always meant here; scoped, it means exactly one *in that scope* and none outside it. `[OptionalChild]` means at most one, for children genuinely present-or-absent within a single scope. |
| `[NoChildren]` | Class-level. This block is a leaf. |

`PackagePlatform` is the case that shows why scoping belongs on the multiplicity attribute rather
than in a separate optionality concept: `PLAT` is not optional from BFBB onward, it is *required*
from BFBB onward, and absent before. `[RequiredChild(From = GameVersion.BFBB)]` says exactly that,
and its C# nullability stays `PackagePlatform?` because the property must still be null when reading
N100F. Optionality of the CLR type and optionality of the format are different claims.

Scoped `[RequiredChild]` also makes the "5 children pre-Battle, otherwise 6" rule fall out of the
declarations rather than being restated as a count: the expected child set for a game *is* the set
of child properties in scope, and one further rule — no child outside that set — closes it. That is
the grouping the `Package` source already asks for, taken one step further.

Every rule attribute is also an observable declaration, so a value that has a rule never needs a
separate `[Observed]`. This is the payoff of putting them in the same place: the inventory records
`PVER.subVersion` *because* something asserts about it, and the observable ID is derived from the
declaring block's tag and the member name, so the two can never disagree about which field they mean.

**Restraint is a design constraint, not a style note.** An attribute is right when the rule is a
constant predicate over one member. Anything conditional on another block, another field, or the
archive as a whole stays imperative (§5.5). If an attribute needs a path expression, a lambda, or a
third of its own semantics documented, it is the wrong tool.

**Caching.** `ValidationCatalogue` is a static `Lazy<ValidationCatalogue>` that reflects once over
every `Block` and `Asset` subtype in the assembly, materializes each attribute into a rule object,
and compiles each member accessor into a `Func<object, object?>` delegate. `Validate` walks
precomputed per-type arrays and performs no reflection. Lazy rather than eager, so a consumer that
never validates never pays for it.

### 5.5 The rules that stay imperative

```csharp
public abstract class ValidationRule
{
    public abstract string Id { get; }              // "pver.subversion-constant"
    public abstract Severity Severity { get; }
    public abstract string Description { get; }
    public abstract EvidenceKind Evidence { get; }  // Replayable | Reducible | NonReplayable
    public virtual int RuleRevision => 1;           // hand-bumped; see §8.2
    public virtual bool AppliesTo(ValidationContext context) => true;
}

public abstract class ValidationRule<T> : ValidationRule
{
    public abstract IEnumerable<ValidationIssue> Check(T subject, ValidationContext context);
    public virtual string? Classify(T subject, ValidationContext context) => null;
}
```

Attribute-declared rules are materialized as `ValueRule`s — a `ValidationRule` whose entire content
is a predicate over one observable's value. This is what lets the tests re-run a rule against a
recorded value set without ever holding an `Archive`:

```csharp
public abstract class ValueRule : ValidationRule
{
    public sealed override EvidenceKind Evidence => EvidenceKind.Replayable;
    public abstract string ObservableId { get; }
    public abstract bool Holds(object value, ValidationContext context);
}
```

`Check` for a `ValueRule` is generated: walk the observable's selector over the subject, call
`Holds`, emit an issue per failure. One implementation, every rule. Hand-written `ValueRule`s are
allowed for the odd predicate that has no attribute, but the attribute path covers the catalogue.

Everything else is a plain `ValidationRule<T>`:

- **Cross-block reconciliations** (`PCNT.assetCount` vs `AHDR` count, `LHDR` asset IDs resolving to
  `AHDR`s, `Plus` arithmetic, `PaddingAmount` vs `Padding.Length`) — `ValidationRule<Archive>`,
  `Reducible`.
- **Cross-block correspondences** (`PFLG` bits vs the `PLAT` sibling, §11) —
  `ValidationRule<Package>`, `Replayable`, paired with a composite observable so the test can replay
  it from a tuple value set.
- **Structural rules** beyond child multiplicity (root tag sequence) — `ValidationRule<Block>`.

### 5.6 Asset-level extensibility

The requirement that new `Asset` classes bring their own rules is served without new concepts. A
concrete asset type puts attributes on its fields and registers `ValidationRule<TAsset>` instances
for anything conditional; inherited rules on `BaseAsset` / `EntityAsset` apply by subject-type
matching. The per-type claims from the invariant list — `Stackable` on `SIMP`, `NoShadow` VIL-only,
`AnimateCollision` requires `PreciseCollision`, per-type `Subtype` sets, per-type expected
`baseType` — are `ValidationRule<EntityAsset>` with an `AppliesTo` on `Asset.Type`, and their corpus
counterpart is a composite observable (`AHDR.type+ENT.entityFlags` as a `Bitmask`).

### 5.7 Asset support, and what happens to an unsupported type

Two different questions hide behind the word "support", and conflating them is how a table like this
goes wrong:

```csharp
public enum GameSupport  { Native, ModRequired, Unsupported }  // what the engine does with it
public enum CodecSupport { Modelled, Partial, Opaque }         // what EvilHop does with it
```

**`CodecSupport` is derived, not declared.** It is a read of the codec registry: `Modelled` if a
concrete codec is registered for `(type, game)`, `Partial` if only a shape codec applies, `Opaque`
if neither does. Maintaining it by hand would create a table that can silently disagree with the
code it describes.

**`GameSupport` is declared, and it is declared on the `AssetType` member itself** — not in a
side table. Enum members take attributes, so the claim sits on the one declaration every asset type
is guaranteed to have:

```csharp
public enum AssetType : uint
{
    [SupportedGames(GameVersion.BFBB, GameVersion.TSSM)]
    Simple = 0x53494D50,

    [SupportedGames(From = GameVersion.BFBB)]
    [ModRequired(GameVersion.N100F, Reason = "loads only with the debug-menu AR code")]
    Dispatcher = 0x44504154,
}
```

This is strictly better than a table, for three reasons. It is visible in IntelliSense at the point
of use, so support doesn't have to be restated in every XML doc comment and can't drift from it.
It hangs off the enum rather than off a concrete `Asset` subclass, so it works for the ~95 types
that have no class of their own yet — a table keyed on a class would cover a minority of the enum.
And it is `ValidationAttribute`-shaped like everything else in §5.4, so it inherits the scoping
axes, feeds validation directly through the same catalogue, and is fingerprinted from its
declaration (§8.2) with no version number.

Two attributes cover it: `[SupportedGames]` (`Native` in those scopes) and `[ModRequired]`, which
carries the `Reason` string that a `Warning`-severity issue quotes back to the user (§5.1). A type
with neither is `Unsupported` everywhere, which makes the common case — a type we know nothing
about — cost zero declarations.

`GameSupport` is the half with a corpus counterpart. The test runs it in both directions: every type
observed in game G must be supported in G, and every type declared supported in G must appear in G's
inventory unless waived. The second direction is the valuable one — it catches over-claiming.
`ModRequired` and `Unsupported` are claims about *unofficial* usage and are unfalsifiable from
official archives; they get no inventory counterpart, and the doc says so rather than pretending
otherwise.

**What happens when a codec is filtered out by game.** Nothing throws. The existing codec resolution
is already a ladder — concrete codec, generic shape codec, plain `GenericAsset` with the whole body
as an unparsed tail — and per-game filtering just enters it one rung lower:

1. A concrete codec registered for `(type, game)` reads the asset.
2. If a concrete codec exists but does not declare `game`, fall back to that type's **shape codec**.
   Bytes past the known prefix land in the unparsed tail; nothing is lost and the write path is
   byte-exact.
3. If the type has no known shape for that game, fall back to `GenericAsset`. The whole body is
   opaque, and round-trip is still exact.

Each step down emits a `ValidationIssue` — `Info` for step 2, `Warning` for step 3 — so the
degradation is visible rather than silent. This is the direct answer to "write anything": an
unsupported type is preserved perfectly and understood partially, which is strictly better than
either throwing or guessing.

**We never auto-select a different game's codec.** Reading an Incredibles `SIMP` with BFBB's codec
would produce plausible-looking fields that are wrong, which is worse than opaque bytes, and there
is no principled way to pick which game to borrow from.

**The user can ask for one explicitly**, at two granularities:

- Whole archive: construct the `Archive` with an explicit `FormatProfile`. This is how BFBB's
  `font2.HIP` — an N100F-format file shipped inside a BFBB build — gets read.
- One type: `FormatProfile.AssetInterpretations`, an `IReadOnlyDictionary<AssetType, GameVersion>`
  that says "read this type as that game would." Empty by default. An entry emits an `Info` issue
  recording that an interpretation was forced, so a validation report never silently reflects a
  reinterpretation.

Both are caller-supplied and neither is inferred, which keeps the "guess" out of the library and in
the hands of whoever actually knows.

---

## 6. Known violations, and why classification lives in the library

Some invariants are violated by shipped archives, permanently. The requirement is to keep the rule,
record the violation, and distinguish "expected, and we have a theory" from "no idea."

Classification belongs to the rule, in EvilHop, because a user opening a shipped localized archive
benefits from exactly the same distinction. That is the litmus test passing cleanly, and it is the
whole justification for putting rules in the library rather than the tool.

```csharp
// on ValidationRule<T>
public virtual string? Classify(T subject, ValidationContext context) => null;

public static class KnownViolations
{
    // closed vocabulary; every tag carries the theory that justifies it
    public const string LocalizationTextChecksum = "localization-text-checksum";
    public const string TruncatedName = "truncated-name";
    public const string InsufficientContext = "insufficient-context";

    public static IReadOnlyDictionary<string, KnownViolation> All { get; }
}

public sealed record KnownViolation(string Tag, string Theory, Severity DemotedSeverity);
```

Two rules govern it:

- **Classification demotes severity only when `Origin == Official`.** A checksum mismatch in a
  shipped localized `.HIP` is `Info` with tag `localization-text-checksum`. The same mismatch in an
  archive of unknown origin is an `Error`, because the likeliest explanation is that the user broke
  it. Because `Origin` defaults to `Unknown` (§5.3), demotion is opt-in and cannot happen by
  accident — which is the whole reason the field exists.
- **A rule that lacks the context to classify says so** — it emits `insufficient-context` rather than
  guessing or suppressing.

Classification is also useful on *success*: `AssetId` records which calculation rule reproduced the
stored ID (`direct`, `anm-suffix`, `mph-suffix`, `dff_destruct-suffix`), turning a boolean into a
distribution (§10.4).

**The CI test for this is a direct application of the governing rule:** the inventory records
classification tags as raw strings; the test asserts every recorded tag maps to a `KnownViolations`
entry. Deleting a tag from the library without regenerating fails the build, exactly as deleting an
`AssetType` would.

---

## 7. The inventory format

One file per game, `corpus/<game>.json`, lowercased `GameVersion` name, matching the existing
`tests/EvilHop.Tests/TestData/<game>/` convention. Per-game rather than per-build because most rules
are scoped per-game, six files stay legible in a diff, and build identity is carried on each record
rather than in the filename.

An inventory is an **aggregate, not a dump.** It records distinct values with counts and a bounded
set of witnesses — never one row per asset. Per-asset rows appear only in violation ledgers, which
are small by construction. Full-fidelity per-asset output is a separate, gitignored JSONL dump
(§8.4).

Every hash in the file is the **first 7 hex characters** of a SHA-256, git-style. The corpus is a
few thousand archives; a collision is not a realistic concern at that scale, and the readability
gain in a diff is real.

### 7.1 File shape

```json
{
  "schema": 1,
  "game": "BFBB",
  "sources": [
    { "path": "bfbb/gc/ntsc/hb01.hip", "sha256": "9f2c1a4", "bytes": 9437184,
      "build": "bfbb-gc-ntsc-release", "role": "Level" },
    { "path": "bfbb/gc/pal/hb01_DE.hip", "sha256": "41ba07c", "bytes": 9441280,
      "build": "bfbb-gc-pal-release", "role": "Localized", "language": "DE",
      "pairGroup": "bfbb/gc/pal/hb01" }
  ],
  "facets": {
    "blockFields": {
      "generator": { "revision": 3, "inputs": "14ab5e3" },
      "coverage": { "archives": 412, "sourceSetHash": "7d0e13a" },
      "observations": { }
    }
  }
}
```

`sources` is written once and referenced by every facet through `sourceSetHash`, so 400 paths are
not repeated nine times. Witnesses carry the path verbatim rather than an index — readability beats
a few kilobytes.

### 7.2 Record types

Five shapes, and the whole file is built from them.

**`ValueSet`** — an observable's distinct values. Covers invariant categories 2 and 3 entirely.

```json
"AHDR.type": {
  "kind": "enumerated", "presentation": "fourcc",
  "values": [
    { "value": 1095520845, "display": "ANIM", "count": 18422,
      "witnesses": ["bfbb/gc/ntsc/bb01.hip", "bfbb/gc/ntsc/hb01.hip"] }
  ]
},
"PFLG.flags": {
  "kind": "bitmask", "presentation": "hex", "union": 34078734, "display": "0x0208000E"
}
```

`value` is authoritative and numeric, so values sort and compare without parsing. `display` is a
mechanical rendering chosen by the observable's `Presentation`: `"ANIM"` for `Fourcc`,
`"0x00000002"` for `Hex`, absent for `Number`. Hex is the right default for anything read as a bit
pattern or a packed code — flag words, fourccs, version constants — and decimal for anything read as
a quantity.

`Summarized` observables carry `{min, max, count, distinct}` instead of `values`; `Bitmask`
observables carry a single `union`.

**`Relation`** — a reducible rule's ledger. Covers invariant categories 1 and 4.

```json
"adbg.checksum-matches-data": {
  "checked": 190233, "held": 190226,
  "known": [ { "tag": "localization-text-checksum", "count": 7,
               "witnesses": ["bfbb/gc/pal/hb01_DE.hip"] } ],
  "unclassified": [],
  "anchors": [ { "subject": "AHDR[0x8A3F1C2D]", "stored": 3735928559, "data": "base64…" } ]
}
```

`known` collapses to counts plus witnesses; `unclassified` is enumerated in full, because an
unclassified violation is a finding and its whole value is being readable.

**`Coverage`** — presence facts keyed by build and role: which type uints appear where, which layer
type uints appear where, which `(type, role)` pairs exist, and which values of a within-game-varying
field appear in which build. Feeds `[SupportedGames]`, the `.HIP`/`.HOP`/localized question, and the
quirk-promotion evidence of §5.4.

**`Reference`** — cross-archive resolution, by ring and by resolved target type (§10.2).

**`Provenance`** — the fenced non-observation section (§7.5).

### 7.3 Facets

A facet is the unit of piecemeal regeneration. Nine of them, each carrying its own generator stamp
and coverage:

| Facet | Contents |
|---|---|
| `structure` | Root tag sequence, `PACK` child sets, required-child multiplicity, leaf-childlessness. |
| `blockFields` | `ValueSet`s for every archive- and block-scoped observable. |
| `assetFields` | `ValueSet`s and bitmask unions for asset-scoped observables, including `type+alignment`, `type+baseType`, `type+subtype`. |
| `derived` | `Relation` ledgers for every reducible reconciliation, plus the `AHDR.id` classification distribution. |
| `layout` | Padding fill bytes, gap-byte uniformity, per-layer trailing alignment, data-start alignment. |
| `layers` | Per-build ordered layer-type sequences and per-game layer-type sets. |
| `references` | Reference resolution by ring and target type, and global/cohort inference evidence. |
| `links` | Event and parameter observations. Empty by construction until EvilHop parses links. |
| `verification` | Fenced provenance (§7.5). |

### 7.4 Determinism

Every facet is a pure function of its covered archives and its generator. Concretely: object keys
sorted, value lists sorted by value ascending, witnesses chosen as the lexicographically first two
paths, anchors chosen as the lexicographically first N by `(path, subject)`, two-space indent, LF
endings, trailing newline, no timestamps anywhere except `verification`. Regenerating an unchanged
facet must produce byte-identical output — this is what makes over-regeneration free of consequence,
and it is tested directly in `EvilHop.Corpus.Tests`.

### 7.5 The fence

`verification` is the one section that is not an observation, and it is named and structured so a
reader cannot mistake it for one. It records, per build: a code version, a UTC date, and pass/fail
counts for parse fidelity and round-trip, with failures grouped by a classification tag.

```json
"verification": {
  "roundTrip": {
    "codeVersion": "a2e5f19", "ranAt": "2026-08-31T00:00:00Z",
    "builds": [ { "build": "bfbb-gc-ntsc-release", "archives": 412, "identical": 410,
                  "failures": [ { "path": "…", "tag": "dpak-no-assets-padding" } ] } ]
  }
}
```

`EvilHop.Tests.Inventory` asserts exactly one thing about it: every failure tag maps to a declared
`RoundTripFailure` tag in the library. It does not assert pass counts — that would be asserting a
recorded verdict. The counts exist to be read by a human and to show up in a diff.

---

## 8. Generation

### 8.1 Map/reduce

The tool is a two-stage pipeline, and this is what makes piecemeal generation actually cheap:

- **Map** — for each archive, produce an *observation record*: the per-archive contribution to every
  facet. Expensive (opens the archive, parses assets), and **cached** on disk in a gitignored
  cache keyed by `(archiveSha256, facetInputFingerprint)`.
- **Reduce** — aggregate all records for a game into facets. Cheap, in-memory, and **always run over
  the full set**. The committed output is therefore always a pure function of all covered archives,
  never a history-dependent accumulation.

Incrementality lives entirely in the cache. Correctness lives entirely in the reduce.

### 8.2 Staleness, and how `InputFingerprint` is computed

Each facet declares what it depends on:

```csharp
public interface IFacetGenerator
{
    string Id { get; }
    int Revision { get; }                     // hand-bumped when the facet's own logic changes
    IEnumerable<string> Dependencies { get; } // catalogue keys: observable IDs, rule IDs, enum names
    string InputFingerprint();                // derived; see below
}
```

`InputFingerprint()` is the first 7 hex characters of a SHA-256 over the sorted list of `key=digest`
lines, one per dependency, where the digest is produced by `ValidationCatalogue.DigestOf(key)`:

| Dependency | Digest is built from |
|---|---|
| Observable | `id`, scope, cardinality, presentation, projection name, projection revision |
| Attribute-declared rule | `id`, observable ID, attribute type name, severity, game scope, and the attribute's constant arguments |
| Imperative rule | `id`, rule type name, severity, evidence kind, `RuleRevision` |
| Enum | sorted `name=value` pairs |

The point is that **the declarative half is fingerprinted from its declaration**, with no
hand-maintained version number: change `[ConstantValue(2u)]` to `[ConstantValue(3u)]`, and the
digest of that rule changes, and every facet that depends on it goes stale automatically. That
mechanical precision is a third argument for the attribute design in §5.4 — attributes are data, and
data can be hashed.

**Why not the assembly identity.** Deterministic builds make the MVID a stable function of the
source, but a maximally coarse one: any unrelated edit anywhere in `EvilHop` would change it and
invalidate the entire map cache on every commit. The declaration digest is exactly as reliable for
declarative rules and strictly more precise.

**What the fingerprint cannot see** is the body of an imperative `Check`. That residue is covered by
`RuleRevision` on the rule and `Revision` on the facet, both hand-bumped — which is a real
discipline cost and the reason imperative rules are the exception rather than the norm. The
obligation to bump belongs in `AGENTS.md` alongside the other repo-wide rules, so it is enforced by
review rather than by memory.

`inventory --check` prints the full `key=digest` list on a mismatch, so a stale fingerprint is
explainable rather than an opaque hash difference.

Staleness is a `Revision`/`InputFingerprint` mismatch, or a change to the covered source set. It is
**advisory**: because generation is deterministic, regenerating something that wasn't stale costs
time and produces no diff, so the tool errs toward regenerating and `--all` is always safe.

### 8.3 What regenerates, by scenario

This is the requirement stated concretely.

| Change | Map re-runs | Facets rewritten |
|---|---|---|
| New `Asset` class / codec | Asset-scoped map only | `assetFields`, `derived`, `links` |
| New build added to `artifacts/` | The new archives only | That game's facets |
| New invariant added (replayable, on an already-observed field) | **Nothing** | **Nothing** — only test-side code changes |
| Corrected definition of a replayable invariant | **Nothing** | **Nothing** |
| New invariant added (reducible) | That facet's map | `derived` (or `layout`/`structure`) |
| New observable added | That observable's scope | Its facet |
| `AssetType` enum gains a member | Nothing | Nothing — raw uints were already recorded |

The two "nothing" rows are the payoff of pushing rules to the replayable side, and the strongest
argument for §3.1 being the default. They hold because the attribute that declares the rule declares
the observable too: the field was already being recorded before anyone asserted anything about it.

### 8.4 Verbs

```
evilhop-corpus inventory  [--game <g>] [--facet <id>] [--all] [--no-cache] [--check]
evilhop-corpus verify     [--game <g>] [--round-trip] [--record]
evilhop-corpus dump       [--game <g>] --out <path>
```

- `inventory` regenerates stale facets and writes `corpus/`. `--check` regenerates everything and
  fails if the output differs from what's committed — the pre-release gate, run locally.
- `verify` runs the non-replayable checks over `artifacts/`. `--record` writes the outcome into the
  `verification` facet; without it, the run is read-only and just reports. This is the answer to
  "where does round-trip belong": a verb, with an optional durable trace.
- `dump` writes full-fidelity per-asset JSONL to a gitignored path, for ad-hoc investigation.

### 8.5 The manifest

Most of what the tool needs to know about an archive it can work out for itself. `artifacts/` is
organised by game, EvilHop is gaining format sniffing, and the roles follow universal filename
conventions:

- `<name>.HOP` is `Paired` with `<name>.HIP`.
- `<name>_XX.HIP` is `Localized`, with `XX` as the language code, and joins `<name>`'s pair group.
- Everything else defaults to `Level`.

A pair group is therefore keyed on the shared base name and is symmetric: every member sees every
other member (§10.2).

Those conventions are hardcoded in the tool, not configured, because they do not vary by game or
build. Configuring them would be inventing variability that does not exist.

`corpus/manifest.json` is hand-authored, committed, and small, and covers only the three things
convention cannot supply:

```json
{
  "schema": 1,
  "builds": [
    { "id": "bfbb-gc-ntsc-release", "directory": "bfbb/gc/ntsc",
      "globals": ["boot.HIP", "font.HIP", "mn.HIP"] }
  ],
  "cohorts": [
    { "id": "player-pl01", "archive": "PL01.HIP", "members": ["hb0?.hip", "bb0?.hip"] }
  ],
  "overrides": [
    { "path": "bfbb/gc/ntsc/font2.HIP", "game": "N100F", "role": "Global",
      "note": "N100F-format archive shipped inside a BFBB build" }
  ]
}
```

- **`globals`** is the always-loaded hypothesis, and it is **one-way**: every other archive in the
  build can see these, but these cannot see the others (§10.2). It is the only role that genuinely
  varies between games, and even between builds within a game it is nearly uniform. It is a
  hypothesis under measurement, which is exactly why it is declared rather than inferred.
- **`cohorts`** are the conditional ring (§10.2): archives loaded only by a subset of levels, and
  symmetric within the group. This section starts empty and is filled in from what `verify` reports.
- **`overrides`** pin a serializer, `FormatProfile`, game, or role for a specific archive. This is a
  real requirement, not a hypothetical: BFBB's `font2.HIP` must be read as an N100F archive.

Resolution precedence is: per-archive override, then build declaration, then the profile EvilHop
sniffs, then the directory-implied game. When sniffing gets good enough to detect a case like
`font2.HIP` on its own, the override is deleted and the inventory diff proves the change was inert.

The manifest is committed because it is the reviewable statement of what our corpus *is*, and
because `EvilHop.Tests.Inventory` needs role and build identity to reason about records. It is not
in EvilHop, because it is a claim about our artifact set, not about the format.

---

## 9. `EvilHop.Tests.Inventory`

Reads `corpus/` and `EvilHop`. Never touches `artifacts/`. An assembly fixture loads and
deserializes the six inventories once.

Six generic tests carry most of the weight, and they gain coverage automatically as rules are added:

1. **`ValueRuleTests`** — for every `ValueRule` in the catalogue (which is to say, every attribute in
   §5.4), for every game where it applies, for every value in the matching `ValueSet`: assert
   `Holds`. This one test covers invariant categories 2 and 3 in their entirety, including the
   `AHDR.type` → `AssetType` case from the requirements.
2. **`ClosedVocabularyTests`** — every recorded classification tag, round-trip failure tag, and rule
   ID maps to a live library declaration.
3. **`ObservableCoverageTests`** — every observable in the catalogue has a record in every game's
   inventory (or an explicit waiver), and every recorded observable still exists in the catalogue.
   This is the anti-rot test: without it, deleting an observable silently deletes its coverage and
   nothing fails.
4. **`AnchorTests`** — recompute every reducible rule's anchors from their inlined inputs
   (§3.2), pinning `Crc32Mpeg2`, `BKDRHash`, and `AssetId.FromName`.
5. **`SupportTests`** — observed type coverage against the `[SupportedGames]` declarations, both
   directions (§5.7).
6. **`SchemaTests`** — the inventory `schema` matches the reader's, so a format change can't be
   half-landed.

Hand-written tests fill in where the generic machinery doesn't reach: per-game layer-type sets,
reference-ring expectations, and the structural facts in `structure`.

`EvilHop.Corpus.Tests` (the tool's own project) covers the plumbing: map/reduce correctness,
byte-identical determinism, cache-hit output equals cache-miss output, `InputFingerprint` stability
and sensitivity, and the reflection guards on the observable catalogue from §4.

---

## 10. The open questions this design settles

### 10.1 Where round-trip belongs

A `verify --round-trip` verb, with `--record` writing a fenced provenance entry (§7.5). Not a CI
test — it cannot be one. The durable, *observational* half of round-tripping is the **failure
taxonomy**: which quirk caused each mismatch, tagged against a library-declared vocabulary. That
part does get a CI test, via `ClosedVocabularyTests`, and it is the part that actually teaches us
about the format.

### 10.2 Which archives are always loaded

Resolution is not a global lookup. Each archive has its own **visible set**, and a reference is
resolved against that set and nothing else. The tool assigns every reference to one of five
**rings**, in order:

1. `self` — same archive
2. `paired` — another member of the archive's pair group
3. `cohort` — a manifest-declared group loaded together but not universally: `PL01.HIP` is plausibly
   loaded only by levels that use `PL01` as the player, and a reference resolving there is neither
   global nor local
4. `global` — an archive in the manifest's global-candidate set
5. `unresolved`

**The rings differ in direction, and getting that wrong would silently inflate resolution rates.**

- `paired`, `localized`, and `cohort` are **symmetric**. A pair group is a set whose members are all
  mutually visible: `BB01.HIP`, `BB01.HOP`, and `BB01_US.HIP` each see the other two. The group is
  keyed on the shared base name, which is why a source record carries `pairGroup` rather than a
  one-way `pairedWith` pointer.
- `global` is **one-way**. Every archive in BFBB can see `boot.HIP`'s assets; `boot.HIP` cannot see
  theirs. A global's own visible set is itself plus the other globals — nothing else.

So a reference *out of* a global into a level archive does not resolve, and that is a finding rather
than a defect in the tool: it is evidence that the archive is not actually a global. This makes the
directionality do real work. The `cohort` ring exists for the same reason — without it, a
conditionally-loaded archive forces a false choice between calling it global and over-claiming, or
leaving its referents unresolved and losing the signal. Cohorts are discovered by looking at *which*
builds a candidate resolves references for — resolves for every level, it's global; resolves for a
recognisable subset, it's a cohort — and then written back into the manifest.

`references` records, per build and per reference kind (`surfaceId`, `modelId`, `animListId`, link
destination, param widget, check ID):

```json
"surfaceId": {
  "byRing": { "self": 40122, "paired": 3, "cohort": 0, "global": 88, "unresolved": 12 },
  "targetTypes": [ { "value": 1398362694, "display": "SURF", "count": 40213 } ],
  "unresolvedTargets": ["0x1234abcd"]
}
```

**Recording the resolved target type is the point of the facet, not a detail of it.** The claim
worth testing about `surfaceId` is not that it resolves — it is that it always resolves to a `SURF`.
Because the distinct type space is bounded (§3.1), `targetTypes` is an ordinary `ValueSet` and
"`surfaceId` targets `SURF`" becomes an `[AllowedValues]`-style replayable rule that a generic test
re-runs. Every reference kind gets the same treatment, which converts a whole category of
cross-archive claims from "unverifiable" into the cheapest bucket we have.

It additionally records **inference evidence**: for each candidate global or cohort, how many builds'
unresolved references it would resolve if admitted, and — because the relation is one-way — how many
of the candidate's *own* references escape its visible set, which is the number that falsifies a bad
global. That evidence is the deliverable. The hypothesis
lives in the manifest, gets refined by looking at the numbers, and when the numbers stop moving it
**graduates into EvilHop** as a per-game declaration — at which point the tests start asserting it.
This is the general shape of the whole subsystem: *the corpus is where a hypothesis is measured; the
library is where a settled fact lives.*

### 10.3 Links and events

The `links` facet records, per game: observed `(sourceEvent, ownerTypeRaw)` and
`(destinationEvent, targetTypeRaw)` pairs with counts, and per-event parameter byte patterns. All raw
`short`s and raw type uints — the inventory is agnostic of an `Event` enum that doesn't exist yet.

It is **empty until EvilHop parses links**, because no codec currently does (`LinkCount` as an
override is the "unparsed" signal). The tool will not reimplement link parsing to fill it — that
fails the litmus test. This is a stated dependency, and the facet exists now so that landing link
parsing lights it up without a format change.

When an `Event` enum and per-game event table arrive, the tests follow the `AHDR.type` pattern
exactly: every observed event ID maps to a known event for that game; every observed
`(event, ownerType)` pair is declared legal.

### 10.4 `AHDR.id`, as a distribution rather than a boolean

`AssetId` is **reducible**, not replayable: its per-item input is small, but with roughly 777,000
assets in BFBB alone and names that are near-unique by construction, the distinct input space is the
corpus itself. So `derived` records a distribution rather than values:

```json
"ahdr.id-matches-name": {
  "checked": 777430, "held": 761882,
  "byRule": { "direct": 742119, "anm-suffix": 14206, "mph-suffix": 3218, "dff_destruct-suffix": 2339 },
  "known": [ { "tag": "truncated-name", "count": 15102, "witnesses": ["…"] } ],
  "unclassified": [ { "subject": "AHDR[0x1A2B3C4D]", "witness": "…" } ],
  "anchors": [ { "name": "trailer.dff", "type": 1297040460, "stored": 2882343476 } ]
}
```

The counts tell us how well `AssetId.FromName`'s transform table covers reality; the `unclassified`
rows are the residue that needs a theory; the anchors keep `BKDRHash` and the transform table under
test on CI without shipping a name table. A new transform rule moves rows from `unclassified` into a
named bucket, and — because the map cache is keyed on the facet's fingerprint — costs one re-map of
`derived` and nothing else.

---

## 11. Rules kept, reframed, and dropped

Not everything in the invariant list earns its keep.

**Dropped outright:**

- **"Container blocks hold no data."** This is a statement about the model — those classes have no
  data fields — not about data. A rule for it would be testing C#. The *real* half of the concern,
  duplicate `ATOC`/`LTOC`/`AHDR` siblings past the first-match `SetChild`, is kept as
  `[RequiredChild]` multiplicity.
- **"Assets divided evenly between layers of the same type."** Unfalsifiable as stated. The
  observation that replaces it is the per-build ordered layer sequence in `layers`, which lets the
  actual pattern be rediscovered from data. The TSSM+ "3 BSP + 1 JSPINFO" claim survives as an
  observed sequence, not a rule.
- **"ATOC list is sorted by id."** The wiki claims it; `AssetSession.ReplayAtocOrder` contradicts it.
  No rule until the data says which is right. `derived` records per-build sortedness and deviation
  distance, and settles it.
- **`PCRT.createdDateString` cross-checked against `CreatedDate`.** Already declined in-library for a
  good reason (unknown build-machine timezone). Only the format rule survives, and only as a
  projected shape.
- **"`PCRT`/`PMOD` store a valid Unix timestamp."** Every `uint` is a valid Unix timestamp, so the
  rule as stated is a tautology. Narrowing it to a plausibility window doesn't rescue it: an archive
  authored by a modder in 2026, or by a tool that writes a zero date, is entirely legitimate and
  flagging it — even at `Info` — is noise in a report that should contain only signal. The dates are
  still *observed*, as a `Summarized` min/max, because that costs nothing and answers "when were
  these built"; no rule reads it.

**Kept, and stronger than first stated:**

- **`PFLG` flag validity.** There is more structure here than "some combination we've seen", and it
  is worth three real rules rather than a passive value set:
  - `[RequiredBits(PackFlags.Default)]` — `Unknown2`, `Unknown3`, `Unknown4`, and `Unknown6` are
    always set.
  - `[DefinedBits]` — no bit outside `PackFlags` is ever set.
  - **Correspondence with the `PLAT` sibling**: the platform, region, and language bits must agree
    with `PLAT.PlatformId`, `PLAT.Region`, and `PLAT.Language`, and the `Platform` bit must agree
    with whether a `PLAT` child exists at all. This is cross-block, so it is an imperative
    `ValidationRule<Package>` — but its observation is the composite `ValueSet`
    `PFLG.flags+PLAT.platformId+PLAT.region+PLAT.language`, whose distinct cardinality is a handful
    of rows per game. That makes it **replayable** despite being imperative, and it is the clearest
    example of why the taxonomy keys on cardinality rather than on rule shape.

**Reframed:**

- **Per-type `ADBG.Alignment` default table.** The library explicitly doesn't model it. Recorded as
  the composite observable `AHDR.type+ADBG.alignment`, which reconstructs the table empirically. It
  graduates into the library once the observation is stable.
- **N100F prototype quirks** (0x00 fill, plus-to-2048, `Alignment == 0`, `ClientVersion`
  `0x00000001`). Recorded in `layout` and `Coverage` per build, so `FormatQuirks` gains the right
  flags *because* the corpus showed the divergence, rather than gaining speculative flags now.
  Until then the affected rules widen to the observed set rather than being dropped — a prototype
  is a different *build*, not a different *game*, and `GameVersion` stays one member per title
  (§5.4).

Everything else in the invariant list maps onto §3's taxonomy without special handling.

---

## 12. Build order

1. **`EvilHop.Validation` core** — `Severity`, `ValidationIssue`, `IssueSite`, `BlockPath`,
   `ValidationContext`, `IValidatable`, `ValidationRule<T>`, `Archive.Validate()`. No rules yet.
2. **Attributes, `ValidationCatalogue`, and `ValueRule`** — the attribute family from §5.4, the
   cached reflection pass, and the first dozen constant/closed-set declarations converted from the
   existing `Validation TODO:` comments. This is the load-bearing piece; everything else is
   downstream.
3. **Tool skeleton** — manifest, map/reduce, cache, `InputFingerprint`, deterministic JSON writer,
   `blockFields` facet. The `AGENTS.md` rule about bumping `Revision` and `RuleRevision` lands with
   it, not after it.
4. **`EvilHop.Tests.Inventory`** — fixture plus `ValueRuleTests` and `ObservableCoverageTests`. The
   net is live from here on, and every subsequent attribute joins it for free.
5. **`assetFields`, `structure`, `layers`** — the remaining observation facets.
6. **`derived` + anchors** — reducible ledgers, `KnownViolations`, `AnchorTests`.
7. **`verify` verb + `verification` facet** — round-trip, with its failure taxonomy.
8. **`references`** — rings, target types, and global/cohort inference.
9. **`[SupportedGames]` on `AssetType`** and its two-directional test; codec per-game filtering and
   the fallback ladder.
10. **`links`** — after EvilHop parses links.

Steps 1–4 are the minimum that makes the whole thing real; each later step adds a facet without
disturbing the ones before it, which is the same property that makes regeneration piecemeal.

---

## 13. Risks

- **The reducible bucket is a recorded judgement.** Anchors bound the damage but don't eliminate it,
  and `AHDR.id` — a rule we care about a great deal — sits in it because of scale. Mitigation is to
  keep the bucket small, to keep anchor selection deterministic so the vectors don't churn, and to
  prefer replayable framings wherever cardinality permits.
- **Imperative logic isn't fingerprintable.** `RuleRevision` and facet `Revision` are hand-bumped,
  which is discipline rather than mechanism. Three things blunt it: attributes cover most rules and
  are fingerprinted automatically, `inventory --check` before release catches a missed bump, and
  determinism makes over-regenerating harmless. The obligation is written down in `AGENTS.md` so it
  is a review item rather than folklore.
- **Attributes can grow past their competence.** The failure mode is an attribute with a lambda, a
  path expression, or three interacting optional properties. §5.4's restraint clause is the guard,
  and the honest test is whether the attribute reads as a fact about the field.
- **Projections lose information.** A projected observable can't answer a question nobody thought to
  ask when the projection was written. The `dump` verb is the escape hatch: full fidelity is always
  one command away from `artifacts/`.
- **The observable catalogue could rot silently.** `ObservableCoverageTests` exists specifically
  because this failure mode is invisible by default.

## 14. What would falsify this design

- If most rules turn out to be reducible rather than replayable, §3.1 isn't the default and the
  test suite is mostly hand-written — at which point the observable/`ValueRule` machinery is
  overhead rather than leverage.
- If the attribute set can't cover the invariant catalogue without growing a configuration language,
  the declarative half is a false economy and rules should just be objects.
- If per-game files turn out to change on nearly every build addition anyway, per-build files would
  give better diffs and the split in §7 is wrong.
- If the map cache turns out not to be the bottleneck — if reduce over six games is itself slow —
  then incrementality has to move into the reduce, and §8.1's clean "reduce is always full" guarantee
  has to be renegotiated.

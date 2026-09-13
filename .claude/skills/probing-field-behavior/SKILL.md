---
name: probing-field-behavior
description: Use this skill to find out what an asset field actually does in the running game - whether a field that is constant across every official archive is vestigial or merely unused, what an unknown flag bit means, or whether a field belongs on the logical or physical surface. Builds a workshop level containing copies of one asset that differ only in the field under test, boots it in Dolphin, and records the result under probes/. Covers the controls that make a null result trustworthy.
---

# Probing a field's behaviour

The corpus records which values a field takes in shipped content. It cannot say what the engine does
with them. This skill closes that gap: build a level where copies of one asset differ only in the
field under test, walk through it, and watch.

**The load-bearing rule: a field being constant across every official archive is a reason to probe
it, never a reason to demote it.** `SurfaceAsset.WallJumpScaleXZ` is 1 in all 2,239 BFBB surfaces and
is fully implemented - zero removes the push, negative reverses it. "Constant" is a fact about level
design, not about the engine.

## Before you build anything, read the corpus

Scan the asset type's real field values first. It costs one command and routinely changes the probe.

Write a throwaway script under `dump/` (see `reading-corpus-inventory`, or iterate `artifacts/`
directly with `Archive.Load` + `OpenAssets`) that tallies each field's distinct values with an
exemplar archive per value. You are looking for:

- **Unmodelled flag bits.** A raw number where an enum name was expected - `PhysFlags=32` when
  `SurfacePhysicsFlags` stops at `1 << 4`. Cross-reference the *names* of the assets that set it;
  Heavy Iron named things literally, and `WALLJUMP_SURFACE` gives the bit away.
- **The one archive that differs.** A field constant everywhere except a single asset is the best
  positive control you will get, because the shipped value is known to work.
- **Gates.** If the field under test correlates with a flag, that flag probably enables it.

### Gates are the failure mode that matters

A field that only applies when some other flag is set will look completely dead if you probe it with
that flag clear, and the result is indistinguishable from a real null. This is the single most likely
way to reach a confidently wrong conclusion. When the corpus suggests a gate, set it on every
variant, and add one further variant that is identical to a working one *except* the gate - that
isolates the flag from the fields it controls.

## The workshop

`ZZ01` is a purpose-built empty level in the extracted disc. `scripts/build-probe.cs` assembles it.

| Piece | Where it comes from |
|---|---|
| Level template (`CAM`/`MINF`/`ENV`/`LKIT`/`PLYR`) | `IndustrialPark-EditorFiles/.../Utility/blank.HIP` + `blank.HOP` |
| Floor | `disco_floor_A_3m` from `bc01.HOP`, tiled |
| Skydome | `skydome_jf` from `jf02.HOP` - without it the framebuffer is never cleared and you get hall-of-mirrors |
| Rig models | Whatever the shipped rig for this mechanic uses |

Facts worth not rediscovering:

- **A fresh scene ID works.** `ZZ01` needs no registration; the engine resolves it to
  `files/{slot}/zz01.HIP`. The level folder's case follows the disc's own convention - lowercase in
  BFBB, uppercase in TSSM - while the file inside is lowercase in both.
- **The boot ini is CRLF, and its name varies** (`sb.ini` in BFBB, `SB04.ini` in TSSM). Never touch
  it with `sed -i`, which rewrites every line ending. The script patches it in place and keeps a
  `.orig`.
- **Collision scales with `Scale`.** A tile at `Scale 4` collides across its full scaled footprint,
  so large arenas cost few assets.
- **Models live in the HOP, game objects in the HIP.** Both are written in the same run.
- **Borrow textures with models.** A borrowed model whose `RW3` stayed behind renders untextured.
- **Borrow from the target game's own disc.** Models and textures are not interchangeable between
  games even where name and asset ID match exactly - `disco_floor_A_3m` is 816 bytes in BFBB and 736
  in TSSM, because TSSM re-exported it under a different RenderWare version. Loading BFBB's copies
  under TSSM crashes the game.
- **IndustrialPark archives omit `DPAK`'s padding-amount field.** Read them with
  `profile with { StreamDataHasPaddingField = false }` or every asset offset lands four bytes late
  and anything crossing the boundary silently degrades to empty. Official archives carry the field.

### Copy a shipped rig rather than inventing one

Find the assets the game itself uses for the mechanic and copy their field values wholesale, changing
only what is under test. For wall jumping that meant locating the entity whose `SurfaceId` pointed at
`WALLJUMP_SURFACE`, then reusing its model, scale and collision settings. This removes a whole class
of "did I configure the carrier wrong?" ambiguity, and it is faster than guessing at rotation
conventions.

## Designing the variant row

Lay variants in a single row along +X from the spawn, evenly spaced, numbered west to east, and tint
each one differently. Spatial order is how the finding gets reported - the tester says "1. normal,
2. nothing, 3. stronger" without needing to match names to positions.

- **Variant 1 is the control**: the value every shipped asset uses.
- **Pick values to be unmissable, not realistic.** Zero, several times the default, an absurd value,
  and a negative. You are trying to make an effect visible at a glance.
- **Always include a positive control**: a strip varying a *different* field that is known to work,
  through the same rig. If the control shows no difference either, the rig is broken and a null
  result on the field under test means nothing. Prefer a value some shipped asset actually uses.

## Running it

```
dotnet run .claude/skills/probing-field-behavior/scripts/build-probe.cs -- bfbb
Dolphin -e dump/bfbb-gc/sys/main.dol
```

The script writes both archives into the disc and points its boot ini at the workshop, keeping the
original alongside as `.orig`. Pass a game key (`bfbb`, `tssm`) to target a different disc; the
`setups` table at the top of the script is the only thing that differs between them.

Dolphin boots an extracted disc directly from `sys/main.dol`, which needs `sys/` (`main.dol`,
`boot.bin`, `bi2.bin`, `apploader.img`) beside `files/`. Restart Dolphin after each rebuild; it will
not pick up changed files otherwise. Set `INDUSTRIALPARK_EDITORFILES` if your checkout of the
template files is not where the script's default expects.

Verify both archives parse back before booting - zero diagnostics, under either padding convention.
A malformed archive wastes a boot.

Then hand over: list the variants with their numbers, tints and settings, say what to do at each
one, and say explicitly what the positive control would have to show for the run to count. Ask for
freeform prose back; "1. no difference, 2. does x" is enough.

## Recording the result

Write `probes/{game}-{asset}-{topic}.md` and add it to the table in `probes/README.md`. Record the
values tested, the rig, what happened at each variant, what changed in the library, and - separately
- what the probe did *not* establish.

Then update the library, because the probe is evidence and the XML documentation is the claim:

- A field that does something keeps its place on the logical surface. Say what it does, and name any
  flag it depends on.
- A field that does nothing under every configuration tried moves to the physical surface, documented
  as unknown rather than as useless. It can be promoted later if someone finds what it drives.
- A newly identified flag bit becomes a named enum member.

Do not write "always 1 in every sample checked" as though it settles anything. Either the probe
established what the field does, or it did not.

## Other games

Only a null result needs every game - a field that works in one game demonstrably works.

Adding a game means one entry in the script's `setups` table, not new probe code: the template
directory, the disc, its boot ini, the level slot, which shipped archives to borrow the floor,
skydome and rig models from, and a reference asset to copy shared values off. The BFBB and TSSM
workshops are byte-for-byte the same probe.

What actually differs between games:

- **The template's asset set.** N100F wants `ENV`/`PLYR`/`CAM` and a blank `BSP`; BFBB wants
  `CAM`/`MINF`/`ENV`/`LKIT`/`PLYR`; TSSM drops `MINF` and adds a `DYNA` glow prop.
  `IndustrialPark-EditorFiles` ships a `blank.HIP` for each.
- **Spawn height.** Templates do not agree, so anchor the floor to the player's own position rather
  than to `y=0`.
- **Asset layout.** From TSSM on, every `SURF` carries ~140 bytes of `ExtendedData` whose layout is
  unknown. Copy it from a shipped asset of the same type rather than authoring an empty one.
- **Sniffing cannot identify the game.** TSSM, The Incredibles and Ratatouille share a format
  version, so a sniffed profile will name the wrong one. Always construct the profile from the game
  you meant.

GameCube is the easiest target throughout. Keep generated archives so a finding can be re-checked on
PS2 and Xbox once EvilHop can convert between platforms.

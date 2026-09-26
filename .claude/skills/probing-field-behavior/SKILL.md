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

Write a throwaway script under `dump/` (see the `reading-corpus-inventory` skill to query `corpus/`,
or inspect local game archives if available with `Archive.Load` + `OpenAssets`) that tallies each
field's distinct values with an exemplar archive per value. You are looking for:

- **Unmodelled flag bits.** A raw number where an enum name was expected - a `PhysFlags` value
  carrying a bit `SurfaceAsset.PhysicsBehavior` doesn't define. Cross-reference the *names* of the assets that set it;
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
| Level template (`CAM`/`MINF`/`ENV`/`LKIT`/`PLYR`) | Blank template (from external IndustrialPark editor files) or a stripped shipped level |
| Floor | Flat tile from the target game's disc (`gc_rfcl0003` for N100F, `disco_floor_A_3m` for BFBB/TSSM, `electric_pad_on_e` for Incredibles, `electric_plate_aone` for ROTU, `platform_4x4xp5` for Ratatouille) |
| Skydome | Shipped skydome (`skydome_jf` for BFBB/TSSM, `skydome_red_NJ` for Incredibles, `skydome_bm` for ROTU, `skydome_fp` for Ratatouille); N100F uses an enclosed box of scaled floor tiles |
| Rig models | Shipped rig assets for the mechanic under test |

Every tile is placed by the width it should span and the point its top surface should centre on,
not by a raw scale and origin, so one probe layout serves every game. Tiles are centred on 12-unit
steps from the spawn so the player never starts on a seam. Every player starts facing +X, down the
variant row (yaw $\pi/2$; yaw turns +Z toward +X).

### Game-specific constraints

`scripts/build-probe.cs` abstracts most setup differences, but specific engines impose constraints on rig design and testing:

- **N100F (Scooby)**:
  - Levels are single-archive (`.HIP` only, no `.HOP`).
  - N100F `SURF` is unmodelled in EvilHop; surface variants cannot be probed (the pad/ramp row is plain floor).
  - Boot INI is `sd2.ini`.
- **The Incredibles**:
  - Boot INI is `in.ini`.
- **Ratatouille**:
  - **Dolphin must have a controller connected on port 4**, or any scene crashes on its first frame when the follow camera polls input.
  - Boot INI is `rats.ini`.
- **ROTU**:
  - Every scene is co-op (`ZZ01` maps Mr. Incredible as P1 and Frozone as AI follow).
  - The camera rides a fixed `CameraCurve` trailing side-on from +Z, looking toward -Z. Because +X runs right-to-left on screen, **Variant 1 is the rightmost**. Lay variants along +X and keep rigs visible from this vantage (avoid placing objects on the +Z side of the row).
  - The navigation mesh bounds the playable area; jumping past its edges kills the player. Keep all rigs within the floor bounds.
  - Boot INI is `in2.ini`.

### Rules of thumb and engine gotchas

- **`EntityAsset.Angle` is `(yaw, roll, pitch)`**: Yaw turns +Z toward +X ($\pi/2$ points down +X).
- **ATOC order**: Archives must have their ATOC written in ascending unsigned asset ID order (required by N100F's loader, which binary-searches the ATOC on load).
- **`SimpleObject` updates (TSSM onward)**: A SIMP with nothing that needs a per-frame update is statically batched and its `Update` never runs. To force per-frame updates (e.g. for facing or animation testing) on a non-collidable SIMP, set `CollisionFlags.LedgeGrab`. `AnimateCollision` without `PreciseCollision` on a model without animation will crash.
- **Borrowing models and textures**:
  - Always borrow from the target game's own disc (models and textures are not binary-compatible across games even when names and IDs match).
  - Always borrow a model's corresponding texture (`.RW3`) alongside it.
  - Only borrow models actively referenced by shipped entities; unreferenced leftover models in shipped archives can crash on load.
- **IndustrialPark archives**: Community archives authored by IndustrialPark omit `DPAK`'s padding-amount field. Read them with `FormatProfile with { StreamDataHasPaddingField = false }`.
- **Format sniffing**: Format sniffing cannot differentiate TSSM, The Incredibles, and Ratatouille (they share format versions). Explicitly construct profiles via `GameVersion`.
- **Scene ID resolution**: `ZZ01` needs no engine registration; the engine resolves it to `files/{slot}/zz01.HIP`. Folder casing matches disc convention (lowercase in BFBB, uppercase in TSSM); archive file names are lowercase.
- **Boot INIs**: Boot INIs must preserve CRLF line endings.
- **Collision scales with `Scale`**: A tile scaled up collides across its full scaled footprint.
- **Archive split**: Models and textures live in the HOP; game entities live in the HIP. Borrowed assets retain their source layer type.
- **Debugging with Dolphin**: Dolphin's PC, LR, and callstack can be resolved against decomp symbols or by disassembling the GameCube DOL. Instruction breakpoints in Dolphin confirm whether code reading a field executes; use "Boot to Pause" for load-time logic. Use the `validating-asset-docs` skill if available for more information on decomp workflows.

### Copy a shipped rig rather than inventing one

Find the assets the game itself uses for the mechanic and copy their field values wholesale, changing only what is under test (e.g. reuse the model, scale, and collision flags of an existing surface carrier). This eliminates carrier configuration errors and avoids guessing at rotation or physics conventions.

## Designing the variant row

Lay variants in a single row along +X from the spawn, evenly spaced, numbered west to east, and tint each one differently. Spatial order is how findings are reported - the tester can simply report "1: normal, 2: nothing, 3: stronger" without matching names to positions.

- **Variant 1 is the control**: the value every shipped asset uses.
- **Pick values to be unmissable, not realistic.** Zero, several times the default, an absurd value, and a negative. Make the effect visible at a glance.
- **Always include a positive control**: a variant or strip varying a *different* field known to work, through the same rig. If the positive control shows no difference either, the rig is broken and a null result on the field under test is meaningless. Prefer a value that shipped assets actually use.

## Running it

```bash
dotnet run .claude/skills/probing-field-behavior/scripts/build-probe.cs -- bfbb
Dolphin -e dump/bfbb-gc/sys/main.dol
```

The script writes both archives into the extracted disc directory and points its boot INI at the workshop (preserving the original as `.orig`). Pass a game key (`n100f`, `bfbb`, `tssm`, `incredibles`, `rotu`, `rat`) to target a different disc. Everything above `// THE PROBE` in `build-probe.cs` is shared scaffolding; customize the rig below that marker.

Dolphin boots an extracted disc directly from `sys/main.dol`, which requires `sys/` (`main.dol`, `boot.bin`, `bi2.bin`, `apploader.img`) beside `files/`. **Restart Dolphin after each rebuild**; it caches files and will not pick up changes otherwise. Set `INDUSTRIALPARK_EDITORFILES` if your checkout of template files is not at the default path.

Verify both archives parse back cleanly before booting (`Archive.Load` + `OpenAssets` with zero diagnostics). A malformed archive wastes a boot.

When handing over to the user:
1. List the variants with their numbers, tints, and parameter settings.
2. Provide explicit instructions on what to test at each variant.
3. State clearly what the positive control should demonstrate for the run to be valid.
4. Ask for freeform observations back.

## Recording the result

Record probe findings in a local working note, such as `probes/{game}-{asset}-{topic}.md`. Record:
- The field values tested.
- The rig used.
- Observations at each variant.
- What changed in the library as a result.
- What the probe did *not* establish (unresolved questions or edge cases).

Then update the library:
- **Active fields**: Keep on the logical surface. Document behavior and any prerequisite gate flags in XML docs.
- **Dead/unresponsive fields**: Move to the physical surface (`asset.Physical`), documented as unknown rather than useless.
- **Newly identified flag bits**: Promote to named enum members.

Do not write "always 1 in every sample checked" in documentation as though it settles engine behavior. Either the probe established what the field does, or it remains unproven.

## Adding a new game target

Only a null result requires verifying across every game - an effect confirmed in one game demonstrably works.

To support an additional game, add an entry to the `setups` dictionary in `scripts/build-probe.cs`:
- The template archive source (IndustrialPark directory or shipped donor level and assets to retain).
- The extracted disc directory and boot INI name/settings.
- The workshop slot (`ZZ01`) and directory.
- Model, texture, and skydome borrow targets (with tile dimensions).
- A reference asset to copy shared values from (such as a shipped SURF's friction and slide angles).

# AGENTS.md

Instructions for coding agents working in this repository. This file is
self-contained: everything needed to bootstrap is here or in the repo.

## Project summary

EvilHop is a C# .NET library for reading, writing, and manipulating **HIP
archive files** — the binary asset container format used by several games
developed by Heavy Iron Studios:

- Scooby-Doo! Night of 100 Frights
- SpongeBob SquarePants: Battle for Bikini Bottom
- The SpongeBob SquarePants Movie
- The Incredibles
- The Incredibles: Rise of the Underminer
- Ratatouille (Jan 18, 2006 prototype)

There are six known format versions with a linear (non-branching) history; all
six are supported targets for reading and writing.

## Core architecture

**Two-layer model.** The low-level *Block* layer models the file as a tree of
strongly-typed, inert data holders, one per discrete file segment, each with an
ordered `Children` collection whose in-memory order matches on-disk order. The
high-level *Asset* layer is projected over that tree: each asset combines one
`AssetHeader` block (id, type, absolute offset, size, padding, flags) with a
contiguous, non-overlapping slice of the single `DataStream` block's byte pool.

**Versioned serializers.** All binary reading and writing lives in `Serializer`
classes, one per format version, forming an inheritance chain in which each
version overrides only the delta from the previous one. Methods that are stable
across versions are sealed; virtual methods carry documented contracts.

**Write-time materialization.** During editing the Asset layer is the sole
source of truth, so block offsets, sizes, padding, and `DataStream` contents are
knowingly stale. An internal, per-write `WriteCoordinator` created by the
serializer rebuilds the `DataStream` in layer order, recomputes offsets, sizes,
and padding, and serializes dirty assets — all in one pass, invisible to
consumers. There is no change-notification or eventing.

**Modal Layer Access.** Asset Mode is the default: block fields managed by the
Asset layer (offsets, sizes, padding, ids) are read-only. Block Editing Mode is
entered explicitly; it detaches assets and unlocks every block field. Mode
switches are idempotent.

## Non-negotiable design principles

Violating one of these requires explicit justification in the pull request.

1. **Dumb data holders.** Blocks and strongly-typed asset subclasses do not
   self-serialize, self-validate, walk the tree, or compute derived values.
   Serialization lives only in `Serializer` classes. Two narrow exceptions: the
   `Children` collection enforces tree invariants (no cycles, single parent),
   and the base `Asset` class provides infrastructure only (source version,
   dirty/valid state, typed field get/set).
2. **Write anything, validate optionally.** Never block a write by default.
   Missing or invalid data is written as-is. Validation is advisory through a
   validation mode of `None`, `Warn` (default), or `Strict`.
3. **Two-layer architecture with modal access.** Both layers stay public;
   mutability is governed by the current mode, never by two mutable views of the
   same data.
4. **Write-time materialization.** The Asset layer is authoritative while
   editing; all synchronization is deferred to write time.
5. **Fully in-memory.** The whole file is read up front and the stream closed
   immediately. No lazy loading, no stream retention. Bounded by an assumed
   maximum of 100 MB per archive.
6. **Best-effort round-trip fidelity.** Output is byte-*similar*, not
   byte-identical: unmodified assets keep their original bytes via dirty
   tracking, padding is preserved, and field ordering is stable.
7. **Externalized, versioned serialization.** Version-specific behavior belongs
   in the serializer chain as a delta over the previous version, covered by
   per-version round-trip tests.

## Build, test, and format

Solution: `EvilHop.slnx`. Library: `src/EvilHop`. Tests:
`tests/EvilHop.Tests` (xUnit). Target framework: `net10.0`.

| Task | Command |
| --- | --- |
| Build | `dotnet build EvilHop.slnx --configuration Release` |
| Test (with coverage) | `dotnet test EvilHop.slnx --configuration Release --collect:"XPlat Code Coverage"` |
| Format check | `dotnet format EvilHop.slnx --verify-no-changes` |

Builds are deterministic and treat warnings as errors, with .NET analyzers and
code-style enforcement on, so any analyzer or style warning fails the build.
Formatting and style come from the repository `.editorconfig`; run
`dotnet format EvilHop.slnx` to fix violations rather than hand-editing. All
three commands must pass before opening a pull request. Coverage is reported in
CI but never enforced.

## Packaging and versioning

Versions are derived from Git tags by MinVer using the tag prefix `v`, so tag
`v1.2.3` yields version `1.2.3`; with no reachable tag the version is a
`0.0.0-alpha.0.N` prerelease. Do not hand-edit version numbers in project files.

- Push to `main` publishes the MinVer-derived prerelease to NuGet.org.
- A stable release publishes to NuGet.org via a manual workflow trigger or by
  pushing a `v*` tag.

## Repository layout

The codebase is being rebuilt from scratch on the `rewrite` branch, so the tree
is intentionally sparse and will grow quickly:

```
EvilHop.slnx              solution
Directory.Build.props     shared MSBuild properties (TFM, analyzers, packaging)
src/EvilHop/              the library
tests/EvilHop.Tests/      xUnit tests
.github/workflows/        CI and release pipelines
```

Keep this file current: when directories, commands, or build specifics change,
update the relevant section in the same pull request.

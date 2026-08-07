# EvilHop

EvilHop is a C# .NET library for reading, writing, and manipulating **HIP archive files**, the binary asset container format used in several games developed by Heavy Iron Studios.

## Supported Games

- Scooby-Doo! Night of 100 Frights
- SpongeBob SquarePants: Battle for Bikini Bottom
- The SpongeBob SquarePants Movie
- The Incredibles
- The Incredibles: Rise of the Underminer
- Ratatouille (Jan 18, 2006 prototype)

## Features

- **Two-layer API**: Work with logical Assets (textures, models, etc.) or drop down to the Block layer for low-level format access.
- **Cross-version support**: Read and write across all six known HIP format versions, including cross-version conversion.
- **Best-effort round-trip fidelity**: Unmodified assets are preserved byte-for-byte; modified assets are re-serialized.
- **Permissive by design**: Write anything - validation is advisory-only and never blocks writes by default.

## Contributing

The library targets `net10.0`; the solution is `EvilHop.slnx`, with the library
under `src/EvilHop` and tests under `tests/EvilHop.Tests`.

### Build

```sh
dotnet build
```

Builds are deterministic and treat warnings as errors, with .NET analyzers and
code-style enforcement enabled.

### Test

```sh
dotnet test
```

### Format check

```sh
dotnet format --verify-no-changes
```

Style is enforced by the repository `.editorconfig`; run `dotnet format` to fix
violations.

## Packaging and publishing

Package versions are derived from Git tags by [MinVer](https://github.com/adamralph/minver)
using the tag prefix `v`, so tag `v1.2.3` produces version `1.2.3`. With no
reachable tag, MinVer produces a `0.0.0-alpha.0.N` prerelease version.

- **Prereleases** publish to [NuGet.org](https://www.nuget.org/packages/EvilHop)
  automatically on every push to `main`.
- **Stable releases** publish to NuGet.org by running the `Release` workflow
  manually (`workflow_dispatch`) or by pushing a `v*` tag.

## License

EvilHop is licensed under the [MIT License](LICENSE).

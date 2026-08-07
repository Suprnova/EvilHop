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

## License

EvilHop is licensed under the [MIT License](LICENSE).
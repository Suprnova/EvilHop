# EvilHop

EvilHop is a C# .NET library for reading, writing, and manipulating **HIP archive files**, the binary asset container format used in several games developed by Heavy Iron Studios.

> **Status: alpha.** The block layer is complete and round-trips every archive tested byte-for-byte. The asset layer is under construction - the object model exists, but the session and codecs that populate it do not yet. Breaking changes are expected, and there is no NuGet package yet.

## Supported Games

- Scooby-Doo! Night of 100 Frights
- SpongeBob SquarePants: Battle for Bikini Bottom
- The SpongeBob SquarePants Movie
- The Incredibles
- The Incredibles: Rise of the Underminer
- Ratatouille (January 18, 2006 prototype)

## Two Layers

EvilHop exposes the same bytes two ways, and treats both audiences as first-class:

- **Block layer** - the HIP container exactly as it exists on disk: a tree of tagged blocks with their fields. Complete and permissive. Anything expressible in a HIP file is expressible here, including states the game would reject.
- **Asset layer** - the game objects those blocks describe, as typed objects with names, positions, and links to one another. Offsets, sizes, and checksums are maintained for you rather than being yours to get wrong.

The two are mutually exclusive: while an asset session is open, it owns the blocks that describe assets.

## Example

Reading an archive and listing what it contains, at the block layer:

```csharp
using EvilHop;
using EvilHop.Blocks;
using EvilHop.Serialization;

using var file = File.OpenRead("hb01.HIP");
var archive = Archive.Load(file, new BFBBSerializer());

var dictionary = archive.Roots.OfType<Dictionary>().Single();
foreach (var header in dictionary.AssetTable.Headers)
    Console.WriteLine($"{header.Debug.Name} ({header.Type}) - {header.Size} bytes");

using var output = File.Create("hb01.out.HIP");
archive.Save(output);
```

## Design Principles

- **Write anything, validate optionally.** Nothing stops you from producing a state the game would reject, so long as it can be serialized. `Validate()` reports problems; it never blocks a write.
- **Round-trip fidelity.** Reading an archive and writing it back unmodified reproduces the original bytes, down to padding and fill. Every serializer is held to this against real archives.
- **Unknown is preserved, not discarded.** Bytes the library does not yet understand are carried through untouched rather than dropped, so partial understanding never costs data.
- **Tolerant reading.** A file that violates an expectation is still a file. Malformed input degrades to a lower-fidelity representation with a diagnostic rather than throwing.

## Planned

- Asset sessions and per-type codecs - the asset layer's read/write path.
- Cross-version conversion, upgrading and downgrading archives between the supported games.
- Native field definitions for embedded payload formats (RenderWare streams, Bink video, audio), which today import and export as whole files.

## Building

Requires the .NET 10 SDK.

```
dotnet build
dotnet test
```

The test suite is hermetic and needs no game files.

## License

EvilHop is licensed under the [MIT License](LICENSE).

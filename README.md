# EvilHop

EvilHop is a C# .NET library for reading, writing, and manipulating **HIP archive files**, the binary asset container format used in several games developed by Heavy Iron Studios.

## Status

EvilHop is alpha software. Breaking changes are expected, and there is no NuGet package yet.

The block layer is complete: every serializer round-trips every archive tested byte-for-byte, including unknown fields and padding. The asset layer opens an archive through a session, parsing its blocks into typed `Asset`s and rebuilding them unconditionally on commit.

Asset codec support falls into three states, per type:

- **Typed** - the asset's fields are modeled and read/written individually. Most gameplay-relevant types are here.
- **Payload** - the asset wraps an embedded file (a RenderWare model, a Bink video, audio) that is imported and exported as a whole blob rather than parsed field-by-field. This is the intended shape for these types, not a gap: `BinkVideo`, `BSP`, `JSP`, `Model`, `RawImage`, `Sound`, `StreamingSound`, `StreamingTexture`, `Texture`.
- **Untyped** - no codec exists yet. The asset reads through its shape's generic prefix if available and remains byte-preserved. Not yet supported: `Dynamic`.

A handful of Typed assets are only partially modeled, marked with a `// TODO: Partial implementation` comment at their definition:

- `AnimationAsset` - ROTU and Ratatouille's revised SKB layout isn't modeled.
- `AnimationTableAsset` - N100F's revised States layout isn't modeled.
- `Button` - N100F's shorter, differently laid out format isn't modeled.
- `CameraAsset` - N100F's shorter, differently laid out format isn't modeled.
- `CutsceneAsset` - chunked media data is unmodeled and preserved as unparsed bytes.
- `CutsceneTableAsset` - trailing TimeChunk-offset, visibility, and break tables are unmodeled.
- `NPCAsset` - N100F Prototype-only fields aren't parsed.
- `OneLinerAsset` - a trailing 67-byte trailer isn't modeled.
- `ParticleSystemAsset` - particle commands are undecoded and stored as raw bytes.
- `SoundInfoAsset` - non-GameCube platforms aren't implemented.
- `SurfaceAsset` - N100F's smaller SURF layout isn't modeled.

## Supported Games

- Scooby-Doo! Night of 100 Frights
- SpongeBob SquarePants: Battle for Bikini Bottom
- The SpongeBob SquarePants Movie
- The Incredibles
- The Incredibles: Rise of the Underminer
- Ratatouille (January 18, 2006 prototype)

## Example

EvilHop exposes a HIP archive's bytes two ways. The block layer is the container exactly as it sits on disk - a tree of tagged blocks - and is complete and permissive. The asset layer sits on top of it: opening a session parses those blocks into typed `Asset`s with names, positions, and links, maintaining offsets, sizes, and checksums for you rather than leaving them for you to get wrong. The two are mutually exclusive - while a session is open, it owns the blocks that describe assets.

Reading an archive, listing its pickups, and doubling the Scooby Snack count of every snack gate:

```csharp
using EvilHop;
using EvilHop.Assets;
using EvilHop.Serialization;

using var file = File.OpenRead("hb01.HIP");
var archive = Archive.Load(file, new BFBBSerializer());

using var session = archive.OpenAssets();

var pickups = session.Layers.SelectMany(layer => layer.Assets).OfType<PickupAsset>();

foreach (var pickup in pickups)
{
    Console.WriteLine($"{pickup.Name} ({pickup.Kind}) at {pickup.Position} - worth {pickup.PickupValue}");

    if (pickup.Kind == PickupAsset.PickupKind.SnackGate)
        pickup.PickupValue *= 2;
}

session.Commit();

using var output = File.Create("hb01.out.HIP");
archive.Save(output);
```

## Documentation

The architecture is documented in living documents that are updated in the same change that moves
the code they describe:

- [`docs/overview.md`](docs/overview.md) - the whole library, one level deep.
- [`docs/architecture.md`](docs/architecture.md) - the design decisions behind how it is built.
- [`docs/glossary.md`](docs/glossary.md) - every block and the asset/serialization jargon.

## Building

Requires the .NET 10 SDK.

```
dotnet build
dotnet test
```

## License

EvilHop's code is licensed under the [MIT License](LICENSE). A small number of third-party test
fixtures are licensed separately under more restrictive, project-only terms - see
[`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md) for details.

# Changelog

Each version lists what it adds and, under **Breaking**, every public member it removes or changes.
`src/EvilHop/PublicAPI.Shipped.txt` holds the full public API as released.

## 0.4.0

The first version published on NuGet.

### Added

- **Explicit game and platform.** `Archive.Load(stream, game)` and `Archive.Load(stream, game, platform)`,
  `Serializer.Create(game)` and `Serializer.Create(game, platform)`, and `Archive.OpenAssets(Platform)` to supply a
  platform the serializer doesn't state. `Archive.Create(serializer)` builds an empty TSSM archive stamped for its
  platform. `Serializer.Sniff(stream, game)` takes the game from the caller, and a `SniffResult` says where its
  platform came from (`PlatformSource`) and whether the game was fixed (`GameFixed`).
- **Read-only views.** `Archive.ReadAssets()` returns an `AssetView`: the assets parsed as a session parses them,
  with nothing taken from the archive and nothing committed. It gives each asset's bytes (`GetBytes`), each layer's
  image (`GetLayerImage`), readers over either in the platform's byte order, and the layer an asset was read in
  (`LayerOf`).
- **An asset index.** `AssetView.Index` finds assets by ID, by name and by type, and lists the asset IDs each asset
  names (`ReferencesFrom`) and every asset naming an ID (`ReferencesTo`), with the member path of each.
- **Layer layouts.** `AssetView.GetLayout(layer)` lists a layer image's assets and the gaps between them, and
  `Locate(offset)` says what sits at an offset.
- **Support states.** `Asset.Support` says whether an asset is typed, a payload or untyped, including one that
  degraded; `AssetSupport.Of` answers for an asset type or a dynamic kind before any read.
- Typed assets: Button, Duplicator, Navigation Mesh, Portal, Scene Settings, Slide Property, Spline, Villain
  Properties, Volume, Wireframe, Zip Line, Dynamic and its talk box kind, and the PS2 and Xbox sound info headers.
  N100F's camera and button fields, Surface's fields outside Battle for Bikini Bottom, more pipe info fields, and
  GameCube sound modelling.

### Fixed

- Committing an archive that ends with a `HIPB` block.
- The order `ATOC` entries are written in.
- N100F's animation table state.

### Breaking

- `Archive.Load(Stream)`, which sniffed the game and platform, is removed: state them with
  `Archive.Load(stream, game, platform)`.
- The per-game `DefaultProfile` statics (`BFBBSerializer`, `IncrediblesSerializer`, `N100FSerializer`,
  `ROTUSerializer`, `RatatouilleSerializer`, `TSSMSerializer`) are removed: use `Serializer.Create`.
- `FormatProfile.Platform` is a `Platform?`, null when not stated, which changes `FormatProfile`'s constructor and
  `Deconstruct`. `Archive.OpenAssets()` throws `InvalidOperationException` when the serializer states no platform.
- `Serializer.Sniff` no longer falls back to GameCube: an unknown platform is null. `SniffResult`'s constructor and
  `Deconstruct` gain `PlatformSource` and `GameFixed`.
- A second `Archive.OpenAssets()` while a session is open throws `InvalidOperationException` instead of a
  `NullReferenceException`.
- `DynaAsset` and `Physical.IDynaAsset` are replaced by `DynamicAsset` and `Physical.IDynamicAsset`, whose `Kind`
  is a `DynamicKind` in place of `DynaType`.
- `Physical.IEntityAsset`'s members move to `Physical.IEntity`, which it now extends with `Physical.IBaseAsset`.
- `BaseAsset.BaseFlags` moves to `Physical.IBaseAsset.BaseFlags`.
- `SimpleObjectAsset.AnimationSpeed`, `Collision` and `InitialAnimationState` move to
  `Physical.ISimpleObjectAsset`, whose `SimpleFlags` is now a `SimpleObjectAsset.Behavior`; `FaceCamera`,
  `FacePlayer`, `HasCollision` and `Upright` read its bits.
- `PlatformAsset.Flags` moves to `Physical.IPlatformAsset.PlatformFlags`; `Shakes` and `Solid` read its bits.
- `SurfaceAsset.ExtendedData` and `IsEnabled`, and `Physical.ISurfaceAsset.IsEnabled`, are replaced by the modelled
  fields of `Physical.ISurfaceAsset`.
- `SoundInfoAsset.DspHeader` is now `SoundInfoAsset.SoundHeader`, and `SoundInfoAsset.SoundBanks` holds
  `SoundInfoAsset.SoundBank`s instead of byte arrays.
- `CameraKind.Shoulder` is 5; 1 is `CameraKind.FollowWithOffset`.
- `PlatformMotion.Springboard.LockingBehavior.LockView` is now `HighBounceCamera`.
- `PlatformMotion.TeeterTotter.Unknown` is replaced by its modelled fields.

## 0.3.0 and earlier

Released on GitHub only: [0.3.0](https://github.com/Suprnova/EvilHop/releases/tag/v0.3.0),
[0.2.0](https://github.com/Suprnova/EvilHop/releases/tag/v0.2.0),
[0.1.1](https://github.com/Suprnova/EvilHop/releases/tag/v0.1.1),
[0.1.0](https://github.com/Suprnova/EvilHop/releases/tag/v0.1.0).

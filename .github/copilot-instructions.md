# EvilHop Project Instructions

**Project:** EvilHop
**Language:** C# (.NET 10.0)
**Domain:** Binary Serialization/Deserialization Library for Heavy Iron Studios' HIP file format.

## 1. Project Overview

EvilHop is a library for reading, writing, and modifying HIP archive files used in games like *Scooby-Doo! Night of 100 Frights*, *Battle for Bikini Bottom*, *The Incredibles*, and others.

The library is designed to handle multiple file format versions (V1, V2, V3, V4) and game-specific quirks through strict architectural separation of **Data** (Blocks) and **Logic** (Serializers/Validators).

* EvilHop (class library): `/src/EvilHop/EvilHop.csproj`
* EvilHop.Tests (XUnit testing project): `/tests/EvilHop.Tests/EvilHop.Tests.csproj`
* EvilHop.Audit (console application for auditing game files against specs, ignore in most cases): `/src/EvilHop.Audit/EvilHop.Audit.csproj`

There is no CI/CD implementation in this project, nor is there a CONTRIBUTING.md file or guidelines for Pull Requests. **Do not** suggest adding these unless prompted to. `dotnet format` is typically ran before commits, however.

## 2. Core Architectural Principles

### A. The "Dumb Block" Principle

* **Blocks are Data Containers Only:** Classes in `EvilHop.Blocks` must **never** contain serialization logic, file IO code, or version-specific validation rules.
* **No Calculated Sizes:** Blocks do not know their own file size. Size calculation is the responsibility of the `Serializer`.
* **No File Offsets:** Blocks do not store their absolute file offset (to avoid state synchronization issues). Offsets are calculated contextually during traversal.
* **Properties:** Use auto-properties. For Enums, use the Enum type directly as the property type (backed by `uint` via casting in the Serializer).
* **Children:** Do not use generic lists for known children. Use the `GetRequiredChild<T>()` and `GetChild<T>()` helpers.

### B. The Strategy Pattern (Versioning)

* **Inheritance Model:** Serialization logic uses an inheritance chain: `V1Serializer` → `V2Serializer` → `V3Serializer`.
* **Delta Updates:** A derived serializer (e.g., V2) only overrides methods for blocks that changed in that version.
* **Registry System:** Serializers use a `Dictionary` registry (mapped by Block ID or Type) to dispatch Read/Write/Init/Size logic. This replaces massive switch statements.

### C. Two-Tiered User Experience

1.  **Power User (Bare Metal):** Interacts directly with `Block` classes and `Serializers`. Can create invalid states. Manages their own cross-referencing.
2.  **Normal User (Facade):** Interacts with a high-level Abstraction Layer (`HipArchive`, `HipAsset`). This layer handles synchronization, offsets, and easy asset manipulation.

## 3. Namespace & Class Design

### `EvilHop.Blocks`

* **Base Class:** All blocks inherit from `Block`.
* **Children Access:**
	* Use `protected T GetRequiredChild<T>()` for blocks guaranteed to exist (throws `InvalidDataException` if missing).
	* Use `protected T? GetChild<T>()` for optional/version-dependent blocks.
* **Constructors:** Blocks typically have parameterless constructors. Complex initialization is handled by the Serializer's `New<TBlock>` factory method.
* **Padding:** Do **not** store padding arrays. Store `PaddingAmount` (int) and skip/write zeros in the Serializer.

### `EvilHop.Serialization`

* **`IFormatSerializer`:** The interface defining `Read<T>`, `Write`, `New<T>`, and `GetBlockSize`.
* **`FileFormatFactory`:** Sniffs the file header to return the correct concrete Serializer (e.g., `ScoobyV1Serializer`).

### `EvilHop.Serialization.Serializers`

* **Registry Pattern:** Constructors must register handlers.
	* `RegisterBlock<T>(string id, Func<T> initFunc, Func<BinaryReader, uint, T>? readFunc, Action<BinaryWriter, T>? writeFunc) where T : Block`
* **Logic Location:** Read/Write logic lives in partial classes (e.g., `PackageBlock.Serialization.cs`) to keep logic co-located with Block definitions physically, but logically inside the Serializer class.
* **Structure:**
	* `V1Serializer` (Base implementation).
	* `V2Serializer` (Inherits V1, overrides changed blocks).
	* `V3Serializer` / `V4Serializer` (Sequential inheritance).
	* **Game Scopes:** Concrete classes (e.g., `BattleSerializer`) inherit from the appropriate abstract version (`V2Serializer`).

### `EvilHop.Serialization.Validation`

* **Separation:** Validation logic lives here, NOT in Blocks.
* **Pattern:** Validators follow the same inheritance chain as Serializers.
* **Output:** Validators return `IEnumerable<ValidationIssue>`. Do not throw exceptions for validation failures; return issues with `Severity`.
	* `ValidationSeverity.None`: Not an issue that will affect the game. Occasionally used for undocumented values when we know it's likely to be undocumented.
	* `ValidationSeverity.Warning`: Known to be invalid given correct understanding of the specifications. Might affect the game in some way, but not likely to prevent it from loading the file.
	* `ValidationSeverity.Error`: Known to be invalid, and most likely will cause the game to be unable to load the file.
* **Usage:** Exposed via `block.IsValid(serializer, out errors)` extension method.

## 4. Implementation Details & Patterns

### Reading Blocks

When implementing `Read` logic in a Serializer:
1.  Read properties using `EvilHop.Primitives` extensions (e.g., `reader.ReadEvilString()` for strings, `reader.ReadEvilInt()` for uints).
2.  Do **not** validate values here, and do **not** attempt to catch exceptions raised by `EvilHop.Primitives` (`EvilHop.Primitives` throws exceptions for EOF or improperly terminated Strings).
3.  For Enums: Read as `uint`, cast to Enum. Allow undefined values (forward compatibility).

### Writing Blocks

1.  Cast Enums back to `uint`.
2.  Use the `Serializer.GetBlockSize(block)` method if a length field needs to be written.

### Planned Functionality, Open to Change

#### The Abstraction Layer (Planned)

* **`HipArchive`:** Wraps the Root Block and the Serializer.
* **`HipAsset`:** A Facade wrapping an `AssetHeader` and `StreamData`.
* **`AssetBlockView`:** A `readonly struct` used to iterate assets without memory allocation.
	* Holds references to `AssetHeader`, `StreamData`, and the calculated `AbsoluteStreamOffset`.
	* Returns `ReadOnlySpan<byte>` for data access (calculated on the fly).
	* **Do not** store a list of Assets on the Root block.

#### Handling Assets (The "Disjointed Data" Problem)

* Asset Metadata is in `AssetTable` (Header).
* Asset Binary is in `AssetStream` (Data).
* **Resolution:** The `Root` block (HIPA) exposes an `IEnumerable<AssetBlockView>`. It calculates the offset of the `AssetStream` block stateless-ly by summing sibling sizes, then yields views that link the Header to the Stream.

## 5. Coding Standards (.NET 10)

* **Namespaces:** Use file-scoped namespaces (`namespace EvilHop.Blocks;`).
* **Properties:** Use `field` keyword where applicable or auto-properties.
* **Nullability:** Enable Nullable Reference Types (`<Nullable>enable</Nullable>`).
* **Collections:** Use `Dictionary` for lookups, `IEnumerable` for lazy iteration. Avoid `List<T>` in public APIs unless ownership is transferred.

## 6. Common Pitfalls to Avoid

* **Do not** add `Validate()` methods to Block classes.
* **Do not** add `Write()` methods to Block classes.
* **Do not** store `FileOffset` properties on Block classes.
* **Do not** throw exceptions for invalid Enum values; report them via the Validator.
* **Do not** hardcode Block IDs in switch statements; use the Serializer Registry.
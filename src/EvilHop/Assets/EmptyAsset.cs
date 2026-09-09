namespace EvilHop.Assets;

/// <summary>
/// An <see cref="Asset"/> whose <see cref="Blocks.AssetHeader"/> declares a zero-byte payload.
/// </summary>
/// <remarks>
/// Not a parse failure - <see cref="AssetSession"/> never attempts to read one, so it never appears
/// in <see cref="AssetSession.Diagnostics"/> or <see cref="AssetSession.ChangedAssets"/>.
/// <see cref="Asset.Type"/> still reports whatever type the header names.
/// </remarks>
public sealed class EmptyAsset : Asset;

using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="Archive"/>'s assets, read without taking them from the archive. Reading one parses
/// the assets as an <see cref="AssetSession"/> does, but leaves the block tree attached and unlocked
/// and never commits, and it keeps every asset's bytes as they were read.
/// </summary>
/// <remarks>
/// <para>
/// A view is a snapshot. Its <see cref="Layer"/>s and <see cref="Asset"/>s are the same data holders
/// a session gives, but nothing writes them back: editing one changes only this view's object, and
/// <see cref="GetBytes"/> and <see cref="GetLayerImage"/> keep answering with what was read.
/// Committing a session on the same archive later doesn't change a view either.
/// </para>
/// <para>
/// The bytes are those the archive's blocks described when the view was read. For an archive loaded
/// and not edited through the block layer, that is the file as it is on disk.
/// </para>
/// </remarks>
public sealed class AssetView
{
    private readonly byte[] _data;
    private readonly Dictionary<Asset, Range> _assetRanges;
    private readonly Range[] _layerRanges;

    private AssetView(FormatProfile profile, AssetRead read)
    {
        Profile = profile;
        Layers = read.Layers;
        Diagnostics = read.Diagnostics;
        _data = [.. read.Data];
        _assetRanges = new(ReferenceEqualityComparer.Instance);
        foreach (var (asset, placement) in read.PlacementOf)
            _assetRanges[asset] = RangeOf(read, placement);
        _layerRanges = [.. read.Layers.Select(layer => LayerRangeOf(read, layer))];
    }

    /// <summary>
    /// The profile the assets were read with: the archive's game and the resolved platform, which
    /// decides <see cref="FormatProfile.Endianness"/>.
    /// </summary>
    public FormatProfile Profile { get; }

    /// <summary>The archive's <see cref="Layer"/>s, in the order they are listed on disk.</summary>
    public IReadOnlyList<Layer> Layers { get; }

    /// <summary>
    /// Problems encountered while reading. An asset that fails to parse degrades to its untyped form
    /// and is reported here rather than throwing.
    /// </summary>
    public IReadOnlyList<AssetDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Lookups and references over <see cref="Layers"/>, built the first time this is read.
    /// </summary>
    public AssetIndex Index => field ??= new(Layers);

    /// <summary>
    /// Reads <paramref name="archive"/>'s assets under its serializer's profile, with
    /// <paramref name="supplied"/> filling in a platform the profile doesn't state.
    /// </summary>
    internal static AssetView Read(Archive archive, Platform? supplied)
    {
        var profile = AssetReader.ResolveProfile(archive, supplied);
        return new(profile, AssetReader.Read(archive, profile));
    }

    /// <summary>
    /// The bytes of <paramref name="asset"/> as they were authored, in its platform's byte order.
    /// </summary>
    /// <param name="asset">An asset of this view.</param>
    /// <returns>
    /// The asset's bytes; empty for a zero-size asset, and for one whose bytes lie outside the
    /// archive's data, which <see cref="Diagnostics"/> reports.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="asset"/> wasn't read by this view.</exception>
    public ReadOnlyMemory<byte> GetBytes(Asset asset) => _data.AsMemory(AssetRangeOf(asset));

    /// <summary>
    /// The image of the layer at <paramref name="layer"/>: the bytes from its first asset's start to
    /// its last asset's end, with the gaps between them, exactly as on disk. An offset into the image
    /// is an offset into the heap block the game loads the layer into.
    /// </summary>
    /// <param name="layer">The layer's index in <see cref="Layers"/>.</param>
    /// <returns>The layer's image; empty when the layer has no asset with bytes in the archive's data.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="layer"/> isn't an index into <see cref="Layers"/>.</exception>
    public ReadOnlyMemory<byte> GetLayerImage(int layer) => _data.AsMemory(LayerRangeOf(layer));

    /// <summary>
    /// Opens a reader over <see cref="GetBytes"/>, in <see cref="Profile"/>'s byte order.
    /// </summary>
    /// <param name="asset">An asset of this view.</param>
    /// <returns>A reader positioned at the asset's first byte. Disposing it releases nothing the view needs.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="asset"/> wasn't read by this view.</exception>
    public EndianReader OpenReader(Asset asset) => ReaderOver(AssetRangeOf(asset));

    /// <summary>
    /// Opens a reader over <see cref="GetLayerImage"/>, in <see cref="Profile"/>'s byte order.
    /// </summary>
    /// <param name="layer">The layer's index in <see cref="Layers"/>.</param>
    /// <returns>A reader positioned at the image's first byte. Disposing it releases nothing the view needs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="layer"/> isn't an index into <see cref="Layers"/>.</exception>
    public EndianReader OpenLayerReader(int layer) => ReaderOver(LayerRangeOf(layer));

    private Range AssetRangeOf(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return _assetRanges.TryGetValue(asset, out var range)
            ? range
            : throw new ArgumentException($"The asset wasn't read by this {nameof(AssetView)}.", nameof(asset));
    }

    private Range LayerRangeOf(int layer)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(layer);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(layer, _layerRanges.Length);
        return _layerRanges[layer];
    }

    private EndianReader ReaderOver(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(_data.Length);
        return new EndianReader(new MemoryStream(_data, offset, length, writable: false), Profile.Endianness);
    }

    private static Range LayerRangeOf(AssetRead read, Layer layer)
    {
        var present = layer.Assets
            .Select(asset => RangeOf(read, read.PlacementOf[asset]))
            .Where(range => range.End.Value > range.Start.Value)
            .ToList();

        return present.Count == 0
            ? default
            : present.Min(range => range.Start.Value)..present.Max(range => range.End.Value);
    }

    private static Range RangeOf(AssetRead read, AssetPlacement placement) =>
        AssetReader.TryGetRange(read.DataStart, read.Data.Length, placement.Offset, placement.Size, out var range)
            ? range
            : default;
}

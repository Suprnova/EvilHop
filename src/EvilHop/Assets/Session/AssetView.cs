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
    private readonly Dictionary<Asset, (Range Range, AssetType Type)> _assets;
    private readonly Dictionary<Asset, FieldMap> _fieldMaps = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Asset, int> _layerOf = new(ReferenceEqualityComparer.Instance);
    private readonly Range[] _layerRanges;
    private readonly AssetEntry[][] _layerAssets;
    private readonly LayerLayout?[] _layouts;

    private AssetView(FormatProfile profile, AssetRead read)
    {
        Profile = profile;
        Layers = read.Layers;
        Diagnostics = read.Diagnostics;
        _data = [.. read.Data];
        _assets = new(ReferenceEqualityComparer.Instance);
        foreach (var (asset, placement) in read.PlacementOf)
            _assets[asset] = (RangeOf(read, placement), placement.Type);
        _layerRanges = [.. read.Layers.Select(layer => LayerRangeOf(read, layer))];
        for (int layer = 0; layer < read.Layers.Count; layer++)
            foreach (var asset in read.Layers[layer].Assets)
                _layerOf[asset] = layer;
        _layerAssets = [.. read.Layers.Select((layer, index) => AssetEntriesOf(read, layer, _layerRanges[index]))];
        _layouts = new LayerLayout?[read.Layers.Count];
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

    /// <summary>
    /// The layout of the layer at <paramref name="layer"/> as read: its assets and the gaps between
    /// them, which says what sits at any offset of <see cref="GetLayerImage"/>.
    /// </summary>
    /// <param name="layer">The layer's index in <see cref="Layers"/>.</param>
    /// <returns>The layer's layout, built the first time it is asked for.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="layer"/> isn't an index into <see cref="Layers"/>.</exception>
    public LayerLayout GetLayout(int layer)
    {
        var image = LayerRangeOf(layer);
        return _layouts[layer] ??= new(layer, _layerAssets[layer], _data.AsSpan(image));
    }

    /// <summary>
    /// The fields of <paramref name="asset"/>'s bytes as its codec read them: which member each byte
    /// was read into, under <see cref="Profile"/>.
    /// </summary>
    /// <param name="asset">An asset of this view.</param>
    /// <returns>
    /// The asset's field map, covering <see cref="GetBytes"/> exactly, built the first time it is
    /// asked for. Editing the asset afterwards doesn't change it.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="asset"/> wasn't read by this view.</exception>
    public FieldMap Describe(Asset asset)
    {
        var (range, type) = Read(asset);
        if (!_fieldMaps.TryGetValue(asset, out var map))
            _fieldMaps[asset] = map = AssetReader.Describe(_data, range, type, Profile);
        return map;
    }

    /// <summary>
    /// The index in <see cref="Layers"/> of the layer <paramref name="asset"/> was read in.
    /// </summary>
    /// <param name="asset">An asset of this view.</param>
    /// <returns>The layer's index, as read: moving the asset to another layer afterwards doesn't change it.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="asset"/> wasn't read by this view.</exception>
    public int LayerOf(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return _layerOf.TryGetValue(asset, out int layer)
            ? layer
            : throw new ArgumentException($"The asset wasn't read by this {nameof(AssetView)}.", nameof(asset));
    }

    private Range AssetRangeOf(Asset asset) => Read(asset).Range;

    private (Range Range, AssetType Type) Read(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return _assets.TryGetValue(asset, out var read)
            ? read
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

    private static AssetEntry[] AssetEntriesOf(AssetRead read, Layer layer, Range image) =>
    [
        .. layer.Assets
            .Select(asset => (Asset: asset, Placement: read.PlacementOf[asset], Range: RangeOf(read, read.PlacementOf[asset])))
            .Where(entry => entry.Range.End.Value > entry.Range.Start.Value)
            .Select(entry => new AssetEntry(
                (uint)(entry.Range.Start.Value - image.Start.Value),
                entry.Placement.Size,
                entry.Asset,
                entry.Placement.Alignment))
    ];

    private static Range RangeOf(AssetRead read, AssetPlacement placement) =>
        AssetReader.TryGetRange(read.DataStart, read.Data.Length, placement.Offset, placement.Size, out var range)
            ? range
            : default;
}

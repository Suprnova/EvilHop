namespace EvilHop.Assets;

/// <summary>
/// One stretch of a layer's image: an asset's bytes or a gap between two assets.
/// </summary>
/// <param name="Offset">Where the stretch starts, from the image's first byte.</param>
/// <param name="Size">The stretch's length in bytes.</param>
public abstract record LayoutEntry(uint Offset, uint Size)
{
    /// <summary>The offset just past the stretch's last byte.</summary>
    public uint End => Offset + Size;
}

/// <summary>
/// An asset's bytes in a layer's image.
/// </summary>
/// <param name="Offset">Where the asset starts, from the image's first byte.</param>
/// <param name="Size">The asset's length in bytes.</param>
/// <param name="Asset">The asset.</param>
/// <param name="Alignment">The alignment the asset's <c>ADBG</c> stores, as stored.</param>
public sealed record AssetEntry(uint Offset, uint Size, Asset Asset, int Alignment) : LayoutEntry(Offset, Size);

/// <summary>
/// The bytes between two consecutive assets of a layer's image.
/// </summary>
/// <param name="Offset">Where the gap starts, from the image's first byte.</param>
/// <param name="Size">The gap's length in bytes.</param>
/// <param name="Fill">The byte every byte of the gap is, or <see langword="null"/> when they differ.</param>
public sealed record GapEntry(uint Offset, uint Size, byte? Fill) : LayoutEntry(Offset, Size);

/// <summary>
/// What sits at one offset of a layer's image.
/// </summary>
/// <param name="Entry">The asset or gap holding the offset.</param>
/// <param name="OffsetInEntry">How far into <paramref name="Entry"/> the offset is.</param>
/// <param name="Previous">The nearest asset before <paramref name="Entry"/>, if any.</param>
/// <param name="Next">The nearest asset after <paramref name="Entry"/>, if any.</param>
public readonly record struct LayoutLocation(LayoutEntry Entry, uint OffsetInEntry, AssetEntry? Previous, AssetEntry? Next)
{
    /// <summary>The asset holding the offset, or <see langword="null"/> in a gap.</summary>
    public Asset? Asset => (Entry as AssetEntry)?.Asset;

    /// <summary>
    /// Whether the offset is in an asset's first four bytes, which hold a base asset's own ID: a write
    /// there replaces it.
    /// </summary>
    public bool IsFirstWord => Entry is AssetEntry && OffsetInEntry < 4;
}

/// <summary>
/// The layout of one layer's image as read: its assets in offset order, with the gaps between them.
/// </summary>
/// <remarks>
/// The entries cover the image exactly, from its first asset's start to its last asset's end. An
/// asset with no bytes in the image (zero-size, or declaring bytes outside the archive's data) has no
/// entry. Should two assets overlap, both are entries, no gap sits between them, and
/// <see cref="Locate"/> gives the later-starting one.
/// </remarks>
public sealed class LayerLayout
{
    internal LayerLayout(int layer, IEnumerable<AssetEntry> assets, ReadOnlySpan<byte> image)
    {
        Layer = layer;
        Length = (uint)image.Length;

        var entries = new List<LayoutEntry>();
        uint end = 0;
        foreach (var asset in assets.OrderBy(asset => asset.Offset))
        {
            if (asset.Offset > end)
                entries.Add(new GapEntry(end, asset.Offset - end, FillOf(image[(int)end..(int)asset.Offset])));
            entries.Add(asset);
            end = Math.Max(end, asset.End);
        }
        Entries = entries;
    }

    /// <summary>The index of the layer in <see cref="AssetView.Layers"/>.</summary>
    public int Layer { get; }

    /// <summary>The image's length in bytes, the length of <see cref="AssetView.GetLayerImage"/>.</summary>
    public uint Length { get; }

    /// <summary>The image's assets and gaps, in offset order.</summary>
    public IReadOnlyList<LayoutEntry> Entries { get; }

    /// <summary>
    /// What sits at <paramref name="offset"/> of the image.
    /// </summary>
    /// <param name="offset">An offset from the image's first byte, which is the offset in the heap block the game loads the layer into.</param>
    /// <returns>The location, or <see langword="null"/> when <paramref name="offset"/> is past the image.</returns>
    public LayoutLocation? Locate(uint offset)
    {
        if (offset >= Length) return null;

        int index = LastStartingAtOrBefore(offset);
        while (Entries[index].End <= offset) index--;

        return new LayoutLocation(
            Entries[index],
            offset - Entries[index].Offset,
            NearestAsset(index, -1),
            NearestAsset(index, 1));
    }

    private AssetEntry? NearestAsset(int from, int step)
    {
        for (int index = from + step; index >= 0 && index < Entries.Count; index += step)
            if (Entries[index] is AssetEntry asset) return asset;
        return null;
    }

    private int LastStartingAtOrBefore(uint offset)
    {
        int low = 0, high = Entries.Count - 1;
        while (low < high)
        {
            int middle = (low + high + 1) / 2;
            if (Entries[middle].Offset <= offset) low = middle;
            else high = middle - 1;
        }
        return low;
    }

    private static byte? FillOf(ReadOnlySpan<byte> gap) =>
        gap.IndexOfAnyExcept(gap[0]) < 0 ? gap[0] : null;
}

using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Assets.Serialization;

/// <summary>
/// Copies the fields an <see cref="Asset"/> sources from its <see cref="AssetHeader"/> and
/// <see cref="AssetDebug"/> blocks rather than from its own slice of <see cref="StreamData.Data"/>.
/// </summary>
/// <remarks>
/// Every codec's read path starts here, generic or concrete, so this population is written once
/// rather than repeated per type.
/// </remarks>
internal static class AssetFields
{
    /// <summary>
    /// Populates <paramref name="asset"/>'s header-sourced fields from <paramref name="header"/>
    /// and <paramref name="debug"/>.
    /// </summary>
    /// <param name="asset">The <see cref="Asset"/> to populate.</param>
    /// <param name="header">The <see cref="AssetHeader"/> the asset was declared by.</param>
    /// <param name="debug">The <see cref="AssetDebug"/> child of <paramref name="header"/>.</param>
    public static void Populate(Asset asset, AssetHeader header, AssetDebug debug)
    {
        asset.Id = new AssetId(header.Id);
        asset.Type = header.Type;
        asset.Name = debug.Name;
        asset.FileName = debug.FileName;

        asset.Physical.Type = header.Type;
        asset.Physical.Flags = header.Flags;
        asset.Physical.Alignment = debug.Alignment;
    }

    /// <summary>
    /// Writes <paramref name="asset"/>'s header-sourced fields back onto a freshly built
    /// <paramref name="header"/> and <paramref name="debug"/>. The inverse of
    /// <see cref="Populate"/>, minus the fields commit computes (<see cref="AssetHeader.Offset"/>,
    /// <see cref="AssetHeader.Size"/>, <see cref="AssetHeader.Plus"/>,
    /// <see cref="AssetDebug.Checksum"/>).
    /// </summary>
    /// <param name="asset">The <see cref="Asset"/> to read from.</param>
    /// <param name="header">The <see cref="AssetHeader"/> to populate.</param>
    /// <param name="debug">The <see cref="AssetDebug"/> to populate.</param>
    public static void Apply(Asset asset, AssetHeader header, AssetDebug debug)
    {
        header.Id = asset.Id.Value;
        header.Type = asset.Physical.Type;
        header.Flags = asset.Physical.Flags;

        debug.Alignment = asset.Physical.Alignment;
        debug.Name = asset.Name;
        debug.FileName = asset.FileName;
    }
}

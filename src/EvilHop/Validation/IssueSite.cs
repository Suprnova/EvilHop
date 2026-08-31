using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Validation;

/// <summary>
/// The location within an archive that a <see cref="ValidationIssue"/> pertains to.
/// </summary>
public abstract record IssueSite
{
    /// <summary>
    /// Produces a stable, human-readable locator for this site.
    /// </summary>
    /// <returns>The locator string.</returns>
    public abstract string Describe();
}

/// <summary>
/// An <see cref="IssueSite"/> pointing at the archive as a whole, for facts with no narrower home.
/// </summary>
public sealed record ArchiveSite : IssueSite
{
    /// <inheritdoc/>
    public override string Describe() => "archive";
}

/// <summary>
/// An <see cref="IssueSite"/> pointing at a specific <see cref="Block"/>.
/// </summary>
/// <param name="Path">The path locating the block.</param>
public sealed record BlockSite(BlockPath Path) : IssueSite
{
    /// <inheritdoc/>
    public override string Describe() => Path.ToString();
}

/// <summary>
/// An <see cref="IssueSite"/> pointing at a specific member of a <see cref="Block"/>.
/// </summary>
/// <param name="Path">The path locating the block.</param>
/// <param name="Member">The name of the member on the block.</param>
public sealed record BlockFieldSite(BlockPath Path, string Member) : IssueSite
{
    /// <inheritdoc/>
    public override string Describe() => $"{Path}.{Member}";
}

/// <summary>
/// An <see cref="IssueSite"/> pointing at a specific layer, by its position among the archive's
/// layers.
/// </summary>
/// <param name="Index">The layer's zero-based index.</param>
/// <param name="LayerTypeRaw">The layer's raw, unvalidated type value.</param>
public sealed record LayerSite(int Index, uint LayerTypeRaw) : IssueSite
{
    /// <inheritdoc/>
    public override string Describe() => $"layer[{Index}] (type 0x{LayerTypeRaw:X8})";
}

/// <summary>
/// An <see cref="IssueSite"/> pointing at a specific <see cref="Asset"/>.
/// </summary>
/// <param name="Id">The asset's ID.</param>
/// <param name="TypeRaw">The asset's raw, unvalidated type value.</param>
/// <param name="Name">The asset's name, or <see langword="null"/> if it has none.</param>
public sealed record AssetSite(AssetId Id, uint TypeRaw, string? Name) : IssueSite
{
    /// <inheritdoc/>
    public override string Describe() => Name is null ? $"asset {Id}" : $"asset {Id} ({Name})";
}

/// <summary>
/// An <see cref="IssueSite"/> pointing at a specific member of an <see cref="Assets.Asset"/>.
/// </summary>
/// <param name="Asset">The site of the owning asset.</param>
/// <param name="Member">The name of the member on the asset.</param>
public sealed record AssetFieldSite(AssetSite Asset, string Member) : IssueSite
{
    /// <inheritdoc/>
    public override string Describe() => $"{Asset.Describe()}.{Member}";
}

/// <summary>
/// An <see cref="IssueSite"/> pointing at a specific <see cref="Link"/> owned by a
/// <see cref="BaseAsset"/>.
/// </summary>
/// <param name="Owner">The site of the asset that owns the link.</param>
/// <param name="Index">The link's zero-based index among its owner's links.</param>
public sealed record LinkSite(AssetSite Owner, int Index) : IssueSite
{
    /// <inheritdoc/>
    public override string Describe() => $"{Owner.Describe()}.Links[{Index}]";
}

/// <summary>
/// An <see cref="IssueSite"/> pointing at the gap between an asset's data and the next asset's, in
/// the underlying stream.
/// </summary>
/// <param name="Preceding">The site of the asset preceding the gap.</param>
public sealed record AssetGapSite(AssetSite Preceding) : IssueSite
{
    /// <inheritdoc/>
    public override string Describe() => $"gap after {Preceding.Describe()}";
}

/// <summary>
/// An <see cref="IssueSite"/> pointing at a byte range within the underlying stream, for issues
/// with no owning block or asset.
/// </summary>
/// <param name="Offset">The region's absolute offset within the stream.</param>
/// <param name="Length">The region's length, in bytes.</param>
public sealed record StreamRegionSite(long Offset, long Length) : IssueSite
{
    /// <inheritdoc/>
    public override string Describe() => $"stream[0x{Offset:X}, 0x{Offset + Length:X})";
}

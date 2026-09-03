using EvilHop.Blocks;
using EvilHop.Common;

namespace EvilHop.Assets;

/// <summary>
/// A named grouping of <see cref="Asset"/>s, based on the "category" of their type.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#Layers">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class Layer
{
    /// <summary>
    /// The <see cref="Layer"/>'s type, retrieved from <see cref="LayerHeader.Type"/>.
    /// </summary>
    public LayerType Type { get; set; }
    /// <summary>
    /// The <see cref="Layer"/>'s debug value, retrieved from <see cref="LayerDebug.Value"/>.
    /// </summary>
    public uint DebugValue { get; set; }

    /// <summary>
    /// A list of <see cref="Asset"/>s that belong to this <see cref="Layer"/>.
    /// </summary>
    public IReadOnlyList<Asset> Assets => _assets;
    private readonly List<Asset> _assets = [];

    /// <summary>
    /// Adds the provided <paramref name="asset"/> to this <see cref="Layer"/>.
    /// </summary>
    /// <param name="asset">The <see cref="Asset"/> to add.</param>
    /// <exception cref="InvalidOperationException">If the provided <paramref name="asset"/> already belongs to a <see cref="Layer"/>.</exception>
    public void Add(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (asset.Layer is not null)
            throw new InvalidOperationException("Asset already belongs to a layer.");
        asset.Layer = this;
        _assets.Add(asset);
    }

    /// <summary>
    /// Removes the provided <paramref name="asset"/> from this <see cref="Layer"/>.
    /// </summary>
    /// <param name="asset">The <see cref="Asset"/> to remove.</param>
    /// <returns><see langword="true"/> if <paramref name="asset"/> was present, otherwise <see langword="false"/>.</returns>
    public bool Remove(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (!_assets.Remove(asset)) return false;
        asset.Layer = null;
        return true;
    }
}

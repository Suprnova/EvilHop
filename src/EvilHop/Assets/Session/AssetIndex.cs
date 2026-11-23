using EvilHop.Common;

namespace EvilHop.Assets;

/// <summary>
/// One asset ID that an asset names: a link's destination, a group's item, a typed asset's own ID
/// field, and the like.
/// </summary>
/// <param name="Source">The asset that names <paramref name="Target"/>.</param>
/// <param name="Layer">The index of <paramref name="Source"/>'s layer in <see cref="AssetView.Layers"/>.</param>
/// <param name="Index">The position of <paramref name="Source"/> in its layer.</param>
/// <param name="Field">
/// The member path that holds the ID, from <paramref name="Source"/>: <c>Links[2].DestinationAssetId</c>,
/// <c>Items[5]</c>, or <c>Physical.ModelId</c> for a member of the physical surface.
/// </param>
/// <param name="Target">The ID named. It need not belong to an asset of the same archive.</param>
public readonly record struct AssetReference(Asset Source, int Layer, int Index, string Field, AssetId Target);

/// <summary>
/// Lookups and plain joins over an <see cref="AssetView"/>'s assets: an asset by ID, by name and by
/// type, the asset IDs an asset names, and every asset that names an ID.
/// </summary>
/// <remarks>
/// <para>
/// Every list is in load order: layer by layer, then by position in the layer. The index is built
/// from the view's assets as they are when <see cref="AssetView.Index"/> is first read; editing them
/// afterwards isn't reflected.
/// </para>
/// <para>
/// References are found from the members' types: every <see cref="AssetId"/> an asset holds, in its
/// own members, in collections and in nested types such as links and script events, is a reference,
/// apart from <see cref="AssetId.None"/> and the members that store the asset's own ID again. A link
/// parameter is a reference only when it is an <see cref="AssetIdParameter"/>.
/// </para>
/// </remarks>
public sealed class AssetIndex
{
    private readonly Dictionary<AssetId, Asset> _byId = [];
    private readonly Dictionary<string, List<Asset>> _byStoredName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<AssetType, List<Asset>> _byType = [];
    private readonly Dictionary<Asset, int> _loadOrder = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Asset, List<AssetReference>> _from = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<AssetId, List<AssetReference>> _to = [];

    internal AssetIndex(IReadOnlyList<Layer> layers)
    {
        for (int layer = 0; layer < layers.Count; layer++)
        {
            var assets = layers[layer].Assets;
            for (int index = 0; index < assets.Count; index++)
            {
                var asset = assets[index];
                _loadOrder[asset] = _loadOrder.Count;
                _byId.TryAdd(asset.Id, asset);
                AddTo(_byStoredName, asset.Name, asset);
                AddTo(_byType, asset.Type, asset);

                var from = new List<AssetReference>();
                AssetReferenceWalker.Walk(asset, (field, target) =>
                {
                    var reference = new AssetReference(asset, layer, index, field, target);
                    from.Add(reference);
                    AddTo(_to, target, reference);
                });
                _from[asset] = from;
            }
        }
    }

    /// <summary>
    /// The asset whose ID is <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The ID to look up.</param>
    /// <returns>The asset, or <see langword="null"/> when no asset of the view has that ID.</returns>
    public Asset? Find(AssetId id) => _byId.GetValueOrDefault(id);

    /// <summary>
    /// Every asset <paramref name="name"/> can mean: each asset whose ID is the hash of
    /// <paramref name="name"/> under its own type's transform (<see cref="AssetId.FromName(string, AssetType)"/>),
    /// and each asset whose stored name is <paramref name="name"/>, ignoring letter case.
    /// </summary>
    /// <remarks>
    /// The ID finds an asset whose stored name differs from the one its ID was hashed from, such as an
    /// <see cref="AssetType.Animation"/> stored without <c>.anm</c>. The stored name finds one by the
    /// truncated form an archive keeps of a long name, whose hash isn't the ID.
    /// </remarks>
    /// <param name="name">The name to look up.</param>
    /// <returns>The matching assets, in load order.</returns>
    public IReadOnlyList<Asset> FindByName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var byId = _byType.Keys
            .Select(type => Find(AssetId.FromName(name, type)))
            .OfType<Asset>()
            .Where(asset => asset.Id == AssetId.FromName(name, asset.Type));
        var byStoredName = _byStoredName.GetValueOrDefault(name) ?? [];

        return [.. byId.Concat(byStoredName).Distinct(ReferenceEqualityComparer.Instance).Cast<Asset>().OrderBy(asset => _loadOrder[asset])];
    }

    /// <summary>
    /// Every asset of <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type to look up.</param>
    /// <returns>The assets of that type, in load order.</returns>
    public IReadOnlyList<Asset> OfType(AssetType type) => _byType.GetValueOrDefault(type) ?? [];

    /// <summary>
    /// The stored name of the asset whose ID is <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The ID to name.</param>
    /// <returns>The name, or <see langword="null"/> when no asset of the view has that ID.</returns>
    public string? NameOf(AssetId id) => Find(id)?.Name;

    /// <summary>
    /// The asset IDs <paramref name="asset"/> names.
    /// </summary>
    /// <param name="asset">An asset of the view.</param>
    /// <returns>Its references, in member order.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="asset"/> isn't an asset of the view.</exception>
    public IReadOnlyList<AssetReference> ReferencesFrom(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return _from.TryGetValue(asset, out var references)
            ? references
            : throw new ArgumentException($"The asset isn't one of this {nameof(AssetIndex)}'s.", nameof(asset));
    }

    /// <summary>
    /// Every reference to <paramref name="id"/>, whether or not an asset of the view has that ID.
    /// </summary>
    /// <param name="id">The ID named.</param>
    /// <returns>The references, in load order of their sources.</returns>
    public IReadOnlyList<AssetReference> ReferencesTo(AssetId id) => _to.GetValueOrDefault(id) ?? [];

    private static void AddTo<TKey, TValue>(Dictionary<TKey, List<TValue>> lists, TKey key, TValue value) where TKey : notnull
    {
        if (!lists.TryGetValue(key, out var list)) lists[key] = list = [];
        list.Add(value);
    }
}

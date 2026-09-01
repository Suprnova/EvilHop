using EvilHop.Common;
using System.Text.Json.Nodes;

namespace EvilHop.Tests.Inventory;

/// <summary>
/// Loads and parses every committed <c>corpus/*.json</c> inventory once, keyed by game. Every test
/// that reads an inventory goes through this rather than the filesystem directly.
/// </summary>
/// <remarks>
/// Never reads <c>artifacts/</c> - only the committed inventories the corpus tool already reduced
/// from it.
/// </remarks>
public sealed class InventoryFixture
{
    /// <summary>The fixture, loaded once and shared by every test.</summary>
    public static InventoryFixture Instance { get; } = new();

    /// <summary>Every inventory found, keyed by the game it covers.</summary>
    public IReadOnlyDictionary<GameVersion, JsonObject> Inventories { get; }

    private InventoryFixture()
    {
        string corpusDirectory = Path.Combine(AppContext.BaseDirectory, "Corpus");
        var inventories = new Dictionary<GameVersion, JsonObject>();

        foreach (GameVersion game in Enum.GetValues<GameVersion>())
        {
            string path = Path.Combine(corpusDirectory, $"{game.ToString().ToLowerInvariant()}.json");
            if (!File.Exists(path)) continue;

            inventories[game] = (JsonObject)JsonNode.Parse(File.ReadAllText(path))!;
        }

        Inventories = inventories;
    }

    /// <summary>
    /// Every facet's recorded <c>observations</c> for <paramref name="game"/>, keyed by facet ID, if
    /// that game has an inventory.
    /// </summary>
    /// <param name="game">The game to look up.</param>
    /// <returns>The facets object, or <see langword="null"/> if none.</returns>
    public JsonObject? Facets(GameVersion game) =>
        Inventories.TryGetValue(game, out var inventory) ? inventory["facets"] as JsonObject : null;

    /// <summary>
    /// The recorded <c>ValueSet</c> for <paramref name="observableId"/> in <paramref name="game"/>'s
    /// inventory, searching every facet - a caller has no reason to know which facet an observable
    /// landed in, only its ID.
    /// </summary>
    /// <param name="game">The game to look up.</param>
    /// <param name="observableId">The observable's identifier.</param>
    /// <returns>The recorded <c>ValueSet</c>, or <see langword="null"/> if none was recorded.</returns>
    public JsonObject? ValueSet(GameVersion game, string observableId) =>
        Facets(game)?
            .Select(facet => facet.Value?["observations"]?[observableId] as JsonObject)
            .FirstOrDefault(valueSet => valueSet is not null);

    /// <summary>Every observation ID recorded in any facet of <paramref name="game"/>'s inventory.</summary>
    /// <param name="game">The game to look up.</param>
    /// <returns>Every recorded observation ID.</returns>
    public IEnumerable<string> ObservationIds(GameVersion game) =>
        Facets(game)?.SelectMany(facet => (facet.Value?["observations"] as JsonObject)?.Select(o => o.Key) ?? [])
            ?? [];
}

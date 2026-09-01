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
    /// The <c>blockFields</c> facet's recorded <c>ValueSet</c>s for <paramref name="game"/>, if that
    /// game has an inventory.
    /// </summary>
    /// <param name="game">The game to look up.</param>
    /// <returns>The observation object, keyed by observable ID, or <see langword="null"/> if none.</returns>
    public JsonObject? BlockFieldsObservations(GameVersion game) =>
        Inventories.TryGetValue(game, out var inventory)
            ? inventory["facets"]?["blockFields"]?["observations"] as JsonObject
            : null;
}

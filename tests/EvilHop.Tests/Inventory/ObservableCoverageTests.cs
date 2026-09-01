using EvilHop.Common;
using EvilHop.Validation;

namespace EvilHop.Tests.Inventory;

/// <summary>
/// Guards the observable catalogue against silent rot: an observable with no corresponding record
/// anywhere, or a recorded observable that no longer exists in the catalogue, would otherwise be
/// invisible until someone happened to look.
/// </summary>
public class ObservableCoverageTests
{
    /// <summary>
    /// Observables not expected to appear in every game's inventory, and why. Empty until a real
    /// gap is found - a waiver is evidence, not a default.
    /// </summary>
    private static readonly IReadOnlySet<(string ObservableId, GameVersion Game)> Waivers =
        new HashSet<(string ObservableId, GameVersion Game)>();

    public static TheoryData<string, GameVersion> BlockScopedObservablesByGame()
    {
        var cases = new TheoryData<string, GameVersion>();

        foreach (var observable in ValidationCatalogue.Instance.Observables.Where(o => o.Scope == ObservableScope.Block))
            foreach (GameVersion game in Enum.GetValues<GameVersion>())
            {
                if (Waivers.Contains((observable.Id, game))) continue;
                cases.Add(observable.Id, game);
            }

        return cases;
    }

    [Theory]
    [MemberData(nameof(BlockScopedObservablesByGame))]
    public void Observations_EveryBlockScopedObservable_HasARecordInTheGamesInventory(string observableId, GameVersion game)
    {
        Assert.True(
            InventoryFixture.Instance.ValueSet(game, observableId) is not null,
            $"'{observableId}' has no record in {game}'s inventory, and no waiver excuses it.");
    }

    [Fact]
    public void Observations_EveryRecordedObservable_StillExistsInTheCatalogue()
    {
        var knownIds = ValidationCatalogue.Instance.Observables.Select(o => o.Id).ToHashSet();

        foreach (var game in InventoryFixture.Instance.Inventories.Keys)
            foreach (string observableId in InventoryFixture.Instance.ObservationIds(game))
            {
                // Recorded by the corpus tool's structure facet directly from the block tree's shape,
                // not from a catalogue-declared observable - nothing to check them against here.
                if (observableId is "archive.rootSequence" or "PACK.children") continue;

                Assert.True(
                    knownIds.Contains(observableId),
                    $"'{observableId}' is recorded in {game}'s inventory but no longer exists in the catalogue.");
            }
    }
}

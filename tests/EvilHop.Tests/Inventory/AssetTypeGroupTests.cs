using EvilHop.Common;
using EvilHop.Validation;
using System.Text.Json.Nodes;

namespace EvilHop.Tests.Inventory;

/// <summary>
/// Checks the two halves of an inventory that record asset types against each other: the types
/// <c>blockFields</c> saw in <c>AHDR</c>, and the groups <c>assetFields</c> partitioned assets into.
/// </summary>
/// <remarks>
/// Both are observations rather than verdicts, so this asserts one against the other rather than
/// either against the library - which makes it the canary for a facet regenerated against a
/// different set of archives than its neighbour, the one way a new asset type could show up in one
/// place and not the other.
/// </remarks>
public class AssetTypeGroupTests
{
    public static TheoryData<GameVersion> Games()
    {
        var cases = new TheoryData<GameVersion>();
        foreach (var game in InventoryFixture.Instance.Inventories.Keys) cases.Add(game);
        return cases;
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void Groups_EveryKeyOfEveryGroupedRecord_AppearsInTheGamesRecordedAssetTypes(GameVersion game)
    {
        var recordedTypes = RecordedAssetTypes(game);

        foreach (var (observableId, keys) in GroupKeysByObservable(game))
            foreach (uint key in keys)
                Assert.True(
                    recordedTypes.Contains(key),
                    $"'{observableId}' has a group for type 0x{key:X8} in {game}'s inventory, but AHDR.type " +
                    "never recorded it. The two facets were generated against different archives.");
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void Groups_EveryGroupedObservable_RecordsAGroupPerTypeItCanApplyTo(GameVersion game)
    {
        // Alignment is copied onto every asset from its ADBG regardless of what the type parses
        // into, so its groups are the complete set - unlike a payload-sourced field, whose groups
        // are only the types some codec knows the shape of.
        var keys = GroupKeysByObservable(game)
            .Where(record => record.ObservableId == "asset.physical.alignment@assetType")
            .SelectMany(record => record.Keys)
            .ToHashSet();

        Assert.Equal(RecordedAssetTypes(game), keys);
    }

    [Fact]
    public void Groups_EveryGroupedObservableInTheCatalogue_IsRecordedAsAGroupedRecord()
    {
        var grouped = ValidationCatalogue.Instance.Observables
            .Where(o => o.Grouping is not ObservableGrouping.None)
            .Select(o => o.Id);

        foreach (var game in InventoryFixture.Instance.Inventories.Keys)
            foreach (string observableId in grouped)
                Assert.Equal("grouped", InventoryFixture.Instance.ValueSet(game, observableId)?["kind"]?.GetValue<string>());
    }

    private static HashSet<uint> RecordedAssetTypes(GameVersion game) =>
        [.. InventoryFixture.Instance.ValueSet(game, "AHDR.type")?["values"]?.AsArray()
            .Select(entry => (uint)entry!["value"]!.GetValue<long>()) ?? []];

    private static IEnumerable<(string ObservableId, IReadOnlyList<uint> Keys)> GroupKeysByObservable(GameVersion game) =>
        InventoryFixture.Instance.ObservationIds(game)
            .Select(id => (Id: id, ValueSet: InventoryFixture.Instance.ValueSet(game, id)))
            .Where(record => record.ValueSet?["kind"]?.GetValue<string>() == "grouped")
            .Select(record => (record.Id, Keys(record.ValueSet!)));

    private static IReadOnlyList<uint> Keys(JsonObject grouped) =>
        [.. grouped["groups"]!.AsArray().Select(group => group!["key"]!.GetValue<uint>())];
}

using EvilHop.Common;
using EvilHop.Validation;
using System.Text.Json.Nodes;

namespace EvilHop.Tests.Inventory;

/// <summary>
/// Replays every <see cref="ValueRule"/> in <see cref="ValidationCatalogue"/> against the values
/// <c>corpus/*.json</c> recorded for it, offline and without touching <c>artifacts/</c>. This is
/// what makes a rule whose input space is small and closed cheap to change: correcting its
/// definition only ever changes this test's verdict, never the recorded evidence.
/// </summary>
public class ValueRuleTests
{
    public static TheoryData<string, GameVersion, object> Cases()
    {
        var cases = new TheoryData<string, GameVersion, object>();

        foreach (var rule in ValidationCatalogue.Instance.Rules)
            foreach (GameVersion game in Enum.GetValues<GameVersion>())
            {
                if (!rule.AppliesTo(new ValidationContext(GameProfiles.For(game)))) continue;

                if (InventoryFixture.Instance.BlockFieldsObservations(game)?[rule.ObservableId] is not JsonObject valueSet)
                    continue;

                foreach (object value in RecordedValues(valueSet))
                    cases.Add(rule.Id, game, value);
            }

        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Holds_ForEveryValueRecordedInTheInventory_IsTrue(string ruleId, GameVersion game, object value)
    {
        var rule = ValidationCatalogue.Instance.Rules.Single(r => r.Id == ruleId);
        var context = new ValidationContext(GameProfiles.For(game));

        Assert.True(rule.Holds(value, context), $"'{ruleId}' does not hold for {value}, recorded in {game}'s inventory.");
    }

    private static IEnumerable<object> RecordedValues(JsonObject valueSet) => valueSet["kind"]?.GetValue<string>() switch
    {
        "enumerated" => valueSet["values"]!.AsArray().Select(entry => FromJsonValue((JsonValue)entry!["value"]!)),
        "bitmask" => [FromJsonValue((JsonValue)valueSet["union"]!)],
        _ => []
    };

    private static object FromJsonValue(JsonValue value)
    {
        if (value.TryGetValue(out uint u)) return u;
        if (value.TryGetValue(out bool b)) return b;
        if (value.TryGetValue(out string? s)) return s!;
        throw new NotSupportedException($"Unsupported recorded observation: {value}");
    }
}

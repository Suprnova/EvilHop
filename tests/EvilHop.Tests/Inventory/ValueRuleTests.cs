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
    /// <summary>
    /// (Rule, game, value) combinations known to violate a rule's default-profile replay for a
    /// quirk-driven reason a per-game context can't represent - a ValueSet records "value X occurred
    /// N times in game G", not which specific archive or quirk produced any one occurrence, so a
    /// rule whose verdict depends on a quirk (like <c>[RequiredChild(ExceptQuirks: ...)]</c>) can't
    /// be replayed exactly for the archive that carried it. A waiver is evidence, not a default -
    /// each entry names the real archive and quirk that justifies it.
    /// </summary>
    private static readonly IReadOnlySet<(string RuleId, GameVersion Game, object Value)> Waivers = new HashSet<(string, GameVersion, object)>
    {
        // font2.HIP carries FormatQuirks.OmitsPlatformBlock - a real BFBB archive with no PLAT
        // child - so its recorded "Platform absent" occurrence fails the rule's default-profile
        // (unquirked) replay even though EvilHop excuses it correctly for the archive it came from.
        ("pack.platform-required", GameVersion.BFBB, 0u)
    };

    public static TheoryData<string, GameVersion, object> Cases()
    {
        var cases = new TheoryData<string, GameVersion, object>();

        foreach (var rule in ValidationCatalogue.Instance.Rules)
            foreach (GameVersion game in Enum.GetValues<GameVersion>())
            {
                if (!rule.AppliesTo(new ValidationContext(GameProfiles.For(game)))) continue;

                if (InventoryFixture.Instance.ValueSet(game, rule.ObservableId) is not { } valueSet)
                    continue;

                foreach (object value in RecordedValues(valueSet))
                {
                    if (Waivers.Contains((rule.Id, game, value))) continue;
                    cases.Add(rule.Id, game, value);
                }
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

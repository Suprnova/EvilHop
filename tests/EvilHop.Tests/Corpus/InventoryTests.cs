using EvilHop.Blocks;
using EvilHop.Common;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace EvilHop.Tests.Corpus;

/// <summary>
/// Reads every committed <c>corpus/*.json</c> inventory and asserts its observations against
/// EvilHop's current code. Hermetic - never touches the local corpus, only the frozen files checked
/// into git, so it runs on every CI build with no artifacts present.
/// </summary>
/// <remarks>
/// The governing rule: the inventory records observations, and only assertions coupling that frozen
/// data to mutable code belong here - enum definitions, the asset ID hash, and field names can all
/// change out from under a committed value without any other test noticing.
/// </remarks>
public class InventoryTests
{
    private static readonly IReadOnlyDictionary<string, JsonElement> Inventories = LoadInventories();

    private static IReadOnlyDictionary<string, JsonElement> LoadInventories()
    {
        string corpusDirectory = Path.Combine(FindRepositoryRoot(), "corpus");
        return Directory.GetFiles(corpusDirectory, "*.json").ToDictionary(
            path => Path.GetFileNameWithoutExtension(path)!,
            path => JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "EvilHop.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root (EvilHop.slnx) from the test output directory.");
    }

    private static (Type? BlockType, PropertyInfo? Property) ResolveField(string fieldKey)
    {
        int dot = fieldKey.IndexOf('.');
        string blockTypeName = fieldKey[..dot];
        string propertyName = fieldKey[(dot + 1)..];

        var blockType = typeof(Block).Assembly.GetType($"EvilHop.Blocks.{blockTypeName}");
        return (blockType, blockType?.GetProperty(propertyName));
    }

    private static uint ParseHex(string hex) => Convert.ToUInt32(hex[2..], 16);

    /// <summary>
    /// Parses an enum-field "set" key back into its numeric value - either a hex string, or, for
    /// FourCCs like <see cref="AssetType"/>, the ASCII string Corpus renders when every byte is
    /// printable (the reverse of the encoding EvilHop.Corpus's <c>ValueFormatter</c> applies).
    /// </summary>
    private static ulong ParseEnumValue(string key)
    {
        if (key.StartsWith("0x", StringComparison.Ordinal))
            return Convert.ToUInt64(key[2..], 16);

        ulong value = 0;
        foreach (char c in key)
            value = (value << 8) | (byte)c;
        return value;
    }

    public static IEnumerable<object[]> FieldKeys() =>
        Inventories.Values
            .SelectMany(root => root.GetProperty("fields").EnumerateObject())
            .Select(p => p.Name)
            .Distinct()
            .Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(FieldKeys))]
    public void Field_KeyMapsToLivePublicReadWriteProperty(string fieldKey)
    {
        var (blockType, property) = ResolveField(fieldKey);

        Assert.NotNull(blockType);
        Assert.NotNull(property);
        Assert.NotNull(property!.GetGetMethod());
        Assert.NotNull(property.GetSetMethod());
    }

    [Theory]
    [MemberData(nameof(FieldKeys))]
    public void Field_EnumTypedRecordedValues_AreAllDefinedEnumMembers(string fieldKey)
    {
        var (_, property) = ResolveField(fieldKey);
        if (property is null || !property.PropertyType.IsEnum) return;

        bool isFlags = property.PropertyType.GetCustomAttribute<FlagsAttribute>() is not null;
        ulong knownBits = isFlags
            ? Enum.GetValues(property.PropertyType).Cast<object>().Aggregate(0UL, (bits, member) => bits | Convert.ToUInt64(member))
            : 0;

        // Collect every offending value before asserting, rather than stopping at the first - a
        // single failed Assert.True per test run hides how many other values are also undefined.
        List<string> undefined = [];

        foreach (var root in Inventories.Values)
        {
            if (!root.GetProperty("fields").TryGetProperty(fieldKey, out var field)) continue;
            if (field.GetProperty("kind").GetString() != "set") continue;

            foreach (var value in field.GetProperty("values").EnumerateObject())
            {
                ulong raw = ParseEnumValue(value.Name);
                if (isFlags)
                {
                    // A [Flags] enum's valid values are combinations of known bits, not necessarily
                    // named members themselves - Enum.IsDefined only matches exact named members.
                    if ((raw & ~knownBits) != 0) undefined.Add(value.Name);
                }
                else
                {
                    var enumValue = Enum.ToObject(property.PropertyType, raw);
                    if (!Enum.IsDefined(property.PropertyType, enumValue)) undefined.Add(value.Name);
                }
            }
        }

        Assert.True(undefined.Count == 0, $"{fieldKey} recorded undefined values: {string.Join(", ", undefined.Distinct())}.");
    }

    /// <summary>
    /// A plain <see cref="FactAttribute"/>, not a <c>[Theory]</c>/<c>[MemberData]</c> pair - the
    /// corpus can legitimately record zero unexplained samples (every name derives cleanly), which
    /// xUnit treats as a hard failure for an empty theory data source.
    /// </summary>
    [Fact]
    public void AssetId_EveryUnexplainedSample_StillFailsToDeriveUnderEveryKnownRule()
    {
        foreach (var root in Inventories.Values)
            foreach (var sample in root.GetProperty("invariants").GetProperty("assetIdMatchesNameHash").GetProperty("unexplained").EnumerateArray())
            {
                string name = sample.GetProperty("name").GetString()!;
                uint id = ParseHex(sample.GetProperty("expected").GetString()!);
                var type = Enum.Parse<AssetType>(sample.GetProperty("type").GetString()!);

                Assert.NotEqual(id, BKDRHash.Calculate(name));

                if (type == AssetType.Animation)
                    Assert.NotEqual(id, BKDRHash.Calculate(Path.ChangeExtension(name, ".anm")));

                if (type == AssetType.MorphTarget)
                {
                    Assert.NotEqual(id, BKDRHash.Calculate(Path.ChangeExtension(name, ".mph")));
                    Assert.NotEqual(id, BKDRHash.Calculate(name + ".mph"));
                }
            }
    }
}

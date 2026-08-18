using EvilHop.Blocks;
using EvilHop.Common;
using System.Reflection;
using System.Text.Json;

namespace EvilHop.Tests.Corpus;

/// <summary>
/// Reads the committed <c>corpus/v1.json</c> and asserts its observations against EvilHop's
/// current code. Hermetic - never touches the local corpus, only the frozen file checked into git,
/// so it runs on every CI build with no artifacts present.
/// </summary>
/// <remarks>
/// The governing rule: the inventory records observations, and only assertions coupling that frozen
/// data to mutable code belong here - enum definitions, the asset ID hash, and field names can all
/// change out from under a committed value without any other test noticing.
/// </remarks>
public class InventoryTests
{
    private static readonly JsonElement Root = LoadInventory();

    private static JsonElement LoadInventory()
    {
        string path = Path.Combine(FindRepositoryRoot(), "corpus", "v1.json");
        using var stream = File.OpenRead(path);
        return JsonDocument.Parse(stream).RootElement.Clone();
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
        Root.GetProperty("fields").EnumerateObject().Select(p => new object[] { p.Name });

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

        var field = Root.GetProperty("fields").GetProperty(fieldKey);
        if (field.GetProperty("kind").GetString() != "set") return;

        bool isFlags = property.PropertyType.GetCustomAttribute<FlagsAttribute>() is not null;
        ulong knownBits = isFlags
            ? Enum.GetValues(property.PropertyType).Cast<object>().Aggregate(0UL, (bits, member) => bits | Convert.ToUInt64(member))
            : 0;

        foreach (var value in field.GetProperty("values").EnumerateObject())
        {
            ulong raw = ParseEnumValue(value.Name);
            if (isFlags)
            {
                // A [Flags] enum's valid values are combinations of known bits, not necessarily
                // named members themselves - Enum.IsDefined only matches exact named members.
                Assert.True((raw & ~knownBits) == 0, $"{fieldKey} recorded value {value.Name} with unknown flag bits.");
            }
            else
            {
                var enumValue = Enum.ToObject(property.PropertyType, raw);
                Assert.True(Enum.IsDefined(property.PropertyType, enumValue), $"{fieldKey} recorded undefined value {value.Name}.");
            }
        }
    }

    /// <summary>
    /// A plain <see cref="FactAttribute"/>, not a <c>[Theory]</c>/<c>[MemberData]</c> pair - the
    /// corpus can legitimately record zero unexplained samples (every name derives cleanly), which
    /// xUnit treats as a hard failure for an empty theory data source.
    /// </summary>
    [Fact]
    public void AssetId_EveryUnexplainedSample_StillFailsToDeriveUnderEveryKnownRule()
    {
        foreach (var sample in Root.GetProperty("invariants").GetProperty("assetIdMatchesNameHash").GetProperty("unexplained").EnumerateArray())
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

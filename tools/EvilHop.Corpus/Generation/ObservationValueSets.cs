using EvilHop.Validation;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Generation;

/// <summary>
/// Reduces a bag of per-archive occurrences into the <c>ValueSet</c> shape every facet's
/// <c>observations</c> is built from - distinct values with counts and witnesses for
/// <see cref="ObservableCardinality.Enumerated"/>, a single bitwise union for
/// <see cref="ObservableCardinality.Bitmask"/> - and, for a grouped observable, into a container of
/// one such <c>ValueSet</c> per group key. Takes a value's shape rather than an
/// <see cref="Observable"/> itself where it can, so it serves both catalogue-declared observables and
/// facts a generator computes directly and records under its own ID.
/// </summary>
internal static class ObservationValueSets
{
    /// <summary>
    /// The most distinct values one <c>ValueSet</c> may record before reduction fails, so a
    /// mis-declared cardinality is a loud error rather than a multi-megabyte commit. Applies to a
    /// grouped observable per group, since each group is a <c>ValueSet</c> in its own right.
    /// </summary>
    public const int MaxEnumeratedValues = 512;

    /// <summary>
    /// The most groups a grouped observable may record. Sized at roughly twice the distinct asset
    /// types any one game is known to ship, so grouping by something far wider than intended - an
    /// ID rather than a type - fails here, where the message can say what went wrong, rather than at
    /// <see cref="MaxGroupedValues"/>.
    /// </summary>
    public const int MaxGroups = 128;

    /// <summary>
    /// The most value rows a grouped observable may record across all of its groups.
    /// </summary>
    /// <remarks>
    /// <see cref="MaxEnumeratedValues"/> alone doesn't bound a grouped record: a hundred groups of
    /// five hundred values each is exactly the commit that cap exists to prevent. This is the bound
    /// that actually holds, and it is deliberately a failure rather than a degradation - collapsing
    /// an over-cap record to a summary would make its committed shape depend on the data crossing a
    /// threshold, so adding one archive could silently rewrite it.
    /// </remarks>
    public const int MaxGroupedValues = 1024;

    /// <summary>
    /// Records one observed value in an archive's map-stage contribution, bucketing it by group key
    /// when the observable it came from is grouped.
    /// </summary>
    /// <param name="record">The archive's map record.</param>
    /// <param name="observation">The observation to record.</param>
    public static void Append(JsonObject record, Observation observation)
    {
        if (observation.GroupKey is not { } key)
        {
            if (record[observation.ObservableId] is not JsonArray values) record[observation.ObservableId] = values = [];
            values.Add(ToJsonValue(observation.Value));
            return;
        }

        if (record[observation.ObservableId] is not JsonObject groups) record[observation.ObservableId] = groups = [];

        string keyText = KeyText(key);
        if (groups[keyText] is not JsonArray grouped) groups[keyText] = grouped = [];
        grouped.Add(ToJsonValue(observation.Value));
    }

    /// <summary>
    /// Reduces every occurrence of <paramref name="observable"/> across <paramref name="records"/>
    /// into its committed shape, grouped or not as it declares.
    /// </summary>
    /// <param name="observable">The observable to reduce.</param>
    /// <param name="records">Every covered archive's map-stage contribution.</param>
    /// <returns>The reduced <c>ValueSet</c>, or the container of them.</returns>
    public static JsonObject Reduce(Observable observable, IReadOnlyList<MappedArchive> records) =>
        observable.Grouping is ObservableGrouping.None
            ? Reduce(observable.Id, observable.Cardinality, observable.Presentation, records)
            : ReduceGrouped(observable, records);

    /// <summary>
    /// Reduces every occurrence of <paramref name="id"/> across <paramref name="records"/> into its
    /// <c>ValueSet</c>.
    /// </summary>
    /// <param name="id">The identifier the value was recorded under in each archive's map record.</param>
    /// <param name="cardinality">How the value's distinct occurrences should be recorded.</param>
    /// <param name="presentation">How the value's occurrences should be rendered.</param>
    /// <param name="records">Every covered archive's map-stage contribution.</param>
    /// <returns>The reduced <c>ValueSet</c>.</returns>
    public static JsonObject Reduce(
        string id, ObservableCardinality cardinality, ObservablePresentation presentation,
        IReadOnlyList<MappedArchive> records)
    {
        var occurrences = records.SelectMany(record => Occurrences(record, id)).ToList();

        var valueSet = new JsonObject
        {
            ["kind"] = KindName(cardinality),
            ["presentation"] = PresentationName(presentation)
        };

        WritePayload(valueSet, id, cardinality, presentation, occurrences);
        return valueSet;
    }

    private static JsonObject ReduceGrouped(Observable observable, IReadOnlyList<MappedArchive> records)
    {
        var occurrencesByKey = GroupedOccurrences(observable.Id, records);

        if (occurrencesByKey.Count > MaxGroups)
            throw new InvalidOperationException(
                $"'{observable.Id}' has {occurrencesByKey.Count} groups, past the {MaxGroups}-group cap. " +
                $"It may be grouped by something wider than {observable.Grouping}.");

        var keyPresentation = observable.KeyPresentation ?? ObservablePresentation.Number;
        var groups = new JsonArray();
        int rows = 0;

        foreach (var (key, occurrences) in occurrencesByKey.OrderBy(pair => pair.Key))
        {
            string? keyDisplay = DisplayFor((long)key, keyPresentation);

            var group = new JsonObject { ["key"] = key };
            if (keyDisplay is not null) group["keyDisplay"] = keyDisplay;

            WritePayload(group, $"{observable.Id}[{keyDisplay ?? KeyText(key)}]", observable.Cardinality, observable.Presentation, occurrences);

            rows += group["values"] is JsonArray values ? values.Count : 1;
            groups.Add(group);
        }

        if (rows > MaxGroupedValues)
            throw new InvalidOperationException(
                $"'{observable.Id}' records {rows} values across {groups.Count} groups, past the " +
                $"{MaxGroupedValues}-value cap. Widest groups: {WidestGroups(groups)}. " +
                "Its cardinality may be mis-declared.");

        return new JsonObject
        {
            ["kind"] = "grouped",
            ["groupedBy"] = CamelCase(observable.Grouping.ToString()),
            ["keyPresentation"] = PresentationName(keyPresentation),
            ["valueKind"] = KindName(observable.Cardinality),
            ["presentation"] = PresentationName(observable.Presentation),
            ["groups"] = groups
        };
    }

    /// <summary>
    /// Writes a value set's payload - its <c>values</c>, or its <c>union</c> - onto
    /// <paramref name="target"/>, which is either a <c>ValueSet</c> that already carries its own kind
    /// and presentation or one group of a grouped record, where both are hoisted to the container.
    /// </summary>
    private static void WritePayload(
        JsonObject target, string id, ObservableCardinality cardinality, ObservablePresentation presentation,
        IReadOnlyList<(string Path, object Value)> occurrences)
    {
        switch (cardinality)
        {
            case ObservableCardinality.Bitmask:
                WriteBitmask(target, occurrences);
                break;

            case ObservableCardinality.Enumerated:
                WriteEnumerated(target, id, presentation, occurrences);
                break;

            default:
                throw new NotSupportedException($"ValueSet reduction doesn't support {cardinality} values yet.");
        }
    }

    private static IEnumerable<(string Path, object Value)> Occurrences(MappedArchive record, string id) =>
        record.Record[id] is JsonArray values
            ? values.Select(node => (record.Path, FromJsonValue((JsonValue)node!)))
            : [];

    private static Dictionary<uint, List<(string Path, object Value)>> GroupedOccurrences(
        string id, IReadOnlyList<MappedArchive> records)
    {
        var occurrencesByKey = new Dictionary<uint, List<(string Path, object Value)>>();

        foreach (var record in records)
        {
            if (record.Record[id] is not JsonObject groups) continue;

            foreach (var (keyText, node) in groups)
            {
                if (node is not JsonArray values) continue;

                uint key = uint.Parse(keyText, CultureInfo.InvariantCulture);
                if (!occurrencesByKey.TryGetValue(key, out var occurrences))
                    occurrencesByKey[key] = occurrences = [];

                occurrences.AddRange(values.Select(value => (record.Path, FromJsonValue((JsonValue)value!))));
            }
        }

        return occurrencesByKey;
    }

    private static void WriteBitmask(JsonObject target, IReadOnlyList<(string Path, object Value)> occurrences)
    {
        uint union = occurrences.Aggregate(0u, (acc, occurrence) => acc | (uint)(long)occurrence.Value);

        target["union"] = union;
        target["display"] = $"0x{union:X8}";
    }

    private static void WriteEnumerated(
        JsonObject target, string id, ObservablePresentation presentation,
        IReadOnlyList<(string Path, object Value)> occurrences)
    {
        var groups = occurrences.GroupBy(o => o.Value).OrderBy(g => g.Key, ValueComparer.Instance).ToList();
        if (groups.Count > MaxEnumeratedValues)
            throw new InvalidOperationException(
                $"'{id}' has {groups.Count} distinct values, past the {MaxEnumeratedValues}-value cap " +
                "for an enumerated value. Its cardinality may be mis-declared.");

        var values = new JsonArray();

        foreach (var group in groups)
        {
            var witnesses = new JsonArray();
            foreach (string path in group.Select(o => o.Path).Distinct().OrderBy(p => p, StringComparer.Ordinal).Take(2))
                witnesses.Add(path);

            var entry = new JsonObject();
            if (DisplayFor(group.Key, presentation) is { } display) entry["display"] = display;
            entry["value"] = ToJsonValue(group.Key);
            entry["count"] = group.Count();
            entry["witnesses"] = witnesses;

            values.Add(entry);
        }

        target["values"] = values;
    }

    /// <summary>The three widest groups, named and sized, for an over-cap failure to point at.</summary>
    private static string WidestGroups(JsonArray groups) =>
        string.Join(", ", groups
            .Select(group => (Name: group!["keyDisplay"]?.GetValue<string>() ?? group["key"]!.ToJsonString(),
                              Rows: (group["values"] as JsonArray)?.Count ?? 1))
            .OrderByDescending(group => group.Rows)
            .Take(3)
            .Select(group => $"{group.Name} ({group.Rows})"));

    private static string KindName(ObservableCardinality cardinality) => cardinality.ToString().ToLowerInvariant();

    private static string PresentationName(ObservablePresentation presentation) => presentation.ToString().ToLowerInvariant();

    private static string CamelCase(string name) => $"{char.ToLowerInvariant(name[0])}{name[1..]}";

    private static string KeyText(uint key) => key.ToString(CultureInfo.InvariantCulture);

    private static string? DisplayFor(object value, ObservablePresentation presentation) => (presentation, value) switch
    {
        (ObservablePresentation.Hex, long hex) => $"0x{(uint)hex:X8}",
        (ObservablePresentation.Fourcc, long fourcc) => FourccDisplay((uint)fourcc),
        _ => null
    };

    private static string FourccDisplay(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    /// Converts a value yielded by an observable, or computed directly by a generator, to JSON.
    /// </summary>
    /// <remarks>
    /// Every whole number widens to <see cref="long"/>, one canonical .NET type - a fresh,
    /// still-in-memory <c>JsonValue&lt;int&gt;</c> and a <c>JsonValue&lt;JsonElement&gt;</c> re-read
    /// from a cache hit disagree on which narrower type reading it back succeeds as, which would
    /// otherwise mix two differently-boxed copies of the same number in one reduce pass and break
    /// comparison. It is signed because some observed fields are: <c>ADBG.alignment</c> stores -1.
    /// </remarks>
    public static JsonValue ToJsonValue(object value) => value switch
    {
        long l => JsonValue.Create(l),
        int i => JsonValue.Create((long)i),
        uint u => JsonValue.Create((long)u),
        bool b => JsonValue.Create(b),
        string s => JsonValue.Create(s),
        _ => throw new NotSupportedException($"Observable values of type '{value.GetType()}' aren't supported yet.")
    };

    private static object FromJsonValue(JsonValue value)
    {
        if (value.TryGetValue(out long l)) return l;
        if (value.TryGetValue(out bool b)) return b;
        if (value.TryGetValue(out string? s)) return s!;
        throw new NotSupportedException($"Unsupported cached observation: {value}");
    }

    private sealed class ValueComparer : IComparer<object>
    {
        public static readonly ValueComparer Instance = new();

        public int Compare(object? x, object? y) => (x, y) switch
        {
            (long a, long b) => a.CompareTo(b),
            (bool a, bool b) => a.CompareTo(b),
            (string a, string b) => string.CompareOrdinal(a, b),
            _ => throw new NotSupportedException($"Can't compare '{x}' and '{y}'.")
        };
    }
}

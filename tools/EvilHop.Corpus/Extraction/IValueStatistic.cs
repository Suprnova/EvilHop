using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Extraction;

/// <summary>
/// A running statistic a <see cref="FieldKind"/> degrades to once a field exceeds the cardinality
/// cap - e.g. a <c>min</c>/<c>max</c> range or a <c>minLength</c>/<c>maxLength</c> range. Owns both
/// its own accumulation and its own emission, so a new kind never needs a matching branch anywhere
/// else.
/// </summary>
internal interface IValueStatistic
{
    /// <summary>Folds one observed value into the running statistic.</summary>
    void Observe(object? value);

    /// <summary>Writes this statistic's fields onto <paramref name="target"/>, omitting any that never observed a value.</summary>
    void WriteTo(JsonObject target);
}

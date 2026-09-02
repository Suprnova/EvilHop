using EvilHop.Assets;
using EvilHop.Blocks;

namespace EvilHop.Validation;

/// <summary>
/// Where within an archive an <see cref="Observable"/>'s value lives.
/// </summary>
public enum ObservableScope
{
    /// <summary>The observable is a fact about the archive as a whole.</summary>
    Archive,

    /// <summary>The observable is a fact about a single <see cref="Blocks.Block"/>.</summary>
    Block,

    /// <summary>The observable is a fact about a single asset.</summary>
    Asset,

    /// <summary>The observable is a fact about a single layer.</summary>
    Layer,

    /// <summary>The observable is a fact about a single link.</summary>
    Link
}

/// <summary>
/// How an <see cref="Observable"/>'s distinct values are recorded.
/// </summary>
public enum ObservableCardinality
{
    /// <summary>Every distinct observed value is recorded, with a count.</summary>
    Enumerated,

    /// <summary>Only <c>{min, max, count, distinct}</c> is recorded, never the values themselves.</summary>
    Summarized,

    /// <summary>Only the bitwise OR of every observed value is recorded.</summary>
    Bitmask
}

/// <summary>
/// How an <see cref="Observable"/>'s recorded values should be rendered for a human reading a diff.
/// </summary>
public enum ObservablePresentation
{
    /// <summary>A decimal quantity.</summary>
    Number,

    /// <summary>A bit pattern or packed code, rendered in hexadecimal.</summary>
    Hex,

    /// <summary>A four-character code, rendered as ASCII.</summary>
    Fourcc,

    /// <summary>Free text.</summary>
    Text,

    /// <summary>Raw bytes.</summary>
    Bytes
}

/// <summary>
/// What kind of fact an <see cref="Observable"/> records.
/// </summary>
public enum ObservableKind
{
    /// <summary>A field's own value - what <c>blockFields</c> records.</summary>
    FieldValue,

    /// <summary>A fact about the block tree's shape rather than a field's value - what <c>structure</c> records.</summary>
    Structural
}

/// <summary>
/// The axis an <see cref="Observable"/>'s occurrences are partitioned along before being recorded.
/// </summary>
/// <remarks>
/// Orthogonal to <see cref="ObservableCardinality"/>: cardinality says how one set of distinct
/// values is recorded, grouping says how many such sets there are and what separates them.
/// </remarks>
public enum ObservableGrouping
{
    /// <summary>Every occurrence is recorded in one set, whatever subject produced it.</summary>
    None,

    /// <summary>
    /// Occurrences are partitioned by the raw <see cref="uint"/> of the observed asset's
    /// <see cref="Assets.Asset.Type"/>, so the record answers "what does this field hold for assets
    /// of each type" rather than "what does this field hold anywhere."
    /// </summary>
    AssetType
}

/// <summary>
/// Declares that an enum's underlying values are four-character codes, so an observable inferred
/// from a property of this type renders as <see cref="ObservablePresentation.Fourcc"/> rather than
/// <see cref="ObservablePresentation.Hex"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
internal sealed class FourccAttribute : Attribute;

/// <summary>
/// The subject an <see cref="Observable"/> reads its value from. A closed hierarchy, one case per
/// <see cref="ObservableScope"/> the mapping stage currently knows how to read.
/// </summary>
public abstract record ObservationSource;

/// <summary>
/// An <see cref="ObservationSource"/> wrapping the <see cref="Blocks.Block"/> an
/// <see cref="ObservableScope.Block"/> observable reads from.
/// </summary>
/// <param name="Block">The block being observed.</param>
public sealed record BlockObservationSource(Block Block) : ObservationSource;

/// <summary>
/// An <see cref="ObservationSource"/> wrapping the <see cref="Assets.Asset"/> an
/// <see cref="ObservableScope.Asset"/> observable reads from.
/// </summary>
/// <param name="Asset">The asset being observed.</param>
public sealed record AssetObservationSource(Asset Asset) : ObservationSource;

/// <summary>
/// One value an <see cref="Observable"/> yielded for one subject, with the key it is partitioned
/// under.
/// </summary>
/// <param name="ObservableId">The identifier of the observable that yielded the value.</param>
/// <param name="Value">The yielded value, always a primitive.</param>
/// <param name="GroupKey">
/// The raw key this occurrence is partitioned under, or <see langword="null"/> when the observable's
/// <see cref="Observable.Grouping"/> is <see cref="ObservableGrouping.None"/>.
/// </param>
public readonly record struct Observation(string ObservableId, object Value, uint? GroupKey);

/// <summary>
/// A named, primitive-valued projection over an archive. The single place that declares where a
/// value lives, read by both the runtime validator and the corpus recorder.
/// </summary>
/// <param name="Id">
/// The observable's stable identifier, such as <c>"PVER.subVersion"</c> or
/// <c>"PLAT.platformId+platformName"</c>.
/// </param>
/// <param name="Scope">Where within an archive this observable's value lives.</param>
/// <param name="Cardinality">How this observable's distinct values are recorded.</param>
/// <param name="Presentation">How this observable's recorded values should be rendered.</param>
/// <param name="Select">
/// Projects <paramref name="Select"/>'s <see cref="ObservationSource"/> argument to this observable's
/// value, yielding nothing if the source doesn't carry one. Yields only primitives - <see cref="uint"/>,
/// <see cref="int"/>, <see cref="string"/>, <see cref="bool"/>, <c>byte[]</c>, or a tuple of those -
/// never a library enum or record.
/// </param>
/// <param name="Kind">
/// What kind of fact this observable records. Defaults to <see cref="ObservableKind.FieldValue"/>,
/// which every attribute-declared observable is.
/// </param>
/// <param name="Grouping">
/// The axis this observable's occurrences are partitioned along. Defaults to
/// <see cref="ObservableGrouping.None"/>, which records one set for the whole corpus.
/// </param>
/// <param name="KeyPresentation">
/// How a group's key should be rendered, derived from <paramref name="Grouping"/>. Non-null exactly
/// when <paramref name="Grouping"/> isn't <see cref="ObservableGrouping.None"/>.
/// </param>
public sealed record Observable(
    string Id,
    ObservableScope Scope,
    ObservableCardinality Cardinality,
    ObservablePresentation Presentation,
    Func<ObservationSource, IEnumerable<object>> Select,
    ObservableKind Kind = ObservableKind.FieldValue,
    ObservableGrouping Grouping = ObservableGrouping.None,
    ObservablePresentation? KeyPresentation = null);

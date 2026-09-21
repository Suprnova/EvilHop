using EvilHop.Common;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// One <see cref="ScriptAsset"/> event - sends <see cref="ParamEvent"/> to <see cref="WidgetId"/>,
/// alongside <see cref="Param"/>, once <see cref="Time"/> seconds have passed since the script
/// started running.
/// </summary>
/// <remarks>
/// Shaped like a <see cref="Link"/>, but not one: it has no source event of its own (elapsed time
/// reaching <see cref="Time"/> is what triggers it) and no <see cref="Link.CheckAssetId"/>.
/// </remarks>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Nothing compares ScriptEvents by value; Param holds reference-equality-only Parameters, so a real implementation would be misleading.")]
public struct ScriptEvent
{
    /// <summary>
    /// Initializes a new instance of <see cref="ScriptEvent"/> with four zeroed <see cref="Param"/>
    /// slots.
    /// </summary>
    public ScriptEvent() { }

    /// <summary>
    /// The time, in seconds, this event fires at, relative to when the script starts running.
    /// </summary>
    public float Time { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> this event sends <see cref="ParamEvent"/> to.
    /// </summary>
    public AssetId WidgetId { get; set; }

    /// <summary>
    /// The event sent to <see cref="WidgetId"/>.
    /// </summary>
    public uint ParamEvent { get; set; }

    /// <summary>
    /// The four parameter slots passed alongside <see cref="ParamEvent"/>. Always exactly 4
    /// elements, in order.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 4.</exception>
    public ImmutableArray<Parameter> Param
    {
        readonly get;
        set => field = value.Length == 4
            ? value
            : throw new ArgumentException($"{nameof(Param)} must contain exactly 4 elements.", nameof(value));
    } = ZeroedParams();

    /// <summary>
    /// A supplemental <see cref="AssetId"/> parameter for <see cref="ParamEvent"/>.
    /// </summary>
    public AssetId ParamWidgetId { get; set; }

    /// <summary>
    /// Whether this event fires at all.
    /// </summary>
    /// <remarks>
    /// Only present in <see cref="GameVersion.ROTU"/> and <see cref="GameVersion.Ratatouille"/>.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    private static ImmutableArray<Parameter> ZeroedParams() =>
        [new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4])];
}

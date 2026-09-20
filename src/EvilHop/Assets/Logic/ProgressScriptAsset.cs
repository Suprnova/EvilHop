using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// Fires events for other assets as a percentage - such as an animation's progress, or a cutscene's
/// playback position - walks forward or backward past each <see cref="ProgressScriptEvent.Percent"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PGRS">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class ProgressScriptAsset() : BaseAsset(AssetType.ProgressScript, baseType: 0x75), IPhysicalProgressScriptAsset
{
    /// <summary>
    /// This script's events, in ascending <see cref="ProgressScriptEvent.Percent"/> order.
    /// </summary>
    public Collection<ProgressScriptEvent> Events { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalProgressScriptAsset Physical => this;

    private uint? _overriddenEventCount;
    uint IPhysicalProgressScriptAsset.EventCount
    {
        get => _overriddenEventCount ?? (uint)Events.Count;
        set => _overriddenEventCount = value == (uint)Events.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.ProgressScript"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };

    internal static ProgressScriptAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new ProgressScriptAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        uint eventCount = reader.ReadUInt32();
        for (uint i = 0; i < eventCount; i++)
        {
            asset.Events.Add(new ProgressScriptEvent
            {
                Percent = reader.ReadSingle(),
                Flags = (ProgressScriptEventFlags)reader.ReadInt32(),
                WidgetId = reader.ReadAssetId(),
                ParamEvent = reader.ReadUInt32(),
                Param =
                [
                    new RawParameter(reader.ReadBytes(4)),
                    new RawParameter(reader.ReadBytes(4)),
                    new RawParameter(reader.ReadBytes(4)),
                    new RawParameter(reader.ReadBytes(4)),
                ],
                ParamWidgetId = reader.ReadAssetId(),
            });
        }
        asset.Physical.EventCount = (uint)asset.Events.Count;

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ProgressScriptAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.EventCount);
        foreach (var evt in asset.Events)
        {
            writer.Write(evt.Percent);
            writer.Write((int)evt.Flags);
            writer.Write(evt.WidgetId);
            writer.Write(evt.ParamEvent);
            foreach (var param in evt.Param) param.WriteTo(writer);
            writer.Write(evt.ParamWidgetId);
        }

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="ProgressScriptAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalProgressScriptAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// The number of <see cref="ProgressScriptAsset.Events"/> stored for this asset, read directly
    /// from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="ProgressScriptAsset.Events"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint EventCount { get; set; }
}

/// <summary>
/// One <see cref="ProgressScriptAsset"/> event - sends <see cref="ParamEvent"/> to
/// <see cref="WidgetId"/>, alongside <see cref="Param"/>, once playback reaches <see cref="Percent"/>.
/// </summary>
/// <remarks>
/// Shaped like a <see cref="Link"/>, but not one: it has no source event of its own (playback past
/// <see cref="Percent"/> is what triggers it) and no <see cref="Link.CheckAssetId"/>.
/// </remarks>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Nothing compares ProgressScriptEvents by value; Param holds reference-equality-only Parameters, so a real implementation would be misleading.")]
public struct ProgressScriptEvent
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProgressScriptEvent"/> with four zeroed
    /// <see cref="Param"/> slots.
    /// </summary>
    public ProgressScriptEvent() { }

    /// <summary>
    /// The percentage, from 0 to 100, of playback this event fires at.
    /// </summary>
    public float Percent { get; set; }

    /// <summary>
    /// This event's flags.
    /// </summary>
    public ProgressScriptEventFlags Flags { get; set; }

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

    private static ImmutableArray<Parameter> ZeroedParams() =>
        [new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4])];
}

/// <summary>
/// Flags controlling playback and dispatch behavior for a progress script event.
/// </summary>
[Flags]
public enum ProgressScriptEventFlags
{
    /// <summary>No flags are set.</summary>
    None = 0,

    /// <summary>
    /// This event fires only once per <see cref="ProgressScriptAsset"/> reset, rather than every time
    /// playback crosses <see cref="ProgressScriptEvent.Percent"/>.
    /// </summary>
    Once = 1 << 0,
}

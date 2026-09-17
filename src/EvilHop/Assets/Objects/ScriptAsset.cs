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
/// A timeline of <see cref="Events"/>, each firing at a fixed time after the script starts running.
/// Send it <c>Run</c> to start, <c>WaitForInput</c> to pause until the player presses a button, and
/// <c>Reset</c> or <c>ScriptReset</c> before running it again; it sends itself <c>Expired</c> once
/// every event has fired.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SCRP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class ScriptAsset() : BaseAsset(AssetType.Script), IPhysicalScriptAsset
{
    /// <summary>
    /// A multiplier applied to this script's own playback speed. Always 1 in every archive checked
    /// so far, from <see cref="GameVersion.TSSM"/> onward.
    /// </summary>
    /// <remarks>
    /// In <see cref="GameVersion.BFBB"/>, per decompiled source, the same on-disk value is instead
    /// read as <see cref="ScriptEvent.Time"/>'s starting offset, in seconds, when the script starts running.
    /// </remarks>
    /// TODO: N100F has no available decompiled source or corpus exemplar to confirm which reading it uses.
    public float ScaleFactor { get; set; }

    /// <summary>
    /// Whether this script runs again from the start once it finishes, rather than staying expired.
    /// </summary>
    /// <remarks>
    /// Not present in <see cref="GameVersion.N100F"/> or <see cref="GameVersion.BFBB"/>.
    /// </remarks>
    public bool Loop { get; set; }

    /// <summary>
    /// This script's events, in ascending <see cref="ScriptEvent.Time"/> order.
    /// </summary>
    public Collection<ScriptEvent> Events { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalScriptAsset Physical => this;

    private uint? _overriddenEventCount;
    uint IPhysicalScriptAsset.EventCount
    {
        get => _overriddenEventCount ?? (uint)Events.Count;
        set => _overriddenEventCount = value == (uint)Events.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Script"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static ScriptAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ScriptAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.ScaleFactor = reader.ReadSingle();
        uint eventCount = reader.ReadUInt32();

        // TODO: N100F is assumed to match BFBB here; unconfirmed against source or real bytes.
        bool hasLoop = profile.Game is not (GameVersion.N100F or GameVersion.BFBB);
        if (hasLoop)
        {
            asset.Loop = reader.ReadByte() != 0;
            reader.ReadBytes(3); // padding, always zero
        }

        bool hasEnabled = profile.Game is GameVersion.ROTU or GameVersion.Ratatouille;
        for (uint i = 0; i < eventCount; i++)
        {
            var evt = new ScriptEvent
            {
                Time = reader.ReadSingle(),
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
            };
            if (hasEnabled) evt.Enabled = reader.ReadInt32() != 0;
            asset.Events.Add(evt);
        }
        asset.Physical.EventCount = (uint)asset.Events.Count;

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ScriptAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.ScaleFactor);
        writer.Write(asset.Physical.EventCount);

        bool hasLoop = profile.Game is not (GameVersion.N100F or GameVersion.BFBB);
        if (hasLoop)
        {
            writer.Write((byte)(asset.Loop ? 1 : 0));
            writer.Write(new byte[3]); // padding
        }

        bool hasEnabled = profile.Game is GameVersion.ROTU or GameVersion.Ratatouille;
        foreach (var evt in asset.Events)
        {
            writer.Write(evt.Time);
            writer.Write(evt.WidgetId);
            writer.Write(evt.ParamEvent);
            foreach (var param in evt.Param) param.WriteTo(writer);
            writer.Write(evt.ParamWidgetId);
            if (hasEnabled) writer.Write(evt.Enabled ? 1 : 0);
        }

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="ScriptAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalScriptAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// The number of <see cref="ScriptAsset.Events"/> stored for this asset, read directly from its
    /// leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="ScriptAsset.Events"/>.Count exist, this field wins during
    /// serialization.
    /// </remarks>
    uint EventCount { get; set; }
}

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
    /// Only present in <see cref="GameVersion.ROTU"/> and <see cref="GameVersion.Ratatouille"/>; always
    /// <see langword="true"/> in every archive checked so far.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    private static ImmutableArray<Parameter> ZeroedParams() =>
        [new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4])];
}

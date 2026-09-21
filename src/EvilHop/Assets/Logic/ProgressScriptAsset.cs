using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Fires events for other assets as a percentage - such as an animation's progress, or a cutscene's
/// playback position - walks forward or backward past each <see cref="ProgressScriptEvent.Percent"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PGRS">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class ProgressScriptAsset() : BaseAsset(AssetType.ProgressScript, baseType: 0x75), Physical.IProgressScriptAsset
{
    /// <summary>
    /// This script's events, in ascending <see cref="ProgressScriptEvent.Percent"/> order.
    /// </summary>
    public Collection<ProgressScriptEvent> Events { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IProgressScriptAsset Physical => this;

    private uint? _overriddenEventCount;
    uint Physical.IProgressScriptAsset.EventCount
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

    internal static ProgressScriptAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ProgressScriptAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        uint eventCount = reader.ReadUInt32();
        for (uint i = 0; i < eventCount; i++)
            asset.Events.Add(ProgressScriptEvent.Read(reader, profile));
        asset.Physical.EventCount = (uint)asset.Events.Count;

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ProgressScriptAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.EventCount);
        foreach (var evt in asset.Events)
            ProgressScriptEvent.Write(evt, writer, profile);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="ProgressScriptAsset"/>'s underlying values.
    /// </summary>
    public interface IProgressScriptAsset : IBaseAsset
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
}

using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
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
public sealed partial class ScriptAsset() : BaseAsset(AssetType.Script, baseType: 0x2A), Physical.IScriptAsset
{
    /// <summary>
    /// The starting time offset in seconds, or playback speed multiplier.
    /// </summary>
    public float ScaleFactor { get; set; }

    /// <summary>
    /// Whether this script runs again from the start once it finishes, rather than staying expired.
    /// </summary>
    /// <remarks>
    /// Not present in <see cref="GameVersion.N100F"/> or <see cref="GameVersion.BFBB"/>.
    /// </remarks>
    public bool Loop
    {
        get => Physical.Loop != 0;
        set => Physical.Loop = (byte)(value ? 1 : 0);
    }

    /// <summary>
    /// This script's events, in ascending <see cref="ScriptEvent.Time"/> order.
    /// </summary>
    public Collection<ScriptEvent> Events { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IScriptAsset Physical => this;

    private byte _loop;
    byte Physical.IScriptAsset.Loop { get => _loop; set => _loop = value; }

    private uint? _overriddenEventCount;
    uint Physical.IScriptAsset.EventCount
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

        bool hasLoop = profile.Game is not (GameVersion.N100F or GameVersion.BFBB);
        if (hasLoop)
        {
            asset.Physical.Loop = reader.ReadByte();
            reader.ReadBytes(3); // padding, always zero
        }

        for (uint i = 0; i < eventCount; i++)
            asset.Events.Add(ScriptEvent.Read(reader, profile));
        asset.Physical.EventCount = (uint)asset.Events.Count;

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
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
            writer.Write(asset.Physical.Loop);
            writer.Write(new byte[3]); // padding
        }

        foreach (var evt in asset.Events)
            ScriptEvent.Write(evt, writer, profile);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="ScriptAsset"/>'s underlying values.
    /// </summary>
    public interface IScriptAsset : IBaseAsset
    {
        /// <summary>
        /// Whether this script runs again from the start once it finishes, as stored on disk.
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Matches on-disk property and logical asset member name.")]
        byte Loop { get; set; }

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
}

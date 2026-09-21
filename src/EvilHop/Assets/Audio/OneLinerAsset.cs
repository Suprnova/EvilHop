using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A table of voice-line triggers, each playing a <see cref="AssetType.SoundGroup"/> in response to a
/// game event, subject to a probability and replay cooldown.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/ONEL">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class OneLinerAsset() : Asset(AssetType.OneLiner), IPhysicalOneLinerAsset
{
    /// <summary>The table's entries, each triggered independently.</summary>
    public Collection<OneLinerEntry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalOneLinerAsset Physical => this;

    private uint? _overriddenEntryCount;
    uint IPhysicalOneLinerAsset.EntryCount
    {
        get => _overriddenEntryCount ?? (uint)Entries.Count;
        set => _overriddenEntryCount = value == (uint)Entries.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.OneLiner"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };

    internal static OneLinerAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new OneLinerAsset();
        AssetFields.Populate(asset, header, debug);

        uint entryCount = reader.ReadUInt32();
        for (int i = 0; i < entryCount; i++)
            asset.Entries.Add(OneLinerEntry.Read(reader, profile));

        asset.Physical.EntryCount = (uint)asset.Entries.Count;
        // TODO: Partial implementation - trailing 67-byte trailer is not modeled
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(OneLinerAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.EntryCount);

        foreach (var entry in asset.Entries)
            OneLinerEntry.Write(entry, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="OneLinerAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalOneLinerAsset : IPhysicalAsset
{
    /// <summary>
    /// The number of <see cref="OneLinerAsset.Entries"/> stored for this asset, read directly from
    /// its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="OneLinerAsset.Entries"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint EntryCount { get; set; }
}

/// <summary>
/// Identifies the type of criteria for which this <see cref="OneLinerEntry"/> will play.
/// </summary>
public enum OneLinerPlayerType
{
    /// <summary>This entry has no gating condition - it is always eligible to play.</summary>
    Always = 0,

    /// <summary>Unknown.</summary>
    Counter = 1,

    /// <summary>Unknown.</summary>
    Checker = 2,

    /// <summary>
    /// This entry's eligibility is gated by evaluating <see cref="OneLinerEntry.FirstParam"/> and
    /// <see cref="OneLinerEntry.SecondParam"/> against an unconfirmed condition.
    /// </summary>
    Tester = 3,
}

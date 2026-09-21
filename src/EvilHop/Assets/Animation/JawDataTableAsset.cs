using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Maps one or more <see cref="AssetType.Sound"/>/<see cref="AssetType.StreamingSound"/> assets to an
/// array of mouth open/close positions, generated from each sound's waveform, driving lip-sync for the
/// player or an NPC while the sound plays.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/JAW">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class JawDataTableAsset() : Asset(AssetType.JawDataTable), Physical.IJawDataTableAsset
{
    /// <summary>
    /// The table's entries, each holding one sound's jaw data.
    /// </summary>
    public Collection<JawDataTableEntry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IJawDataTableAsset Physical => this;

    private int? _overriddenCount;
    int Physical.IJawDataTableAsset.Count
    {
        get => _overriddenCount ?? Entries.Count;
        set => _overriddenCount = value == Entries.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.JawDataTable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.ROTU,
    };

    internal static JawDataTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new JawDataTableAsset();
        AssetFields.Populate(asset, header, debug);

        int count = reader.ReadInt32();
        var soundIds = new AssetId[count];
        for (int i = 0; i < count; i++)
        {
            soundIds[i] = reader.ReadAssetId();
            reader.ReadInt32(); // dataStart, derived from every prior entry's own (aligned) size
            reader.ReadInt32(); // dataLength, derived from this entry's own jaw data length
        }

        foreach (var soundId in soundIds)
            asset.Entries.Add(JawDataTableEntry.Read(reader, soundId, profile));

        asset.Physical.Count = asset.Entries.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(JawDataTableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        bool hasUnknownField = profile.Game is GameVersion.ROTU;

        writer.Write(asset.Physical.Count);
        int dataStart = 0;
        foreach (var entry in asset.Entries)
        {
            int dataLength = (hasUnknownField ? 8 : 4) + entry.JawData.Count;
            writer.Write(entry.SoundId);
            writer.Write(dataStart);
            writer.Write(dataLength);
            dataStart = Align4(dataStart + dataLength);
        }

        foreach (var entry in asset.Entries) JawDataTableEntry.Write(entry, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }

    private static int Align4(int value) => (value + 3) & ~3;
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="JawDataTableAsset"/>'s underlying values.
    /// </summary>
    public interface IJawDataTableAsset : IAsset
    {
        /// <summary>
        /// The number of <see cref="JawDataTableAsset.Entries"/> stored for this asset, read directly
        /// from its leading count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="JawDataTableAsset.Entries"/>.Count exist, this field wins
        /// during serialization.
        /// </remarks>
        int Count { get; set; }
    }
}

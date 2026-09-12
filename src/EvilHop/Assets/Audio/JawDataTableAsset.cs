using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Buffers.Binary;
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
public sealed class JawDataTableAsset() : Asset(AssetType.JawDataTable), IPhysicalJawDataTableAsset
{
    /// <summary>
    /// The table's entries, each holding one sound's jaw data.
    /// </summary>
    public Collection<JawDataTableEntry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalJawDataTableAsset Physical => this;

    private int? _overriddenCount;
    int IPhysicalJawDataTableAsset.Count
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

        bool hasUnknownField = profile.Game is GameVersion.ROTU;
        foreach (var soundId in soundIds)
        {
            int length = hasUnknownField ? reader.ReadInt32() : BinaryPrimitives.ReadInt32LittleEndian(reader.ReadBytes(4));
            uint unknown = hasUnknownField ? reader.ReadUInt32() : 0;
            byte[] jawData = reader.ReadBytes(length);
            SkipPadding(reader, EntrySize(hasUnknownField, length));

            var entry = new JawDataTableEntry { SoundId = soundId, Unknown = unknown };
            foreach (byte b in jawData) entry.JawData.Add(b);
            asset.Entries.Add(entry);
        }

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
            int dataLength = EntrySize(hasUnknownField, entry.JawData.Count);
            writer.Write(entry.SoundId);
            writer.Write(dataStart);
            writer.Write(dataLength);
            dataStart = Align4(dataStart + dataLength);
        }

        foreach (var entry in asset.Entries)
        {
            if (hasUnknownField)
            {
                writer.Write(entry.JawData.Count);
                writer.Write(entry.Unknown);
            }
            else
            {
                WriteLengthLittleEndian(writer, entry.JawData.Count);
            }
            writer.Write(entry.JawData.ToArray());
            WritePadding(writer, EntrySize(hasUnknownField, entry.JawData.Count));
        }

        writer.Write(asset.GetUnparsedTail());
    }

    private static int EntrySize(bool hasUnknownField, int jawDataLength) => (hasUnknownField ? 8 : 4) + jawDataLength;

    private static int Align4(int value) => (value + 3) & ~3;

    private static void SkipPadding(EndianReader reader, int entrySize)
    {
        int padding = Align4(entrySize) - entrySize;
        if (padding > 0) reader.ReadBytes(padding); // 4-byte alignment padding, always zero
    }

    private static void WritePadding(EndianWriter writer, int entrySize)
    {
        int padding = Align4(entrySize) - entrySize;
        for (int i = 0; i < padding; i++) writer.Write((byte)0);
    }

    private static void WriteLengthLittleEndian(EndianWriter writer, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        writer.Write(bytes);
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="JawDataTableAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalJawDataTableAsset : IPhysicalAsset
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

/// <summary>
/// One <see cref="JawDataTableAsset"/> entry: a sound and the mouth open/close positions to animate
/// while it plays.
/// </summary>
public sealed class JawDataTableEntry
{
    /// <summary>
    /// The <see cref="AssetType.Sound"/> or <see cref="AssetType.StreamingSound"/> this jaw data
    /// animates alongside.
    /// </summary>
    public AssetId SoundId { get; set; }

    /// <summary>
    /// The speaker's mouth position at each point in time <see cref="SoundId"/> plays, one byte per
    /// frame from 0 (fully closed) to 255 (fully open).
    /// </summary>
    public Collection<byte> JawData { get; } = [];

    /// <summary>
    /// Unknown. Present only in <see cref="GameVersion.ROTU"/>.
    /// </summary>
    public uint Unknown { get; set; }
}

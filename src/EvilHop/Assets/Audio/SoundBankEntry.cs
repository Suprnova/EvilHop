using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// Metadata for one sound stored in a <see cref="SoundInfoAsset"/>'s FSB3-embedded
/// <see cref="SoundInfoAsset.SoundBanks"/>.
/// </summary>
public sealed class SoundBankEntry
{
    /// <summary>
    /// The <see cref="AssetType.Sound"/> or <see cref="AssetType.StreamingSound"/> asset this entry
    /// describes.
    /// </summary>
    public AssetId SoundAssetId { get; set; }

    /// <summary>Whether the sound loops, and whether it is streamed rather than RAM-resident.</summary>
    public SoundBankEntryFlags Flags { get; set; }

    /// <summary>The zero-based index of the sound within its <see cref="SoundBankIndex"/> bank.</summary>
    public byte SampleIndex { get; set; }

    /// <summary>
    /// The zero-based index, into <see cref="SoundInfoAsset.SoundBanks"/>, of the bank containing
    /// this sound.
    /// </summary>
    public byte SoundBankIndex { get; set; }

    /// <summary>Unknown.</summary>
    public byte SoundInfoIndex { get; set; }

    internal static SoundBankEntry Read(EndianReader reader, FormatProfile _) => new()
    {
        SoundAssetId = reader.ReadAssetId(),
        Flags = (SoundBankEntryFlags)reader.ReadByte(),
        SampleIndex = reader.ReadByte(),
        SoundBankIndex = reader.ReadByte(),
        SoundInfoIndex = reader.ReadByte(),
    };

    internal static void Write(SoundBankEntry entry, EndianWriter writer, FormatProfile _)
    {
        writer.Write(entry.SoundAssetId);
        writer.Write((byte)entry.Flags);
        writer.Write(entry.SampleIndex);
        writer.Write(entry.SoundBankIndex);
        writer.Write(entry.SoundInfoIndex);
    }
}

/// <summary>Flags for one <see cref="SoundBankEntry"/>.</summary>
[Flags]
public enum SoundBankEntryFlags : byte
{
    /// <summary>A RAM-resident, non-looping sound.</summary>
    None = 0,

    /// <summary>The sound loops.</summary>
    Looped = 1 << 0,

    /// <summary>The sound is streamed from disk rather than RAM-resident.</summary>
    Streaming = 1 << 1,
}

using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A table of interchangeable <see cref="AssetType.Sound"/>s or <see cref="AssetType.StreamingSound"/>s
/// - referenced by an <see cref="AssetType.SoundEffect"/>, <see cref="AssetType.OneLiner"/> entry, or
/// similar - one of which is chosen to play at a time.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SGRP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SoundGroupAsset() : BaseAsset(AssetType.SoundGroup), IPhysicalSoundGroupAsset
{
    /// <summary>This group's entries, one of which is chosen to play at a time.</summary>
    public Collection<SoundGroupEntry> Entries { get; } = [];

    /// <summary>The maximum number of plays permitted for this group, passed directly to the sound engine.</summary>
    /// TODO: whether this counts concurrent voices or total plays over the group's lifetime is unconfirmed.
    public sbyte MaxPlays { get; set; }

    /// <summary>This group's playback priority, passed directly to the sound engine.</summary>
    public byte Priority { get; set; }

    /// <summary>The radius inside which this group plays at maximum volume.</summary>
    public float InnerRadius { get; set; }

    /// <summary>The radius beyond which this group stops playing.</summary>
    public float OuterRadius { get; set; }

    /// <summary>
    /// Whether every instance of this group anywhere in the scene shares one play state, so only the
    /// nearest or highest-priority instance ever actually plays - the group-level counterpart to
    /// <see cref="SoundFXAsset.IsEnvironmental"/>.
    /// </summary>
    public bool IsEnvironmentalStream
    {
        get => (Physical.SoundGroupFlags & 0x2) != 0;
        set => Physical.SoundGroupFlags = (byte)(value ? Physical.SoundGroupFlags | 0x2 : Physical.SoundGroupFlags & ~0x2);
    }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalSoundGroupAsset Physical => this;

    private byte? _overriddenEntryCount;
    byte IPhysicalSoundGroupAsset.EntryCount
    {
        get => _overriddenEntryCount ?? (byte)Entries.Count;
        set => _overriddenEntryCount = value == (byte)Entries.Count ? null : value;
    }

    private uint _playedMask;
    uint IPhysicalSoundGroupAsset.PlayedMask { get => _playedMask; set => _playedMask = value; }

    private byte _setBits;
    byte IPhysicalSoundGroupAsset.SetBits { get => _setBits; set => _setBits = value; }

    private byte _soundGroupFlags;
    byte IPhysicalSoundGroupAsset.SoundGroupFlags { get => _soundGroupFlags; set => _soundGroupFlags = value; }

    private byte _soundCategory;
    byte IPhysicalSoundGroupAsset.SoundCategory { get => _soundCategory; set => _soundCategory = value; }

    private byte _playRule;
    byte IPhysicalSoundGroupAsset.PlayRule { get => _playRule; set => _playRule = value; }

    private byte _infoPad0;
    byte IPhysicalSoundGroupAsset.InfoPad0 { get => _infoPad0; set => _infoPad0 = value; }

    private uint _groupNamePointer;
    uint IPhysicalSoundGroupAsset.GroupNamePointer { get => _groupNamePointer; set => _groupNamePointer = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SoundGroup"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static SoundGroupAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new SoundGroupAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.PlayedMask = reader.ReadUInt32();
        byte entryCount = reader.ReadByte();
        asset.Physical.SetBits = reader.ReadByte();
        asset.MaxPlays = (sbyte)reader.ReadByte();
        asset.Priority = reader.ReadByte();
        asset.Physical.SoundGroupFlags = reader.ReadByte();
        asset.Physical.SoundCategory = reader.ReadByte();
        asset.Physical.PlayRule = reader.ReadByte();
        asset.Physical.InfoPad0 = reader.ReadByte();
        asset.InnerRadius = reader.ReadSingle();
        asset.OuterRadius = reader.ReadSingle();
        asset.Physical.GroupNamePointer = reader.ReadUInt32();

        for (int i = 0; i < entryCount; i++)
        {
            asset.Entries.Add(new SoundGroupEntry
            {
                SoundId = reader.ReadAssetId(),
                Volume = reader.ReadSingle(),
                MinPitchMultiplier = reader.ReadSingle(),
                MaxPitchMultiplier = reader.ReadSingle(),
            });
        }
        asset.Physical.EntryCount = (byte)asset.Entries.Count;

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SoundGroupAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.PlayedMask);
        writer.Write(asset.Physical.EntryCount);
        writer.Write(asset.Physical.SetBits);
        writer.Write((byte)asset.MaxPlays);
        writer.Write(asset.Priority);
        writer.Write(asset.Physical.SoundGroupFlags);
        writer.Write(asset.Physical.SoundCategory);
        writer.Write(asset.Physical.PlayRule);
        writer.Write(asset.Physical.InfoPad0);
        writer.Write(asset.InnerRadius);
        writer.Write(asset.OuterRadius);
        writer.Write(asset.Physical.GroupNamePointer);

        foreach (var entry in asset.Entries)
        {
            writer.Write(entry.SoundId);
            writer.Write(entry.Volume);
            writer.Write(entry.MinPitchMultiplier);
            writer.Write(entry.MaxPitchMultiplier);
        }

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="SoundGroupAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalSoundGroupAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// The number of <see cref="SoundGroupAsset.Entries"/> stored for this asset, read directly from
    /// its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="SoundGroupAsset.Entries"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    byte EntryCount { get; set; }

    /// <summary>Unknown.</summary>
    /// <remarks>
    /// Per decompiled source, a runtime bitmask tracking which <see cref="SoundGroupAsset.Entries"/>
    /// have already played, used to avoid repeats. Every archive checked so far has this zero.
    /// </remarks>
    uint PlayedMask { get; set; }

    /// <summary>Unknown.</summary>
    byte SetBits { get; set; }

    /// <summary>The group's raw flags byte, read directly from disk.</summary>
    /// <remarks>
    /// Bit 0x2 is exposed logically as <see cref="SoundGroupAsset.IsEnvironmentalStream"/>. The
    /// remaining bits have no confirmed meaning.
    /// </remarks>
    byte SoundGroupFlags { get; set; }

    /// <summary>Unknown.</summary>
    /// TODO: the wiki suggests this selects between hardcoded playback categories (e.g. dialogue vs.
    /// music), but no decompiled source confirms this specific field's meaning.
    byte SoundCategory { get; set; }

    /// <summary>Unknown.</summary>
    byte PlayRule { get; set; }

    /// <summary>Unknown.</summary>
    byte InfoPad0 { get; set; }

    /// <summary>Unknown.</summary>
    /// <remarks>
    /// Per decompiled source, a runtime pointer to this group's name string, resolved after loading.
    /// Every archive checked so far has this zero.
    /// </remarks>
    uint GroupNamePointer { get; set; }
}

/// <summary>
/// One <see cref="SoundGroupAsset"/> entry - <see cref="SoundId"/>, played at <see cref="Volume"/>
/// with a random pitch offset between <see cref="MinPitchMultiplier"/> and <see cref="MaxPitchMultiplier"/>.
/// </summary>
public sealed class SoundGroupEntry
{
    /// <summary>The <see cref="AssetType.Sound"/> or <see cref="AssetType.StreamingSound"/> this entry plays.</summary>
    public AssetId SoundId { get; set; }

    /// <summary>This entry's playback volume, from 0 to 1.</summary>
    public float Volume { get; set; }

    /// <summary>The minimum pitch offset applied when this entry plays, chosen at random up to <see cref="MaxPitchMultiplier"/>.</summary>
    public float MinPitchMultiplier { get; set; }

    /// <summary>The maximum pitch offset applied when this entry plays, chosen at random down to <see cref="MinPitchMultiplier"/>.</summary>
    public float MaxPitchMultiplier { get; set; }
}

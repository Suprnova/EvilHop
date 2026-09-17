using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A table defining information regarding dynamic pickups, keyed by type hash.
/// Usually has only one instance in the entire game (in <c>boot.hip</c>).
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/TPIK">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class PickupTypesAsset : BaseAsset, IPhysicalPickupTypesAsset
{
    /// <summary>
    /// Initializes a new instance of <see cref="PickupTypesAsset"/>.
    /// </summary>
    public PickupTypesAsset() : base(AssetType.PickupTypes)
    {
        _baseType = 0x00;
    }

    /// <summary>
    /// The table's format version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The pickup type entries in the table.
    /// </summary>
    public Collection<PickupTypeEntry> Entries { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalPickupTypesAsset Physical => this;

    private int? _overriddenRowCount;
    int IPhysicalPickupTypesAsset.RowCount
    {
        get => _overriddenRowCount ?? Entries.Count;
        set => _overriddenRowCount = value == Entries.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.PickupTypes"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static PickupTypesAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new PickupTypesAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Version = reader.ReadInt32();
        int rowCount = reader.ReadInt32();
        for (int i = 0; i < rowCount; i++)
        {
            asset.Entries.Add(new PickupTypeEntry
            {
                TypeHash = reader.ReadAssetId(),
                ModelId = reader.ReadAssetId(),
                PulseModelId = reader.ReadAssetId(),
                PulseTime = reader.ReadSingle(),
                PulseAddScale = reader.ReadSingle(),
                PulseMoveDown = reader.ReadSingle(),
                ColorMultiplier = reader.ReadRgb(),
                Color = reader.ReadUInt32(),
                FlyingSoundGroupId = reader.ReadAssetId(),
                UsedSoundGroupId = reader.ReadAssetId(),
                CantUseSoundGroupId = reader.ReadAssetId(),
                HealthGain = reader.ReadByte(),
                PowerGain = reader.ReadByte(),
                SaveFlag = reader.ReadByte(),
                Initialized = reader.ReadSByte(),
            });
        }

        asset.Physical.RowCount = rowCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PickupTypesAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Version);
        writer.Write(asset.Physical.RowCount);
        foreach (var entry in asset.Entries)
        {
            writer.Write(entry.TypeHash);
            writer.Write(entry.ModelId);
            writer.Write(entry.PulseModelId);
            writer.Write(entry.PulseTime);
            writer.Write(entry.PulseAddScale);
            writer.Write(entry.PulseMoveDown);
            writer.Write(entry.ColorMultiplier);
            writer.Write(entry.Color);
            writer.Write(entry.FlyingSoundGroupId);
            writer.Write(entry.UsedSoundGroupId);
            writer.Write(entry.CantUseSoundGroupId);
            writer.Write(entry.HealthGain);
            writer.Write(entry.PowerGain);
            writer.Write(entry.SaveFlag);
            writer.Write(entry.Initialized);
        }

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="PickupTypesAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalPickupTypesAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// The number of <see cref="PickupTypesAsset.Entries"/> stored for this asset, read directly
    /// from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="PickupTypesAsset.Entries"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    int RowCount { get; set; }
}

/// <summary>
/// One <see cref="PickupTypesAsset"/> entry, defining information for a single pickup type.
/// </summary>
public sealed class PickupTypeEntry
{
    /// <summary>
    /// The identifier or hash used by dynamic pickups to identify this pickup type.
    /// </summary>
    public AssetId TypeHash { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> used for this pickup.
    /// </summary>
    public AssetId ModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> used when the pickup pulses.
    /// </summary>
    public AssetId PulseModelId { get; set; }

    /// <summary>
    /// The duration or rate of the pickup's pulsing effect.
    /// </summary>
    public float PulseTime { get; set; }

    /// <summary>
    /// The additional scale applied during pulsing.
    /// </summary>
    public float PulseAddScale { get; set; }

    /// <summary>
    /// The vertical offset downwards applied during pulsing.
    /// </summary>
    public float PulseMoveDown { get; set; }

    /// <summary>
    /// The color multiplier or tint applied to the pickup's model.
    /// </summary>
    public Rgb ColorMultiplier { get; set; }

    /// <summary>
    /// The packed color of the pickup.
    /// </summary>
    public uint Color { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SoundGroup"/> played while the pickup flies towards the player.
    /// </summary>
    public AssetId FlyingSoundGroupId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SoundGroup"/> played when the pickup is collected.
    /// </summary>
    public AssetId UsedSoundGroupId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SoundGroup"/> played when the pickup cannot be collected.
    /// </summary>
    public AssetId CantUseSoundGroupId { get; set; }

    /// <summary>
    /// The amount of health restored when collected.
    /// </summary>
    public byte HealthGain { get; set; }

    /// <summary>
    /// The amount of power restored when collected.
    /// </summary>
    public byte PowerGain { get; set; }

    /// <summary>
    /// The save state flag associated with this pickup.
    /// </summary>
    public byte SaveFlag { get; set; }

    /// <summary>
    /// Runtime initialization flag, typically 0 on disk.
    /// </summary>
    public sbyte Initialized { get; set; }
}

using EvilHop.Assets.Serialization;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

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
    /// <remarks>
    /// Not present when <see cref="FormatProfile.PickupTypesHasPulseFields"/> is <see langword="false"/>.
    /// </remarks>
    public AssetId PulseModelId { get; set; }

    /// <summary>
    /// The duration or rate of the pickup's pulsing effect.
    /// </summary>
    /// <remarks>
    /// Not present when <see cref="FormatProfile.PickupTypesHasPulseFields"/> is <see langword="false"/>.
    /// </remarks>
    public float PulseTime { get; set; }

    /// <summary>
    /// The additional scale applied during pulsing.
    /// </summary>
    /// <remarks>
    /// Not present when <see cref="FormatProfile.PickupTypesHasPulseFields"/> is <see langword="false"/>.
    /// </remarks>
    public float PulseAddScale { get; set; }

    /// <summary>
    /// The vertical offset downwards applied during pulsing.
    /// </summary>
    /// <remarks>
    /// Not present when <see cref="FormatProfile.PickupTypesHasPulseFields"/> is <see langword="false"/>.
    /// </remarks>
    public float PulseMoveDown { get; set; }

    /// <summary>
    /// The color multiplier or tint applied to the pickup's model.
    /// </summary>
    /// <remarks>
    /// Not present when <see cref="FormatProfile.PickupTypesHasPulseFields"/> is <see langword="false"/>.
    /// </remarks>
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

    internal static PickupTypeEntry Read(EndianReader reader, FormatProfile profile)
    {
        var entry = new PickupTypeEntry
        {
            TypeHash = reader.ReadAssetId(),
            ModelId = reader.ReadAssetId(),
        };

        if (profile.PickupTypesHasPulseFields)
        {
            entry.PulseModelId = reader.ReadAssetId();
            entry.PulseTime = reader.ReadSingle();
            entry.PulseAddScale = reader.ReadSingle();
            entry.PulseMoveDown = reader.ReadSingle();
            entry.ColorMultiplier = reader.ReadRgb();
        }

        entry.Color = reader.ReadUInt32();
        entry.FlyingSoundGroupId = reader.ReadAssetId();
        entry.UsedSoundGroupId = reader.ReadAssetId();
        entry.CantUseSoundGroupId = reader.ReadAssetId();
        entry.HealthGain = reader.ReadByte();
        entry.PowerGain = reader.ReadByte();
        entry.SaveFlag = reader.ReadByte();
        entry.Initialized = reader.ReadSByte();

        return entry;
    }

    internal static void Write(PickupTypeEntry value, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(value.TypeHash);
        writer.Write(value.ModelId);

        if (profile.PickupTypesHasPulseFields)
        {
            writer.Write(value.PulseModelId);
            writer.Write(value.PulseTime);
            writer.Write(value.PulseAddScale);
            writer.Write(value.PulseMoveDown);
            writer.Write(value.ColorMultiplier);
        }

        writer.Write(value.Color);
        writer.Write(value.FlyingSoundGroupId);
        writer.Write(value.UsedSoundGroupId);
        writer.Write(value.CantUseSoundGroupId);
        writer.Write(value.HealthGain);
        writer.Write(value.PowerGain);
        writer.Write(value.SaveFlag);
        writer.Write(value.Initialized);
    }
}

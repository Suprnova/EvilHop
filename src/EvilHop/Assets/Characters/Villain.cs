using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// The xEntNPCAsset fields a <see cref="VillainAsset"/> stores after its entity prefix. Implemented by
/// <see cref="VillainAsset"/>, and by the villains a <see cref="DuplicatorAsset"/> embeds
/// (<see cref="DuplicatorAsset.Villain"/>, <see cref="DuplicatorAsset.Avatar"/>), so
/// <see cref="ReadVillain"/>/<see cref="WriteVillain"/> can serve all three without the embedded ones
/// pretending to be an <see cref="Asset"/>.
/// </summary>
internal interface IVillain
{
    /// <inheritdoc cref="VillainAsset.NpcFlags"/>
    int NpcFlags { get; set; }

    /// <inheritdoc cref="VillainAsset.NpcModelId"/>
    AssetId NpcModelId { get; set; }

    /// <inheritdoc cref="VillainAsset.NpcSettingsId"/>
    AssetId NpcSettingsId { get; set; }

    /// <inheritdoc cref="VillainAsset.MovePointId"/>
    AssetId MovePointId { get; set; }

    /// <inheritdoc cref="VillainAsset.TaskWidgetPrimeId"/>
    AssetId TaskWidgetPrimeId { get; set; }

    /// <inheritdoc cref="VillainAsset.TaskWidgetSecondId"/>
    AssetId TaskWidgetSecondId { get; set; }

    /// <inheritdoc cref="VillainAsset.NavigationMeshId"/>
    AssetId NavigationMeshId { get; set; }

    /// <inheritdoc cref="VillainAsset.SettingsId"/>
    AssetId SettingsId { get; set; }

    /// <summary>
    /// Reads the xEntNPCAsset fields from <paramref name="reader"/>'s current position into
    /// <paramref name="villain"/>.
    /// </summary>
    internal static void ReadVillain(EndianReader reader, FormatProfile profile, IVillain villain)
    {
        villain.NpcFlags = reader.ReadInt32();
        villain.NpcModelId = reader.ReadAssetId();
        villain.NpcSettingsId = reader.ReadAssetId();
        villain.MovePointId = reader.ReadAssetId();

        villain.TaskWidgetPrimeId = reader.ReadAssetId();
        if (profile.VillainHasTaskWidgetSecondId)
            villain.TaskWidgetSecondId = reader.ReadAssetId();

        if (profile.Game is GameVersion.Incredibles)
        {
            villain.NavigationMeshId = reader.ReadAssetId();
            villain.SettingsId = reader.ReadAssetId();
        }
    }

    /// <summary>
    /// Writes <paramref name="villain"/>'s xEntNPCAsset fields. See <see cref="ReadVillain"/>.
    /// </summary>
    internal static void WriteVillain(IVillain villain, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(villain.NpcFlags);
        writer.Write(villain.NpcModelId);
        writer.Write(villain.NpcSettingsId);
        writer.Write(villain.MovePointId);

        writer.Write(villain.TaskWidgetPrimeId);
        if (profile.VillainHasTaskWidgetSecondId)
            writer.Write(villain.TaskWidgetSecondId);

        if (profile.Game is GameVersion.Incredibles)
        {
            writer.Write(villain.NavigationMeshId);
            writer.Write(villain.SettingsId);
        }
    }
}

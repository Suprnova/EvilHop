using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityAsset"/> representing a non-player character, enemy, tiki, or boss.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/VIL">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class VillainAsset : EntityAsset, IHasModel, IGrabbable
{
    /// <summary>
    /// Initializes a new instance of <see cref="VillainAsset"/>.
    /// </summary>
    public VillainAsset() : base(AssetType.Villain)
    {
        _baseType = 0x2B;
    }

    /// <summary>
    /// Flags configuring this NPC's behavior. Bit 0x1 indicates inactive.
    /// </summary>
    public int NpcFlags { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> identifying the NPC model or type.
    /// </summary>
    public AssetId NpcModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.NPCSettings"/> asset for this NPC, if any.
    /// </summary>
    public AssetId NpcSettingsId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the initial <see cref="AssetType.MovePoint"/> for this NPC, if any.
    /// </summary>
    public AssetId MovePointId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the primary task widget (e.g. taskbox), if any.
    /// </summary>
    public AssetId TaskWidgetPrimeId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the secondary task widget (e.g. taskbox), if any.
    /// </summary>
    public AssetId TaskWidgetSecondId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the navigation mesh used by this NPC. Present only in <see cref="GameVersion.Incredibles"/>.
    /// </summary>
    public AssetId NavigationMeshId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the settings asset for this NPC. Present only in <see cref="GameVersion.Incredibles"/>.
    /// </summary>
    public AssetId SettingsId { get; set; }

    AssetId IHasModel.ModelId
    {
        get => Physical.ModelId;
        set => Physical.ModelId = value;
    }

    bool IGrabbable.IsGrabbable
    {
        get => HasCollisionFlag(CollisionFlags.Grabbable);
        set => SetCollisionFlag(CollisionFlags.Grabbable, value);
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Villain"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.Incredibles,
    };

    internal static VillainAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new VillainAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);

        asset.NpcFlags = reader.ReadInt32();
        asset.NpcModelId = reader.ReadAssetId();
        asset.NpcSettingsId = reader.ReadAssetId();
        asset.MovePointId = reader.ReadAssetId();
        asset.TaskWidgetPrimeId = reader.ReadAssetId();
        asset.TaskWidgetSecondId = reader.ReadAssetId();

        if (profile.Game is GameVersion.Incredibles)
        {
            asset.NavigationMeshId = reader.ReadAssetId();
            asset.SettingsId = reader.ReadAssetId();
        }

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(VillainAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);

        writer.Write(asset.NpcFlags);
        writer.Write(asset.NpcModelId);
        writer.Write(asset.NpcSettingsId);
        writer.Write(asset.MovePointId);
        writer.Write(asset.TaskWidgetPrimeId);
        writer.Write(asset.TaskWidgetSecondId);

        if (profile.Game is GameVersion.Incredibles)
        {
            writer.Write(asset.NavigationMeshId);
            writer.Write(asset.SettingsId);
        }

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

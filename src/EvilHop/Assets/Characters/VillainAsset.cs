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
public sealed class VillainAsset() : EntityAsset(AssetType.Villain, baseType: 0x2B), IVillain, IHasModel, IGrabbable
{
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
    /// The <see cref="AssetId"/> of the secondary task widget (e.g. taskbox), if any. Not present -
    /// per <see cref="FormatProfile.VillainHasTaskWidgetSecondId"/> - in BFBB's leftover
    /// <c>gl/Working</c>/<c>gl/New Folder</c> archives.
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
        EntityAssetPrefix.Read(asset, reader, profile);
        IVillain.ReadVillain(reader, profile, asset);

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(VillainAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);
        IVillain.WriteVillain(asset, writer, profile);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

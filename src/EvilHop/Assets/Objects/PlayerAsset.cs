using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// The single entry point for the player character. Carries no gameplay tuning of its own - physics,
/// animation, and camera behavior all come from hardcoded settings - beyond a starting
/// <see cref="LightKitId"/> and whatever <see cref="BaseAsset.Links"/> respond to player events.
/// </summary>
/// <remarks>
/// Position is usually ignored, since the player is placed at a <see cref="AssetType.Marker"/>
/// instead; rotation X/Z, <see cref="EntityAsset.Scale"/>, and <see cref="EntityAsset.ColorMultiplier"/>
/// all reset to their defaults once the player starts moving.
/// <seealso href="https://heavyironmodding.org/wiki/PLYR">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class PlayerAsset : EntityAsset
{
    /// <summary>
    /// The <see cref="AssetType.LightKit"/> applied to the player. Not present in
    /// <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId LightKitId { get; set; }

    internal PlayerAsset() { }

    internal static PlayerAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new PlayerAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;

        if (profile.Game is not GameVersion.N100F)
            asset.LightKitId = reader.ReadAssetId();

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PlayerAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);

        LinkSerialization.Write(asset, writer);

        if (profile.Game is not GameVersion.N100F)
            writer.Write(asset.LightKitId);

        writer.Write(asset.GetUnparsedTail());
    }
}

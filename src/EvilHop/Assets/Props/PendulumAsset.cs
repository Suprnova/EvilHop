using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityAsset"/> that swings back and forth about a pivot. Not present in any
/// official level outside <see cref="GameVersion.N100F"/> - other games use an
/// <see cref="AssetType.Platform"/> with a <see cref="PendulumMotion"/> for the same effect instead -
/// but every later game's engine still reads and drives one correctly.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PEND">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class PendulumAsset() : EntityAsset(AssetType.Pendulum, baseType: 0x12), IHasModel
{
    /// <summary>How this pendulum swings.</summary>
    public PendulumMotion Motion { get; set; } = new();

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Pendulum"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static PendulumAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new PendulumAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        asset.Motion = (PendulumMotion)EntityMotion.Read(reader, profile.Game);

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PendulumAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        asset.Motion.Write(writer, profile.Game);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

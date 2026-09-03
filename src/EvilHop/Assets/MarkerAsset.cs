using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A 3D position in global space, usually used to place the player in the stage after a warp or
/// checkpoint.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/MRKR">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class MarkerAsset : Asset
{
    /// <summary>
    /// The <see cref="MarkerAsset"/>'s position.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Marker"/> is known to be read by. Absent
    /// from <see cref="GameVersion.Ratatouille"/>.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };

    internal MarkerAsset() { }

    internal static MarkerAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new MarkerAsset();
        AssetFields.Populate(asset, header, debug);
        asset.Position = reader.ReadVector3();
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(MarkerAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write(asset.Position);
        writer.Write(asset.GetUnparsedTail());
    }
}

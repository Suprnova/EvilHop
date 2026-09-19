using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A no-data asset whose only purpose is to trigger events for other assets.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/DPAT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class DispatcherAsset() : BaseAsset(AssetType.Dispatcher, baseType: 0x1E)
{
    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Dispatcher"/> is known to be read by.
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

    internal static DispatcherAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new DispatcherAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(DispatcherAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);
        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

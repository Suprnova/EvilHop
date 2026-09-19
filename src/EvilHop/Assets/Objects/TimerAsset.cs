using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> that counts down a duration in seconds, optionally randomized,
/// and triggers events via <see cref="BaseAsset.Links"/> upon expiration.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/TIMR">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class TimerAsset() : BaseAsset(AssetType.Timer, baseType: 0x0E)
{
    /// <summary>
    /// The base duration of the timer, in seconds.
    /// </summary>
    public float Seconds { get; set; }

    /// <summary>
    /// The maximum random variation added to or subtracted from <see cref="Seconds"/>, in seconds.
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public float RandomRange { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Timer"/> is known to be read by.
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

    internal static TimerAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new TimerAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Seconds = reader.ReadSingle();
        if (profile.Game is not GameVersion.N100F) asset.RandomRange = reader.ReadSingle();

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(TimerAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        writer.Write(asset.Seconds);
        if (profile.Game is not GameVersion.N100F) writer.Write(asset.RandomRange);
        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

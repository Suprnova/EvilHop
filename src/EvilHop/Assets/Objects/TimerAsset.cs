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
public sealed class TimerAsset : BaseAsset
{
    /// <summary>
    /// Initializes a new instance of <see cref="TimerAsset"/>.
    /// </summary>
    public TimerAsset() : base(AssetType.Timer)
    {
        _baseType = 0x0E;
    }

    /// <summary>
    /// The base duration of the timer, in seconds.
    /// </summary>
    public float Seconds { get; set; }

    /// <summary>
    /// The maximum random variation added to or subtracted from <see cref="Seconds"/>, in seconds.
    /// </summary>
    public float RandomRange { get; set; }

    internal static TimerAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new TimerAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Seconds = reader.ReadSingle();
        asset.RandomRange = reader.ReadSingle();

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(TimerAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);
        writer.Write(asset.Seconds);
        writer.Write(asset.RandomRange);
        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

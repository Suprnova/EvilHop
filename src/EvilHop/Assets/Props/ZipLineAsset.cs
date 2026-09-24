using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> the player can grab and ride along a <see cref="AssetType.Spline"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/ZLIN">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class ZipLineAsset() : BaseAsset(AssetType.ZipLine, baseType: 0x40), Physical.IZipLineAsset
{
    /// <summary>
    /// The zip line's position.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// How far below the line the rider hangs.
    /// </summary>
    public float HangLength { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Spline"/> the zip line runs along.
    /// </summary>
    public AssetId SplineId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the entity bound to the zip line, if any.
    /// </summary>
    public AssetId BoundEntityId { get; set; }

    /// <summary>
    /// The speed the rider travels along the line at.
    /// </summary>
    public float Speed { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IZipLineAsset Physical => this;

    private byte _dismountType;
    byte Physical.IZipLineAsset.DismountType { get => _dismountType; set => _dismountType = value; }

    private uint _zipLineFlags;
    uint Physical.IZipLineAsset.ZipLineFlags { get => _zipLineFlags; set => _zipLineFlags = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.ZipLine"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };

    internal static ZipLineAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ZipLineAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.DismountType = reader.ReadByte();
        reader.ReadBytes(3); // padding, always zero
        asset.Position = reader.ReadVector3();
        asset.HangLength = reader.ReadSingle();
        asset.SplineId = reader.ReadAssetId();
        asset.BoundEntityId = reader.ReadAssetId();
        asset.Speed = reader.ReadSingle();
        asset.Physical.ZipLineFlags = reader.ReadUInt32();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ZipLineAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.DismountType);
        writer.Write(new byte[3]); // padding
        writer.Write(asset.Position);
        writer.Write(asset.HangLength);
        writer.Write(asset.SplineId);
        writer.Write(asset.BoundEntityId);
        writer.Write(asset.Speed);
        writer.Write(asset.Physical.ZipLineFlags);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="ZipLineAsset"/>'s underlying values.
    /// </summary>
    public interface IZipLineAsset : IBaseAsset
    {
        /// <summary>
        /// How the rider dismounts the zip line. Always 0; the meaning of other values is unknown.
        /// </summary>
        byte DismountType { get; set; }

        /// <summary>
        /// Unknown. 0, or 2 on every zip line with a <see cref="ZipLineAsset.BoundEntityId"/>.
        /// </summary>
        uint ZipLineFlags { get; set; }
    }
}

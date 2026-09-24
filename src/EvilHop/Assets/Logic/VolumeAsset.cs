using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> marking out a region of the world - a sphere, box, or cylinder - for
/// other assets to test positions against.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/VOLU">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class VolumeAsset() : BaseAsset(AssetType.Volume, baseType: 0x1D), Physical.IVolumeAsset
{
    /// <summary>
    /// The region's shape.
    /// </summary>
    public Bound Shape { get; set; } = new Bound.Box();

    /// <summary>
    /// The region's rotation, in radians, about the vertical axis through
    /// (<see cref="PivotX"/>, <see cref="PivotZ"/>).
    /// </summary>
    /// TODO: validate against decompiled source
    public float Rotation { get; set; }

    /// <summary>
    /// The X coordinate of the vertical axis <see cref="Rotation"/> turns the region about.
    /// </summary>
    public float PivotX { get; set; }

    /// <summary>
    /// The Z coordinate of the vertical axis <see cref="Rotation"/> turns the region about.
    /// </summary>
    public float PivotZ { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IVolumeAsset Physical => this;

    private uint _volumeFlags;
    uint Physical.IVolumeAsset.VolumeFlags { get => _volumeFlags; set => _volumeFlags = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Volume"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.ROTU,
    };

    /// <exception cref="InvalidDataException">The stored <see cref="Bound.ShapeKind"/> is unknown.</exception>
    internal static VolumeAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new VolumeAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.VolumeFlags = reader.ReadUInt32();
        if (profile.Game is not GameVersion.N100F)
            reader.ReadBytes(QuickCullSize); // runtime quick-cull cache, always zero
        asset.Shape = Bound.Read(reader, profile);
        reader.ReadUInt32(); // runtime-resolved xMat4x3 pointer, always zero
        if (profile.Game is GameVersion.N100F)
            reader.ReadBytes(12); // padding, always zero

        asset.Rotation = reader.ReadSingle();
        asset.PivotX = reader.ReadSingle();
        asset.PivotZ = reader.ReadSingle();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(VolumeAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.VolumeFlags);
        if (profile.Game is not GameVersion.N100F)
            writer.Write(new byte[QuickCullSize]); // runtime quick-cull cache
        Bound.Write(asset.Shape, writer, profile);
        writer.Write(0u); // runtime-resolved xMat4x3 pointer
        if (profile.Game is GameVersion.N100F)
            writer.Write(new byte[12]); // padding

        writer.Write(asset.Rotation);
        writer.Write(asset.PivotX);
        writer.Write(asset.PivotZ);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// The size, in bytes, of the runtime-only xQCData quick-cull cache stored ahead of
    /// <see cref="Shape"/> outside <see cref="GameVersion.N100F"/>.
    /// </summary>
    private const int QuickCullSize = 32;
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="VolumeAsset"/>'s underlying values.
    /// </summary>
    public interface IVolumeAsset : IBaseAsset
    {
        /// <summary>
        /// Unknown. Always 0.
        /// </summary>
        uint VolumeFlags { get; set; }
    }
}

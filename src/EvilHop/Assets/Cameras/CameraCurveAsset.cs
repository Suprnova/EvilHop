using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> defining a pair of curves that a <see cref="CameraAsset"/> can move
/// along, tuned along their length by a sequence of <see cref="Beads"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/CCRV">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class CameraCurveAsset() : BaseAsset(AssetType.CameraCurve, baseType: 0x8D), Physical.ICameraCurveAsset
{
    /// <summary>
    /// Which <see cref="CameraKind"/> this curve applies to.
    /// </summary>
    public CameraKind CameraType { get; set; }

    /// <summary>How the camera eases into this curve over <see cref="TransitionTime"/>.</summary>
    public CameraTransitionType TransitionType { get; set; }

    /// <summary>The time, in seconds, it takes to move the camera onto this curve.</summary>
    public float TransitionTime { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the first rail this curve moves along.
    /// </summary>
    public AssetId CurveId1 { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the second rail this curve moves along.
    /// </summary>
    public AssetId CurveId2 { get; set; }

    /// <summary>
    /// The tuning points placed along the curve's length.
    /// </summary>
    public Collection<CameraCurveBead> Beads { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ICameraCurveAsset Physical => this;

    private byte _version;
    byte Physical.ICameraCurveAsset.Version { get => _version; set => _version = value; }

    private uint _cameraFlags;
    uint Physical.ICameraCurveAsset.CameraFlags { get => _cameraFlags; set => _cameraFlags = value; }

    private int? _overriddenNumBeads;
    int Physical.ICameraCurveAsset.NumBeads
    {
        get => _overriddenNumBeads ?? Beads.Count;
        set => _overriddenNumBeads = value == Beads.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.CameraCurve"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static CameraCurveAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new CameraCurveAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.Version = reader.ReadByte();
        reader.ReadBytes(3); // padding, always zero
        asset.CameraType = (CameraKind)reader.ReadInt32();
        asset.Physical.CameraFlags = reader.ReadUInt32();
        asset.TransitionType = (CameraTransitionType)reader.ReadInt32();
        asset.TransitionTime = reader.ReadSingle();
        asset.CurveId1 = reader.ReadAssetId();
        asset.CurveId2 = reader.ReadAssetId();

        int numBeads = reader.ReadInt32();
        for (int i = 0; i < numBeads; i++)
            asset.Beads.Add(CameraCurveBead.Read(reader, profile));
        asset.Physical.NumBeads = numBeads;

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(CameraCurveAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.Version);
        writer.Write(new byte[3]); // padding
        writer.Write((int)asset.CameraType);
        writer.Write(asset.Physical.CameraFlags);
        writer.Write((int)asset.TransitionType);
        writer.Write(asset.TransitionTime);
        writer.Write(asset.CurveId1);
        writer.Write(asset.CurveId2);

        writer.Write(asset.Physical.NumBeads);
        foreach (var bead in asset.Beads)
            CameraCurveBead.Write(bead, writer, profile);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="CameraCurveAsset"/>'s underlying values.
    /// </summary>
    public interface ICameraCurveAsset : IBaseAsset
    {
        /// <summary>The internal version of the <see cref="CameraCurveAsset"/> struct.</summary>
        byte Version { get; set; }

        /// <summary>Unknown.</summary>
        uint CameraFlags { get; set; }

        /// <summary>
        /// The number of <see cref="CameraCurveAsset.Beads"/> stored for this asset, read directly from
        /// its leading count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="CameraCurveAsset.Beads"/>.Count exist, this field wins during serialization.
        /// </remarks>
        int NumBeads { get; set; }
    }
}

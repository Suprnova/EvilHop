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
public sealed class CameraCurveAsset() : BaseAsset(AssetType.CameraCurve), IPhysicalCameraCurveAsset
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
    public override IPhysicalCameraCurveAsset Physical => this;

    private byte _version;
    byte IPhysicalCameraCurveAsset.Version { get => _version; set => _version = value; }

    private uint _cameraFlags;
    uint IPhysicalCameraCurveAsset.CameraFlags { get => _cameraFlags; set => _cameraFlags = value; }

    private int? _overriddenNumBeads;
    int IPhysicalCameraCurveAsset.NumBeads
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

    internal static CameraCurveAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
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
        {
            asset.Beads.Add(new CameraCurveBead
            {
                CurveU1 = reader.ReadSingle(),
                CurveU2 = reader.ReadSingle(),
                DistanceAdjust = reader.ReadSingle(),
                PitchOffset = reader.ReadSingle(),
                TargetRadius = reader.ReadSingle(),
                TargetMarginAngle = reader.ReadSingle(),
                LeadOffset = reader.ReadSingle(),
                YOffset = reader.ReadSingle(),
                NearWallAdjust = reader.ReadSingle(),
                FarWallAdjust = reader.ReadSingle(),
            });
        }
        asset.Physical.NumBeads = asset.Beads.Count;

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(CameraCurveAsset asset, EndianWriter writer, FormatProfile _)
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
        {
            writer.Write(bead.CurveU1);
            writer.Write(bead.CurveU2);
            writer.Write(bead.DistanceAdjust);
            writer.Write(bead.PitchOffset);
            writer.Write(bead.TargetRadius);
            writer.Write(bead.TargetMarginAngle);
            writer.Write(bead.LeadOffset);
            writer.Write(bead.YOffset);
            writer.Write(bead.NearWallAdjust);
            writer.Write(bead.FarWallAdjust);
        }

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="CameraCurveAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalCameraCurveAsset : IPhysicalBaseAsset
{
    /// <summary>Currently always 4.</summary>
    byte Version { get; set; }

    /// <summary>Unknown. Usually 0.</summary>
    uint CameraFlags { get; set; }

    /// <summary>
    /// The number of <see cref="CameraCurveAsset.Beads"/> stored for this asset, read directly from
    /// its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="CameraCurveAsset.Beads"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    int NumBeads { get; set; }
}

/// <summary>
/// One tuning point along a <see cref="CameraCurveAsset"/>'s length.
/// </summary>
public sealed class CameraCurveBead
{
    /// <summary>The parameter along the first rail this bead sits at.</summary>
    public float CurveU1 { get; set; }

    /// <summary>The parameter along the second rail this bead sits at.</summary>
    public float CurveU2 { get; set; }

    /// <summary>An adjustment to the camera's distance from its target at this bead.</summary>
    public float DistanceAdjust { get; set; }

    /// <summary>An adjustment to the camera's pitch at this bead.</summary>
    public float PitchOffset { get; set; }

    /// <summary>The radius, around the target, the camera tries to keep in view at this bead.</summary>
    public float TargetRadius { get; set; }

    /// <summary>The angle of margin allowed before the camera adjusts to keep its target in view.</summary>
    public float TargetMarginAngle { get; set; }

    /// <summary>How far ahead of its target the camera leads at this bead.</summary>
    public float LeadOffset { get; set; }

    /// <summary>A vertical offset applied to the camera at this bead.</summary>
    public float YOffset { get; set; }

    /// <summary>An adjustment made to keep the camera from clipping into a nearby wall.</summary>
    public float NearWallAdjust { get; set; }

    /// <summary>An adjustment made to keep the camera from drifting too far past a distant wall.</summary>
    public float FarWallAdjust { get; set; }
}

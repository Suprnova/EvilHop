using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> describing a NURBS curve through the world, for other assets to follow
/// or measure against.
/// </summary>
/// <remarks>
/// <para>
/// The game never reads a spline's <see cref="BaseAsset.Links"/>, so none are read or written; a
/// <see cref="Physical.IBaseAsset.LinkCount"/> round-trips on its own, with no links behind it.
/// </para>
/// <para>
/// In <see cref="GameVersion.TSSM"/> and <see cref="GameVersion.Incredibles"/>, the
/// <see cref="BaseAsset"/> header is stored little-endian on every platform, while the rest of the
/// asset follows the platform's byte order.
/// </para>
/// </remarks>
public sealed class SplineAsset() : BaseAsset(AssetType.Spline, baseType: 0x49), Physical.ISplineAsset
{
    /// <summary>
    /// The curve's degree: 1 for straight segments between <see cref="ControlPoints"/>, 3 for a
    /// smooth curve.
    /// </summary>
    /// <remarks>
    /// The game supports degrees up to 4. Changing it without resizing <see cref="Knots"/> to match
    /// corrupts the curve.
    /// </remarks>
    public int Degree { get; set; }

    /// <summary>
    /// The points the curve is shaped by.
    /// </summary>
    /// <remarks>
    /// Adding or removing a control point without resizing <see cref="Knots"/> to match corrupts the
    /// curve.
    /// </remarks>
    public Collection<Vector3> ControlPoints { get; } = [];

    /// <summary>
    /// The curve's knot vector, in non-decreasing order, dividing the curve's parameter range between
    /// <see cref="ControlPoints"/>.
    /// </summary>
    /// <remarks>
    /// Must hold exactly <see cref="ControlPoints"/>' count plus <see cref="Degree"/> plus one
    /// knots, or the curve is corrupt.
    /// </remarks>
    public Collection<float> Knots { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ISplineAsset Physical => this;

    private int? _overriddenKnotMaxIndex;
    int Physical.ISplineAsset.KnotMaxIndex
    {
        get => _overriddenKnotMaxIndex ?? Knots.Count - 1;
        set => _overriddenKnotMaxIndex = value == Knots.Count - 1 ? null : value;
    }

    private int? _overriddenControlPointMaxIndex;
    int Physical.ISplineAsset.ControlPointMaxIndex
    {
        get => _overriddenControlPointMaxIndex ?? ControlPoints.Count - 1;
        set => _overriddenControlPointMaxIndex = value == ControlPoints.Count - 1 ? null : value;
    }

    private uint _knotsPointer;
    uint Physical.ISplineAsset.KnotsPointer { get => _knotsPointer; set => _knotsPointer = value; }

    private uint _controlPointsPointer;
    uint Physical.ISplineAsset.ControlPointsPointer { get => _controlPointsPointer; set => _controlPointsPointer = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Spline"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static SplineAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SplineAsset();
        AssetFields.Populate(asset, header, debug);
        using (var headerReader = new EndianReader(reader.BaseStream, HeaderEndianness(profile, reader.Endianness), leaveOpen: true))
            BaseAssetPrefix.Read(asset, headerReader);

        asset.Degree = reader.ReadInt32();
        int knotMaxIndex = reader.ReadInt32();
        int controlPointMaxIndex = reader.ReadInt32();
        asset.Physical.KnotsPointer = reader.ReadUInt32();
        asset.Physical.ControlPointsPointer = reader.ReadUInt32();

        for (int i = 0; i <= controlPointMaxIndex; i++)
            asset.ControlPoints.Add(reader.ReadVector3());
        for (int i = 0; i <= knotMaxIndex; i++)
            asset.Knots.Add(reader.ReadSingle());

        asset.Physical.KnotMaxIndex = knotMaxIndex;
        asset.Physical.ControlPointMaxIndex = controlPointMaxIndex;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SplineAsset asset, EndianWriter writer, FormatProfile profile)
    {
        using (var headerWriter = new EndianWriter(writer.BaseStream, HeaderEndianness(profile, writer.Endianness), leaveOpen: true))
            BaseAssetPrefix.Write(asset, headerWriter);

        writer.Write(asset.Degree);
        writer.Write(asset.Physical.KnotMaxIndex);
        writer.Write(asset.Physical.ControlPointMaxIndex);
        writer.Write(asset.Physical.KnotsPointer);
        writer.Write(asset.Physical.ControlPointsPointer);

        foreach (var point in asset.ControlPoints)
            writer.Write(point);
        foreach (float knot in asset.Knots)
            writer.Write(knot);

        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// The byte order the <see cref="BaseAsset"/> header is stored in under <paramref name="profile"/>.
    /// </summary>
    private static Endianness HeaderEndianness(FormatProfile profile, Endianness platform) =>
        profile.Game is GameVersion.TSSM or GameVersion.Incredibles ? Endianness.Little : platform;
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="SplineAsset"/>'s underlying values.
    /// </summary>
    public interface ISplineAsset : IBaseAsset
    {
        /// <summary>
        /// The index of the last of <see cref="SplineAsset.Knots"/>, read directly from its stored field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="SplineAsset.Knots"/>.Count exist, this field wins during
        /// serialization.
        /// </remarks>
        int KnotMaxIndex { get; set; }

        /// <summary>
        /// The index of the last of <see cref="SplineAsset.ControlPoints"/>, read directly from its
        /// stored field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="SplineAsset.ControlPoints"/>.Count exist, this field wins
        /// during serialization.
        /// </remarks>
        int ControlPointMaxIndex { get; set; }

        /// <summary>
        /// The export tool's own memory address for <see cref="SplineAsset.Knots"/>. Never read by the
        /// game.
        /// </summary>
        uint KnotsPointer { get; set; }

        /// <summary>
        /// The export tool's own memory address for <see cref="SplineAsset.ControlPoints"/>. Never read
        /// by the game.
        /// </summary>
        uint ControlPointsPointer { get; set; }
    }
}

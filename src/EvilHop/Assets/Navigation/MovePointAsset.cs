using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A point in space that other assets, most commonly NPCs, move along or between.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/MVPT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class MovePointAsset() : BaseAsset(AssetType.MovePoint, baseType: 0x0D), Physical.IMovePointAsset
{
    /// <summary>This move point's position in the game world.</summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// This move point's weight, biasing how often it is chosen when randomly selecting among its
    /// siblings. Usually 10000.
    /// </summary>
    public ushort Weight { get; set; }

    /// <summary>This move point's kind.</summary>
    public PathKind Kind { get; set; }

    /// <summary>This move point's role, if any, in a bezier curve.</summary>
    public BezierRole Role { get; set; }

    /// <summary>
    /// How long, in seconds, an occupant waits at this move point before continuing on. A value of 0
    /// or less disables the wait.
    /// </summary>
    public float Delay { get; set; }

    /// <summary>
    /// The distance an occupant will circle around this move point at, if this is a
    /// <see cref="PathKind.Zone"/>. A value of -1 disables circling. Not present in
    /// <see cref="GameVersion.N100F"/>.
    /// </summary>
    public float ZoneRadius { get; set; }

    /// <summary>
    /// The radius within which an occupant can detect the player from this move point, if this is an
    /// <see cref="PathKind.Arena"/>, much like a sphere <see cref="AssetType.Trigger"/>. A value
    /// of -1 disables detection. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public float ArenaRadius { get; set; }

    /// <summary>The other <see cref="AssetType.MovePoint"/>s this move point can lead to.</summary>
    public Collection<AssetId> SiblingIds { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IMovePointAsset Physical => this;

    private byte _flagsProps;
    byte Physical.IMovePointAsset.FlagsProps { get => _flagsProps; set => _flagsProps = value; }

    private ushort? _overriddenNumPoints;
    ushort Physical.IMovePointAsset.NumPoints
    {
        get => _overriddenNumPoints ?? (ushort)SiblingIds.Count;
        set => _overriddenNumPoints = value == (ushort)SiblingIds.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.MovePoint"/> is known to be read by.
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

    internal static MovePointAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new MovePointAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Position = reader.ReadVector3();
        asset.Weight = reader.ReadUInt16();
        asset.Kind = (PathKind)reader.ReadByte();
        asset.Role = (BezierRole)reader.ReadByte();
        asset.Physical.FlagsProps = reader.ReadByte();
        reader.ReadByte(); // pad, always zero
        int numPoints = reader.ReadUInt16();
        asset.Delay = reader.ReadSingle();

        if (profile.Game is not GameVersion.N100F)
        {
            asset.ZoneRadius = reader.ReadSingle();
            asset.ArenaRadius = reader.ReadSingle();
        }

        for (int i = 0; i < numPoints; i++)
            asset.SiblingIds.Add(reader.ReadAssetId());

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.NumPoints = (ushort)asset.SiblingIds.Count;
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(MovePointAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Position);
        writer.Write(asset.Weight);
        writer.Write((byte)asset.Kind);
        writer.Write((byte)asset.Role);
        writer.Write(asset.Physical.FlagsProps);
        writer.Write((byte)0); // pad
        writer.Write(asset.Physical.NumPoints);
        writer.Write(asset.Delay);

        if (profile.Game is not GameVersion.N100F)
        {
            writer.Write(asset.ZoneRadius);
            writer.Write(asset.ArenaRadius);
        }

        foreach (var siblingId in asset.SiblingIds)
            writer.Write(siblingId);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// Defines whether an occupant moves along or patrols within the area of a <see cref="MovePointAsset"/>.
    /// </summary>
    public enum PathKind : byte
    {
        /// <summary>
        /// This move point does not define the area an occupant moves within; instead,
        /// <see cref="ArenaRadius"/> defines the area within which it can detect the
        /// player, much like a sphere <see cref="AssetType.Trigger"/>. An arena move point will
        /// typically have no <see cref="SiblingIds"/>.
        /// </summary>
        Arena = 0,

        /// <summary>
        /// Occupants start at, or move toward, this move point. After first reaching it, an occupant
        /// with no <see cref="SiblingIds"/> stops; otherwise it proceeds to each sibling
        /// in turn, looping back to the first after the last.
        /// </summary>
        Zone = 1,
    }

    /// <summary>
    /// Defines how a <see cref="MovePointAsset"/> participates in a bezier curve path.
    /// </summary>
    public enum BezierRole : byte
    {
        /// <summary>This move point is not part of a bezier curve.</summary>
        None = 0,

        /// <summary>
        /// A bezier curve is built using this move point, its previous move point, and its first
        /// sibling.
        /// </summary>
        Curve = 1,

        /// <summary>
        /// This move point serves as a secondary control point along the bezier curve.
        /// </summary>
        ControlPoint = 2,
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="MovePointAsset"/>'s underlying values.
    /// </summary>
    public interface IMovePointAsset : IBaseAsset
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        byte FlagsProps { get; set; }

        /// <summary>
        /// The number of <see cref="MovePointAsset.SiblingIds"/> stored for this asset, read directly
        /// from its fixed header.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="MovePointAsset.SiblingIds"/>.Count exist, this field wins
        /// during serialization.
        /// </remarks>
        ushort NumPoints { get; set; }
    }
}

using EvilHop.Common;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A point in space that other assets, most commonly NPCs, move along or between.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/MVPT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class MovePointAsset() : BaseAsset(AssetType.MovePoint), IPhysicalMovePointAsset
{
    /// <summary>This move point's position in the game world.</summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// This move point's weight, biasing how often it is chosen when randomly selecting among its
    /// siblings. Usually 10000.
    /// </summary>
    public ushort Weight { get; set; }

    /// <summary>This move point's kind.</summary>
    public MovePointKind Kind { get; set; }

    /// <summary>This move point's role, if any, in a bezier curve.</summary>
    public MovePointBezierRole BezierRole { get; set; }

    /// <summary>
    /// How long, in seconds, an occupant waits at this move point before continuing on. A value of 0
    /// or less disables the wait.
    /// </summary>
    public float Delay { get; set; }

    /// <summary>
    /// The distance an occupant will circle around this move point at, if this is a
    /// <see cref="MovePointKind.Zone"/>. A value of -1 disables circling. Not present in
    /// <see cref="GameVersion.N100F"/>.
    /// </summary>
    public float ZoneRadius { get; set; }

    /// <summary>
    /// The radius within which an occupant can detect the player from this move point, if this is an
    /// <see cref="MovePointKind.Arena"/>, much like a sphere <see cref="AssetType.Trigger"/>. A value
    /// of -1 disables detection. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public float ArenaRadius { get; set; }

    /// <summary>The other <see cref="AssetType.MovePoint"/>s this move point can lead to.</summary>
    public Collection<AssetId> SiblingIds { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalMovePointAsset Physical => this;

    private byte _flagsProps;
    byte IPhysicalMovePointAsset.FlagsProps { get => _flagsProps; set => _flagsProps = value; }

    private ushort? _overriddenNumPoints;
    ushort IPhysicalMovePointAsset.NumPoints
    {
        get => _overriddenNumPoints ?? (ushort)SiblingIds.Count;
        set => _overriddenNumPoints = value == (ushort)SiblingIds.Count ? null : value;
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="MovePointAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalMovePointAsset : IPhysicalBaseAsset
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

/// <summary>
/// Represents all known values for <see cref="MovePointAsset.Kind"/>.
/// </summary>
public enum MovePointKind : byte
{
    /// <summary>
    /// This move point does not define the area an occupant moves within; instead,
    /// <see cref="MovePointAsset.ArenaRadius"/> defines the area within which it can detect the
    /// player, much like a sphere <see cref="AssetType.Trigger"/>. An arena move point will
    /// typically have no <see cref="MovePointAsset.SiblingIds"/>.
    /// </summary>
    Arena = 0,

    /// <summary>
    /// Occupants start at, or move toward, this move point. After first reaching it, an occupant
    /// with no <see cref="MovePointAsset.SiblingIds"/> stops; otherwise it proceeds to each sibling
    /// in turn, looping back to the first after the last.
    /// </summary>
    Zone = 1,
}

/// <summary>
/// Represents all known values for <see cref="MovePointAsset.BezierRole"/>.
/// </summary>
/// TODO: validate against decompiled source; the distinct effect of <see cref="Unknown2"/> versus
/// <see cref="Curve"/> is not confirmed
public enum MovePointBezierRole : byte
{
    /// <summary>This move point is not part of a bezier curve.</summary>
    None = 0,

    /// <summary>
    /// A bezier curve is built using this move point, its previous move point, and its first
    /// sibling.
    /// </summary>
    Curve = 1,

    /// <summary>Unknown.</summary>
    Unknown2 = 2,
}

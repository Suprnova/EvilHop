using EvilHop.Common;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A triangle mesh used by the Dash minigame's traversal logic. <see cref="Vertices"/> and
/// <see cref="Triangles"/> describe the walkable surface, <see cref="Portals"/> links each
/// triangle to its neighbors across each edge for walking the mesh, and
/// <see cref="LandableStart"/>/<see cref="LeavableStart"/> mark where the player may land onto
/// or leave the track.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/DTRK">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class DashTrackAsset() : BaseAsset(AssetType.DashTrack), IPhysicalDashTrackAsset
{
    /// <summary>
    /// The mesh's vertices, indexed by <see cref="DashTrackTriangle.VertexA"/>/<see cref="DashTrackTriangle.VertexB"/>/<see cref="DashTrackTriangle.VertexC"/>.
    /// </summary>
    public Collection<Vector3> Vertices { get; } = [];

    /// <summary>The mesh's triangles.</summary>
    public Collection<DashTrackTriangle> Triangles { get; } = [];

    /// <summary>
    /// One entry per <see cref="Triangles"/> entry, giving the neighboring triangle across each of
    /// its three edges.
    /// </summary>
    public Collection<DashTrackPortal> Portals { get; } = [];

    /// <summary>The index into <see cref="Triangles"/> where the player may land onto the track.</summary>
    /// TODO: validate against decompiled source
    public int LandableStart { get; set; }

    /// <summary>The index into <see cref="Triangles"/> where the player may leave the track.</summary>
    /// TODO: validate against decompiled source
    public int LeavableStart { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalDashTrackAsset Physical => this;

    private int? _overriddenVertexCount;
    int IPhysicalDashTrackAsset.VertexCount
    {
        get => _overriddenVertexCount ?? Vertices.Count;
        set => _overriddenVertexCount = value == Vertices.Count ? null : value;
    }

    private int? _overriddenTriangleCount;
    int IPhysicalDashTrackAsset.TriangleCount
    {
        get => _overriddenTriangleCount ?? Triangles.Count;
        set => _overriddenTriangleCount = value == Triangles.Count ? null : value;
    }

    private uint _unknown1;
    uint IPhysicalDashTrackAsset.Unknown1 { get => _unknown1; set => _unknown1 = value; }

    private uint _unknown2;
    uint IPhysicalDashTrackAsset.Unknown2 { get => _unknown2; set => _unknown2 = value; }

    private uint _unknown3;
    uint IPhysicalDashTrackAsset.Unknown3 { get => _unknown3; set => _unknown3 = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.DashTrack"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };
}

/// <summary>
/// An explicit interface used to interact with <see cref="DashTrackAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalDashTrackAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// The number of <see cref="DashTrackAsset.Vertices"/> stored for this asset, read directly
    /// from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="DashTrackAsset.Vertices"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    int VertexCount { get; set; }

    /// <summary>
    /// The number of <see cref="DashTrackAsset.Triangles"/> stored for this asset, read directly
    /// from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="DashTrackAsset.Triangles"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    int TriangleCount { get; set; }

    /// <summary>Unknown.</summary>
    /// TODO: validate against decompiled source; this and the two fields below occupy the same 12
    /// bytes as <c>track_asset</c>'s <c>vertex</c>/<c>triangle_list</c>/<c>portal</c> pointers, and
    /// may just be their leftover in-memory values.
    uint Unknown1 { get; set; }

    /// <summary>Unknown.</summary>
    uint Unknown2 { get; set; }

    /// <summary>Unknown.</summary>
    uint Unknown3 { get; set; }
}

/// <summary>
/// One <see cref="DashTrackAsset"/> triangle: three <see cref="DashTrackAsset.Vertices"/> indices,
/// plus the per-edge coefficients used to test whether a point lies within it.
/// </summary>
public sealed class DashTrackTriangle
{
    /// <summary>The first of the triangle's three <see cref="DashTrackAsset.Vertices"/> indices.</summary>
    public ushort VertexA { get; set; }

    /// <summary>The second of the triangle's three <see cref="DashTrackAsset.Vertices"/> indices.</summary>
    public ushort VertexB { get; set; }

    /// <summary>The third of the triangle's three <see cref="DashTrackAsset.Vertices"/> indices.</summary>
    public ushort VertexC { get; set; }

    /// <summary>Unknown. Observed to always be 0 in every real archive.</summary>
    public ushort Flags { get; set; }

    /// <summary>Unknown. Used alongside <see cref="V"/>, likely in point-in-triangle tests.</summary>
    /// TODO: validate against decompiled source
    public Vector3 U { get; set; }

    /// <summary>Unknown. Used alongside <see cref="U"/>, likely in point-in-triangle tests.</summary>
    /// TODO: validate against decompiled source
    public Vector3 V { get; set; }
}

/// <summary>
/// The triangles neighboring one <see cref="DashTrackAsset"/> triangle across each of its three
/// edges, used to walk from one triangle to the next as the player crosses it. A value of
/// <c>0xFFFF</c> marks an edge with no neighbor.
/// </summary>
/// TODO: validate against decompiled source; the edge each of these three corresponds to is
/// inferred from position, not confirmed.
public sealed class DashTrackPortal
{
    /// <summary>The neighboring triangle across the edge opposite <see cref="DashTrackTriangle.VertexA"/>, or <c>0xFFFF</c> if none.</summary>
    public ushort Neighbor0 { get; set; }

    /// <summary>The neighboring triangle across the edge opposite <see cref="DashTrackTriangle.VertexB"/>, or <c>0xFFFF</c> if none.</summary>
    public ushort Neighbor1 { get; set; }

    /// <summary>The neighboring triangle across the edge opposite <see cref="DashTrackTriangle.VertexC"/>, or <c>0xFFFF</c> if none.</summary>
    public ushort Neighbor2 { get; set; }
}

using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
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
public sealed class DashTrackAsset() : BaseAsset(AssetType.DashTrack, baseType: 0xCD), Physical.IDashTrackAsset
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
    public int LandableStart { get; set; }

    /// <summary>The index into <see cref="Triangles"/> where the player may leave the track.</summary>
    public int LeavableStart { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IDashTrackAsset Physical => this;

    private int? _overriddenVertexCount;
    int Physical.IDashTrackAsset.VertexCount
    {
        get => _overriddenVertexCount ?? Vertices.Count;
        set => _overriddenVertexCount = value == Vertices.Count ? null : value;
    }

    private int? _overriddenTriangleCount;
    int Physical.IDashTrackAsset.TriangleCount
    {
        get => _overriddenTriangleCount ?? Triangles.Count;
        set => _overriddenTriangleCount = value == Triangles.Count ? null : value;
    }

    private uint _unknown1;
    uint Physical.IDashTrackAsset.Unknown1 { get => _unknown1; set => _unknown1 = value; }

    private uint _unknown2;
    uint Physical.IDashTrackAsset.Unknown2 { get => _unknown2; set => _unknown2 = value; }

    private uint _unknown3;
    uint Physical.IDashTrackAsset.Unknown3 { get => _unknown3; set => _unknown3 = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.DashTrack"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };

    internal static DashTrackAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new DashTrackAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        int vertexCount = reader.ReadInt32();
        int triangleCount = reader.ReadInt32();
        asset.LandableStart = reader.ReadInt32();
        asset.LeavableStart = reader.ReadInt32();
        asset.Physical.Unknown1 = reader.ReadUInt32();
        asset.Physical.Unknown2 = reader.ReadUInt32();
        asset.Physical.Unknown3 = reader.ReadUInt32();

        for (int i = 0; i < vertexCount; i++)
            asset.Vertices.Add(reader.ReadVector3());

        for (int i = 0; i < triangleCount; i++)
            asset.Triangles.Add(DashTrackTriangle.Read(reader, profile));

        // Portals have no leading count of their own - they fill whatever's left of the payload.
        byte[] portalBytes = reader.ReadRemainingBytes();
        using var portalReader = new EndianReader(new MemoryStream(portalBytes), profile.Endianness);
        while (portalBytes.Length - portalReader.BaseStream.Position >= 6)
            asset.Portals.Add(DashTrackPortal.Read(portalReader, profile));

        asset.Physical.VertexCount = vertexCount;
        asset.Physical.TriangleCount = triangleCount;
        asset.SetUnparsedTail(portalReader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(DashTrackAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        writer.Write(asset.Physical.VertexCount);
        writer.Write(asset.Physical.TriangleCount);
        writer.Write(asset.LandableStart);
        writer.Write(asset.LeavableStart);
        writer.Write(asset.Physical.Unknown1);
        writer.Write(asset.Physical.Unknown2);
        writer.Write(asset.Physical.Unknown3);

        foreach (var vertex in asset.Vertices)
            writer.Write(vertex);

        foreach (var triangle in asset.Triangles)
            DashTrackTriangle.Write(triangle, writer, profile);

        foreach (var portal in asset.Portals)
            DashTrackPortal.Write(portal, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
};

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="DashTrackAsset"/>'s underlying values.
    /// </summary>
    public interface IDashTrackAsset : IBaseAsset
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
        uint Unknown1 { get; set; }

        /// <summary>Unknown.</summary>
        uint Unknown2 { get; set; }

        /// <summary>Unknown.</summary>
        uint Unknown3 { get; set; }
    }
}

/// <summary>
/// One <see cref="DashTrackAsset"/> triangle: three <see cref="DashTrackAsset.Vertices"/> indices,
/// plus the per-edge coefficients used to test whether a point lies within it.
/// </summary>
public record struct DashTrackTriangle
{
    /// <summary>The first of the triangle's three <see cref="DashTrackAsset.Vertices"/> indices.</summary>
    public ushort VertexA { get; set; }

    /// <summary>The second of the triangle's three <see cref="DashTrackAsset.Vertices"/> indices.</summary>
    public ushort VertexB { get; set; }

    /// <summary>The third of the triangle's three <see cref="DashTrackAsset.Vertices"/> indices.</summary>
    public ushort VertexC { get; set; }

    /// <summary>Unknown.</summary>
    public ushort Flags { get; set; }

    /// <summary>Unknown.</summary>
    public Vector3 U { get; set; }

    /// <summary>Unknown.</summary>
    public Vector3 V { get; set; }

    internal static DashTrackTriangle Read(EndianReader reader, FormatProfile _) => new()
    {
        VertexA = reader.ReadUInt16(),
        VertexB = reader.ReadUInt16(),
        VertexC = reader.ReadUInt16(),
        Flags = reader.ReadUInt16(),
        U = reader.ReadVector3(),
        V = reader.ReadVector3(),
    };

    internal static void Write(DashTrackTriangle value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.VertexA);
        writer.Write(value.VertexB);
        writer.Write(value.VertexC);
        writer.Write(value.Flags);
        writer.Write(value.U);
        writer.Write(value.V);
    }
}

/// <summary>
/// The triangles neighboring one <see cref="DashTrackAsset"/> triangle across each of its three
/// edges, used to walk from one triangle to the next as the player crosses it. A value of
/// <c>0xFFFF</c> marks an edge with no neighbor.
/// </summary>
public record struct DashTrackPortal
{
    /// <summary>The neighboring triangle across the edge opposite <see cref="DashTrackTriangle.VertexA"/>, or <c>0xFFFF</c> if none.</summary>
    public ushort Neighbor0 { get; set; }

    /// <summary>The neighboring triangle across the edge opposite <see cref="DashTrackTriangle.VertexB"/>, or <c>0xFFFF</c> if none.</summary>
    public ushort Neighbor1 { get; set; }

    /// <summary>The neighboring triangle across the edge opposite <see cref="DashTrackTriangle.VertexC"/>, or <c>0xFFFF</c> if none.</summary>
    public ushort Neighbor2 { get; set; }

    internal static DashTrackPortal Read(EndianReader reader, FormatProfile _) => new()
    {
        Neighbor0 = reader.ReadUInt16(),
        Neighbor1 = reader.ReadUInt16(),
        Neighbor2 = reader.ReadUInt16(),
    };

    internal static void Write(DashTrackPortal value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.Neighbor0);
        writer.Write(value.Neighbor1);
        writer.Write(value.Neighbor2);
    }
}

using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A small mesh of grass blade geometry, instanced repeatedly across a level's grass knolls to fill
/// them with individually swaying blades.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/GRSM">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class GrassMeshAsset() : BaseAsset(AssetType.GrassMesh, baseType: 0xCD), IPhysicalGrassMeshAsset
{
    /// <summary>The mesh's vertices, indexed by <see cref="GrassMeshFace"/>.</summary>
    public Collection<GrassMeshVertex> Vertices { get; } = [];

    /// <summary>The mesh's triangular faces.</summary>
    public Collection<GrassMeshFace> Faces { get; } = [];

    /// <summary>The minimum corner of the mesh's bounding box.</summary>
    public Vector3 MinBounds { get; set; }

    /// <summary>The maximum corner of the mesh's bounding box.</summary>
    public Vector3 MaxBounds { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalGrassMeshAsset Physical => this;

    private int? _overriddenVertexCount;
    int IPhysicalGrassMeshAsset.VertexCount
    {
        get => _overriddenVertexCount ?? Vertices.Count;
        set => _overriddenVertexCount = value == Vertices.Count ? null : value;
    }

    private int? _overriddenFaceCount;
    int IPhysicalGrassMeshAsset.FaceCount
    {
        get => _overriddenFaceCount ?? Faces.Count;
        set => _overriddenFaceCount = value == Faces.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.GrassMesh"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };

    internal static GrassMeshAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new GrassMeshAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        int vertexCount = reader.ReadInt32();
        int faceCount = reader.ReadInt32();
        asset.MinBounds = reader.ReadVector3();
        asset.MaxBounds = reader.ReadVector3();

        for (int i = 0; i < vertexCount; i++)
            asset.Vertices.Add(ReadVertex(reader));

        for (int i = 0; i < faceCount; i++)
            asset.Faces.Add(ReadFace(reader));

        asset.Physical.VertexCount = vertexCount;
        asset.Physical.FaceCount = faceCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(GrassMeshAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);
        writer.Write(asset.Physical.VertexCount);
        writer.Write(asset.Physical.FaceCount);
        writer.Write(asset.MinBounds);
        writer.Write(asset.MaxBounds);

        foreach (var vertex in asset.Vertices)
            WriteVertex(writer, vertex);

        foreach (var face in asset.Faces)
            WriteFace(writer, face);

        writer.Write(asset.GetUnparsedTail());
    }

    private static GrassMeshVertex ReadVertex(EndianReader reader) => new()
    {
        Position = reader.ReadVector3(),
        Height = reader.ReadSingle(),
        Normal = reader.ReadVector3(),
        Color = reader.ReadRgba32(),
    };

    private static void WriteVertex(EndianWriter writer, GrassMeshVertex vertex)
    {
        writer.Write(vertex.Position);
        writer.Write(vertex.Height);
        writer.Write(vertex.Normal);
        writer.WriteRgba32(vertex.Color);
    }

    private static GrassMeshFace ReadFace(EndianReader reader) => new()
    {
        VertexA = reader.ReadUInt16(),
        VertexB = reader.ReadUInt16(),
        VertexC = reader.ReadUInt16(),
    };

    private static void WriteFace(EndianWriter writer, GrassMeshFace face)
    {
        writer.Write(face.VertexA);
        writer.Write(face.VertexB);
        writer.Write(face.VertexC);
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="GrassMeshAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalGrassMeshAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// The number of <see cref="GrassMeshAsset.Vertices"/> stored for this asset, read directly from
    /// its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="GrassMeshAsset.Vertices"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    int VertexCount { get; set; }

    /// <summary>
    /// The number of <see cref="GrassMeshAsset.Faces"/> stored for this asset, read directly from its
    /// leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="GrassMeshAsset.Faces"/>.Count exist, this field wins during
    /// serialization.
    /// </remarks>
    int FaceCount { get; set; }
}

/// <summary>One <see cref="GrassMeshAsset"/> vertex.</summary>
public sealed class GrassMeshVertex
{
    /// <summary>The vertex's position, relative to the mesh's origin.</summary>
    public Vector3 Position { get; set; }

    /// <summary>The vertex's height along the blade, used to weight how much wind sway displaces it.</summary>
    public float Height { get; set; }

    /// <summary>The vertex's normal, used for lighting.</summary>
    public Vector3 Normal { get; set; }

    /// <summary>The vertex's color.</summary>
    public Rgba Color { get; set; }
}

/// <summary>
/// One <see cref="GrassMeshAsset"/> face: three <see cref="GrassMeshAsset.Vertices"/> indices forming
/// a triangle.
/// </summary>
public sealed class GrassMeshFace
{
    /// <summary>The first of the face's three <see cref="GrassMeshAsset.Vertices"/> indices.</summary>
    public ushort VertexA { get; set; }

    /// <summary>The second of the face's three <see cref="GrassMeshAsset.Vertices"/> indices.</summary>
    public ushort VertexB { get; set; }

    /// <summary>The third of the face's three <see cref="GrassMeshAsset.Vertices"/> indices.</summary>
    public ushort VertexC { get; set; }
}

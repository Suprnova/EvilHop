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
public sealed class GrassMeshAsset() : BaseAsset(AssetType.GrassMesh, baseType: 0xCD), Physical.IGrassMeshAsset
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
    public override Physical.IGrassMeshAsset Physical => this;

    private int? _overriddenVertexCount;
    int Physical.IGrassMeshAsset.VertexCount
    {
        get => _overriddenVertexCount ?? Vertices.Count;
        set => _overriddenVertexCount = value == Vertices.Count ? null : value;
    }

    private int? _overriddenFaceCount;
    int Physical.IGrassMeshAsset.FaceCount
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

    internal static GrassMeshAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GrassMeshAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        int vertexCount = reader.ReadInt32();
        int faceCount = reader.ReadInt32();
        asset.MinBounds = reader.ReadVector3();
        asset.MaxBounds = reader.ReadVector3();

        for (int i = 0; i < vertexCount; i++)
            asset.Vertices.Add(GrassMeshVertex.Read(reader, profile));

        for (int i = 0; i < faceCount; i++)
            asset.Faces.Add(GrassMeshFace.Read(reader, profile));

        asset.Physical.VertexCount = vertexCount;
        asset.Physical.FaceCount = faceCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(GrassMeshAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        writer.Write(asset.Physical.VertexCount);
        writer.Write(asset.Physical.FaceCount);
        writer.Write(asset.MinBounds);
        writer.Write(asset.MaxBounds);

        foreach (var vertex in asset.Vertices)
            GrassMeshVertex.Write(vertex, writer, profile);

        foreach (var face in asset.Faces)
            GrassMeshFace.Write(face, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>One <see cref="GrassMeshAsset"/> vertex.</summary>
    public record struct GrassMeshVertex
    {
        /// <summary>The vertex's position, relative to the mesh's origin.</summary>
        public Vector3 Position { get; set; }

        /// <summary>The vertex's height along the blade, used to weight how much wind sway displaces it.</summary>
        public float Height { get; set; }

        /// <summary>The vertex's normal, used for lighting.</summary>
        public Vector3 Normal { get; set; }

        /// <summary>The vertex's color.</summary>
        public Rgba Color { get; set; }

        internal static GrassMeshVertex Read(EndianReader reader, FormatProfile _) => new()
        {
            Position = reader.ReadVector3(),
            Height = reader.ReadSingle(),
            Normal = reader.ReadVector3(),
            Color = reader.ReadRgba32(),
        };

        internal static void Write(GrassMeshVertex value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Position);
            writer.Write(value.Height);
            writer.Write(value.Normal);
            writer.WriteRgba32(value.Color);
        }
    }

    /// <summary>
    /// One <see cref="GrassMeshAsset"/> face: three <see cref="Vertices"/> indices forming
    /// a triangle.
    /// </summary>
    public record struct GrassMeshFace
    {
        /// <summary>The first of the face's three <see cref="Vertices"/> indices.</summary>
        public ushort VertexA { get; set; }

        /// <summary>The second of the face's three <see cref="Vertices"/> indices.</summary>
        public ushort VertexB { get; set; }

        /// <summary>The third of the face's three <see cref="Vertices"/> indices.</summary>
        public ushort VertexC { get; set; }

        internal static GrassMeshFace Read(EndianReader reader, FormatProfile _) => new()
        {
            VertexA = reader.ReadUInt16(),
            VertexB = reader.ReadUInt16(),
            VertexC = reader.ReadUInt16(),
        };

        internal static void Write(GrassMeshFace value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.VertexA);
            writer.Write(value.VertexB);
            writer.Write(value.VertexC);
        }
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="GrassMeshAsset"/>'s underlying values.
    /// </summary>
    public interface IGrassMeshAsset : IBaseAsset
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
}

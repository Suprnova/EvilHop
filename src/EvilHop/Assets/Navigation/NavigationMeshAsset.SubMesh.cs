using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace EvilHop.Assets;

public sealed partial class NavigationMeshAsset
{
    /// <summary>
    /// One separately routed piece of a <see cref="NavigationMeshAsset"/>'s walkable ground.
    /// </summary>
    /// <remarks>
    /// <see cref="PortalLookup"/>, <see cref="Portal"/>, <see cref="EdgeShift"/>, <see cref="Exits"/>,
    /// and <see cref="LevelTwoRouteExits"/> are precomputed by the export tool from the mesh's
    /// geometry, and EvilHop does not regenerate them. Editing <see cref="Vertices"/> or
    /// <see cref="Triangles"/> without regenerating them corrupts the mesh's routing.
    /// </remarks>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Precomputed tables with no field structure of their own; an array is the natural representation.")]
    public sealed class SubMesh
    {
        /// <summary>
        /// The sub-mesh's vertices, indexed by <see cref="Triangles"/>.
        /// </summary>
        /// <remarks>
        /// Holds at most 255 vertices, as <see cref="Triangle"/> indexes them by byte. Moving a vertex
        /// without regenerating <see cref="EdgeShift"/> corrupts the mesh.
        /// </remarks>
        public Collection<Vector3> Vertices { get; } = [];

        /// <summary>
        /// The sub-mesh's triangles.
        /// </summary>
        /// <remarks>
        /// Holds at most 254 triangles, as 255 marks "no triangle" in <see cref="Portal"/>. Adding,
        /// removing, or reordering a triangle without regenerating <see cref="PortalLookup"/>,
        /// <see cref="Portal"/>, <see cref="EdgeShift"/>, and <see cref="Exits"/> corrupts the mesh.
        /// </remarks>
        public Collection<Triangle> Triangles { get; } = [];

        /// <summary>
        /// The triangles on this sub-mesh's boundary that lead onto another sub-mesh.
        /// </summary>
        public Collection<SubMeshExit> Exits { get; } = [];

        /// <summary>
        /// The <see cref="AssetId"/>s of the entities NPCs on this sub-mesh steer around, each treated
        /// as a circle the size of its bounding cylinder.
        /// </summary>
        public Collection<AssetId> Objects { get; } = [];

        /// <summary>
        /// The routing table: for every pair of <see cref="Triangles"/>, two bits selecting which edge
        /// of the source triangle leads toward the destination triangle.
        /// </summary>
        /// <remarks>
        /// Must hold exactly <c>(n * n + 3) / 4</c> bytes, where <c>n</c> is
        /// <see cref="Triangles"/>' count, or the game misreads everything after it.
        /// </remarks>
        public byte[] PortalLookup { get; set; } = [];

        /// <summary>
        /// For each of <see cref="Triangles"/>' three edges in turn, the index of the triangle across
        /// that edge, or 255 for none.
        /// </summary>
        /// <remarks>
        /// Must hold exactly three bytes per triangle, or the game misreads everything after it.
        /// </remarks>
        public byte[] Portal { get; set; } = [];

        /// <summary>
        /// Unknown. One value for each of <see cref="Triangles"/>' three edges in turn.
        /// </summary>
        /// <remarks>
        /// Must hold exactly three values per triangle, or the game misreads everything after it.
        /// </remarks>
        public float[] EdgeShift { get; set; } = [];

        /// <summary>
        /// The routing table between sub-meshes, one byte for each of the owning
        /// <see cref="NavigationMeshAsset"/>'s <see cref="SubMeshes"/>.
        /// </summary>
        /// <remarks>
        /// Must hold exactly one byte per sub-mesh, or the game misreads everything after it.
        /// </remarks>
        public byte[] LevelTwoRouteExits { get; set; } = [];

        /// <summary>
        /// The export tool's own memory addresses for this sub-mesh's tables, in order:
        /// <see cref="PortalLookup"/>, <see cref="Portal"/>, <see cref="EdgeShift"/>,
        /// <see cref="Exits"/>, <see cref="Vertices"/>, <see cref="Triangles"/>,
        /// <see cref="Objects"/>, and <see cref="LevelTwoRouteExits"/>. Never read by the game.
        /// </summary>
        public uint[] Pointers { get; set; } = new uint[PointerCount];

        /// <summary>
        /// Uninitialized memory left by the export tool in a field the game only uses at runtime. Not
        /// present in <see cref="GameVersion.Incredibles"/>.
        /// </summary>
        public uint RuntimeFlags { get; set; }

        private const int PointerCount = 8;

        // Counts read from the header, consumed by ReadData; Write derives them from the collections.
        private int _exitCount, _vertexCount, _triangleCount, _objectCount;

        /// <summary>
        /// Reads this sub-mesh's fixed-size entry in the sub-mesh table.
        /// </summary>
        internal static SubMesh ReadHeader(EndianReader reader, FormatProfile profile)
        {
            var value = new SubMesh();
            value.Pointers[0] = reader.ReadUInt32();
            value.Pointers[1] = reader.ReadUInt32();
            value.Pointers[2] = reader.ReadUInt32();
            value.Pointers[3] = reader.ReadUInt32();
            value._exitCount = reader.ReadInt32();
            value.Pointers[4] = reader.ReadUInt32();
            value._vertexCount = reader.ReadInt32();
            value.Pointers[5] = reader.ReadUInt32();
            value._triangleCount = reader.ReadInt32();
            value.Pointers[6] = reader.ReadUInt32();
            value._objectCount = reader.ReadInt32();
            value.Pointers[7] = reader.ReadUInt32();
            if (profile.Game is not GameVersion.Incredibles)
                value.RuntimeFlags = reader.ReadUInt32();
            return value;
        }

        /// <summary>
        /// Reads this sub-mesh's tables, which follow the whole sub-mesh table.
        /// </summary>
        internal static void ReadData(EndianReader reader, FormatProfile profile, SubMesh value, int subMeshCount)
        {
            int n = value._triangleCount;
            value.PortalLookup = reader.ReadBytes((n * n + 3) >> 2);
            value.Portal = reader.ReadBytes(n * 3);
            reader.ReadBytes(PaddingAt(reader.BaseStream.Position)); // padding, always zero

            value.EdgeShift = new float[n * 3];
            for (int i = 0; i < value.EdgeShift.Length; i++)
                value.EdgeShift[i] = reader.ReadSingle();

            for (int i = 0; i < value._exitCount; i++)
                value.Exits.Add(SubMeshExit.Read(reader, profile));
            for (int i = 0; i < value._vertexCount; i++)
                value.Vertices.Add(reader.ReadVector3());
            for (int i = 0; i < n; i++)
                value.Triangles.Add(Triangle.Read(reader, profile));
            for (int i = 0; i < value._objectCount; i++)
                value.Objects.Add(reader.ReadAssetId());

            value.LevelTwoRouteExits = reader.ReadBytes(subMeshCount);
        }

        /// <summary>
        /// Writes this sub-mesh's entry in the sub-mesh table. See <see cref="ReadHeader"/>.
        /// </summary>
        internal static void WriteHeader(SubMesh value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(value.Pointers[0]);
            writer.Write(value.Pointers[1]);
            writer.Write(value.Pointers[2]);
            writer.Write(value.Pointers[3]);
            writer.Write(value.Exits.Count);
            writer.Write(value.Pointers[4]);
            writer.Write(value.Vertices.Count);
            writer.Write(value.Pointers[5]);
            writer.Write(value.Triangles.Count);
            writer.Write(value.Pointers[6]);
            writer.Write(value.Objects.Count);
            writer.Write(value.Pointers[7]);
            if (profile.Game is not GameVersion.Incredibles)
                writer.Write(value.RuntimeFlags);
        }

        /// <summary>
        /// Writes this sub-mesh's tables. See <see cref="ReadData"/>.
        /// </summary>
        internal static void WriteData(SubMesh value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(value.PortalLookup);
            writer.Write(value.Portal);
            writer.Write(new byte[PaddingAt(writer.BaseStream.Position)]); // padding

            foreach (float shift in value.EdgeShift)
                writer.Write(shift);
            foreach (var exit in value.Exits)
                SubMeshExit.Write(exit, writer, profile);
            foreach (var vertex in value.Vertices)
                writer.Write(vertex);
            foreach (var triangle in value.Triangles)
                Triangle.Write(triangle, writer, profile);
            foreach (var id in value.Objects)
                writer.Write(id);

            writer.Write(value.LevelTwoRouteExits);
        }
    }
}

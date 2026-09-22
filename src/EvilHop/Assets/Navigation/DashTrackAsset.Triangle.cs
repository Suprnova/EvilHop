using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class DashTrackAsset
{
    /// <summary>
    /// One <see cref="DashTrackAsset"/> triangle: three <see cref="Vertices"/> indices,
    /// plus the per-edge coefficients used to test whether a point lies within it.
    /// </summary>
    public record struct Triangle
    {
        /// <summary>The first of the triangle's three <see cref="Vertices"/> indices.</summary>
        public ushort VertexA { get; set; }

        /// <summary>The second of the triangle's three <see cref="Vertices"/> indices.</summary>
        public ushort VertexB { get; set; }

        /// <summary>The third of the triangle's three <see cref="Vertices"/> indices.</summary>
        public ushort VertexC { get; set; }

        /// <summary>Unknown.</summary>
        public ushort Flags { get; set; }

        /// <summary>Unknown.</summary>
        public Vector3 U { get; set; }

        /// <summary>Unknown.</summary>
        public Vector3 V { get; set; }

        internal static Triangle Read(EndianReader reader, FormatProfile _) => new()
        {
            VertexA = reader.ReadUInt16(),
            VertexB = reader.ReadUInt16(),
            VertexC = reader.ReadUInt16(),
            Flags = reader.ReadUInt16(),
            U = reader.ReadVector3(),
            V = reader.ReadVector3(),
        };

        internal static void Write(Triangle value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.VertexA);
            writer.Write(value.VertexB);
            writer.Write(value.VertexC);
            writer.Write(value.Flags);
            writer.Write(value.U);
            writer.Write(value.V);
        }
    }
}

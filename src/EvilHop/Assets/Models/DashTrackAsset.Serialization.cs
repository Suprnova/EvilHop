using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class DashTrackAsset
{
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
            asset.Triangles.Add(ReadTriangle(reader));

        // Portals have no leading count of their own - they fill whatever's left of the payload.
        byte[] portalBytes = reader.ReadRemainingBytes();
        using var portalReader = new EndianReader(new MemoryStream(portalBytes), profile.Endianness);
        while (portalBytes.Length - portalReader.BaseStream.Position >= 6)
            asset.Portals.Add(ReadPortal(portalReader));

        asset.Physical.VertexCount = asset.Vertices.Count;
        asset.Physical.TriangleCount = asset.Triangles.Count;
        asset.SetUnparsedTail(portalReader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(DashTrackAsset asset, EndianWriter writer, FormatProfile _)
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
            WriteTriangle(writer, triangle);

        foreach (var portal in asset.Portals)
            WritePortal(writer, portal);

        writer.Write(asset.GetUnparsedTail());
    }

    private static DashTrackTriangle ReadTriangle(EndianReader reader) => new()
    {
        VertexA = (ushort)reader.ReadInt16(),
        VertexB = (ushort)reader.ReadInt16(),
        VertexC = (ushort)reader.ReadInt16(),
        Flags = (ushort)reader.ReadInt16(),
        U = reader.ReadVector3(),
        V = reader.ReadVector3(),
    };

    private static void WriteTriangle(EndianWriter writer, DashTrackTriangle triangle)
    {
        writer.Write((short)triangle.VertexA);
        writer.Write((short)triangle.VertexB);
        writer.Write((short)triangle.VertexC);
        writer.Write((short)triangle.Flags);
        writer.Write(triangle.U);
        writer.Write(triangle.V);
    }

    private static DashTrackPortal ReadPortal(EndianReader reader) => new()
    {
        Neighbor0 = (ushort)reader.ReadInt16(),
        Neighbor1 = (ushort)reader.ReadInt16(),
        Neighbor2 = (ushort)reader.ReadInt16(),
    };

    private static void WritePortal(EndianWriter writer, DashTrackPortal portal)
    {
        writer.Write((short)portal.Neighbor0);
        writer.Write((short)portal.Neighbor1);
        writer.Write((short)portal.Neighbor2);
    }
}

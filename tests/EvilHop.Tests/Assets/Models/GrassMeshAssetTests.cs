using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class GrassMeshAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.GrassMesh;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile profile)
    {
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile profile)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0xCD,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] Int32(int value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] UInt16(ushort value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Single(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Vector(float x, float y, float z) => [.. Single(x), .. Single(y), .. Single(z)];

    private static byte[] Vertex(Vector3 position, float height, Vector3 normal, byte r, byte g, byte b, byte a) =>
    [
        .. Vector(position.X, position.Y, position.Z),
        .. Single(height),
        .. Vector(normal.X, normal.Y, normal.Z),
        r, g, b, a,
    ];

    private static byte[] Face(ushort a, ushort b, ushort c) => [.. UInt16(a), .. UInt16(b), .. UInt16(c)];

    private static byte[] Header(int vertexCount, int faceCount, Vector3 min, Vector3 max) =>
    [
        .. Int32(vertexCount),
        .. Int32(faceCount),
        .. Vector(min.X, min.Y, min.Z),
        .. Vector(max.X, max.Y, max.Z),
    ];

    private static readonly Vector3 Min = new(-1.0f, 0.0f, -1.0f);
    private static readonly Vector3 Max = new(1.0f, 2.0f, 1.0f);

    private static byte[] SampleData() =>
    [
        .. Prefix(0),
        .. Header(2, 1, Min, Max),
        .. Vertex(new Vector3(0.0f, 0.0f, 0.0f), 0.0f, new Vector3(0.0f, 1.0f, 0.0f), 12, 200, 60, 255),
        .. Vertex(new Vector3(0.0f, 1.5f, 0.0f), 1.5f, new Vector3(0.0f, 1.0f, 0.0f), 20, 220, 80, 255),
        .. Face(0, 1, 0),
    ];

    [Fact]
    public void Read_GrassMesh_UnderIncredibles_ProducesGrassMeshAsset() =>
        Assert.IsType<GrassMeshAsset>(Read(SampleData(), IncrediblesSerializer.DefaultProfile));

    [Fact]
    public void Read_GrassMesh_PopulatesEveryField()
    {
        var asset = (GrassMeshAsset)Read(SampleData(), IncrediblesSerializer.DefaultProfile);

        Assert.Equal(Min, asset.MinBounds);
        Assert.Equal(Max, asset.MaxBounds);

        Assert.Equal(2, asset.Vertices.Count);
        var vertex = asset.Vertices[1];
        Assert.Equal(new Vector3(0.0f, 1.5f, 0.0f), vertex.Position);
        Assert.Equal(1.5f, vertex.Height);
        Assert.Equal(new Vector3(0.0f, 1.0f, 0.0f), vertex.Normal);
        Assert.Equal(new Rgba(20 / 255f, 220 / 255f, 80 / 255f, 255 / 255f), vertex.Color);

        Assert.Single(asset.Faces);
        var face = asset.Faces[0];
        Assert.Equal(0, face.VertexA);
        Assert.Equal(1, face.VertexB);
        Assert.Equal(0, face.VertexC);
    }

    [Fact]
    public void Read_GrassMesh_VertexAndFaceCountKeepDerivingAfterMutation()
    {
        var asset = (GrassMeshAsset)Read(SampleData(), IncrediblesSerializer.DefaultProfile);

        Assert.Equal(2, asset.Physical.VertexCount);
        Assert.Equal(1, asset.Physical.FaceCount);

        asset.Vertices.Add(new GrassMeshVertex());
        asset.Faces.Add(new GrassMeshFace());

        Assert.Equal(3, asset.Physical.VertexCount);
        Assert.Equal(2, asset.Physical.FaceCount);
    }

    [Fact]
    public void Read_ThenWrite_GrassMesh_ReproducesInputBytes()
    {
        byte[] data = SampleData();
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_GrassMeshWithNoVerticesOrFaces_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(0), .. Header(0, 0, Vector3.Zero, Vector3.Zero)];
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_GrassMeshWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. SampleData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_GrassMesh_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = SampleData();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<GrassMeshAsset>(asset);
        Assert.Equal(data[8..], asset.GetUnparsedTail().ToArray());
    }
}

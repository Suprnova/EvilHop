using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class DashTrackAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.DashTrack;
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
    private static byte[] UInt32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] UInt16(ushort value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Single(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Vertex(float x, float y, float z) => [.. Single(x), .. Single(y), .. Single(z)];

    private static byte[] Triangle(ushort a, ushort b, ushort c, ushort flags, float u1, float u2, float u3, float v1, float v2, float v3) =>
    [
        .. UInt16(a), .. UInt16(b), .. UInt16(c), .. UInt16(flags),
        .. Vertex(u1, u2, u3),
        .. Vertex(v1, v2, v3),
    ];

    private static byte[] Portal(ushort n0, ushort n1, ushort n2) => [.. UInt16(n0), .. UInt16(n1), .. UInt16(n2)];

    private static byte[] Header(int vertexCount, int triangleCount, int landableStart, int leavableStart, uint unk1, uint unk2, uint unk3) =>
    [
        .. Int32(vertexCount),
        .. Int32(triangleCount),
        .. Int32(landableStart),
        .. Int32(leavableStart),
        .. UInt32(unk1),
        .. UInt32(unk2),
        .. UInt32(unk3),
    ];

    private static byte[] SampleData() =>
    [
        .. Prefix(0),
        .. Header(3, 1, 5, 6, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC),
        .. Vertex(1.0f, 2.0f, 3.0f),
        .. Vertex(4.0f, 5.0f, 6.0f),
        .. Vertex(7.0f, 8.0f, 9.0f),
        .. Triangle(0, 1, 2, 0, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f),
        .. Portal(0xFFFF, 1, 2),
        .. Portal(0xFFFF, 0xFFFF, 0xFFFF),
        .. Portal(0xFFFF, 0xFFFF, 0xFFFF),
    ];

    [Fact]
    public void Read_DashTrack_UnderIncredibles_ProducesDashTrackAsset() =>
        Assert.IsType<DashTrackAsset>(Read(SampleData(), IncrediblesSerializer.DefaultProfile));

    [Fact]
    public void Read_DashTrack_PopulatesEveryField()
    {
        var asset = (DashTrackAsset)Read(SampleData(), IncrediblesSerializer.DefaultProfile);

        Assert.Equal(3, asset.Vertices.Count);
        Assert.Equal(new System.Numerics.Vector3(1.0f, 2.0f, 3.0f), asset.Vertices[0]);
        Assert.Equal(new System.Numerics.Vector3(7.0f, 8.0f, 9.0f), asset.Vertices[2]);

        Assert.Equal(5, asset.LandableStart);
        Assert.Equal(6, asset.LeavableStart);
        Assert.Equal(0xAAAAAAAAu, asset.Physical.Unknown1);
        Assert.Equal(0xBBBBBBBBu, asset.Physical.Unknown2);
        Assert.Equal(0xCCCCCCCCu, asset.Physical.Unknown3);

        Assert.Single(asset.Triangles);
        var triangle = asset.Triangles[0];
        Assert.Equal(0, triangle.VertexA);
        Assert.Equal(1, triangle.VertexB);
        Assert.Equal(2, triangle.VertexC);
        Assert.Equal(0, triangle.Flags);
        Assert.Equal(new System.Numerics.Vector3(0.1f, 0.2f, 0.3f), triangle.U);
        Assert.Equal(new System.Numerics.Vector3(0.4f, 0.5f, 0.6f), triangle.V);

        Assert.Equal(3, asset.Portals.Count);
        Assert.Equal(0xFFFF, asset.Portals[0].Neighbor0);
        Assert.Equal(1, asset.Portals[0].Neighbor1);
        Assert.Equal(2, asset.Portals[0].Neighbor2);
        Assert.Equal(0xFFFF, asset.Portals[2].Neighbor2);
    }

    [Fact]
    public void Read_DashTrack_VertexAndTriangleCountKeepDerivingAfterMutation()
    {
        var asset = (DashTrackAsset)Read(SampleData(), IncrediblesSerializer.DefaultProfile);

        Assert.Equal(3, asset.Physical.VertexCount);
        Assert.Equal(1, asset.Physical.TriangleCount);

        asset.Vertices.Add(new System.Numerics.Vector3());
        asset.Triangles.Add(new DashTrackAsset.Triangle());

        Assert.Equal(4, asset.Physical.VertexCount);
        Assert.Equal(2, asset.Physical.TriangleCount);
    }

    [Fact]
    public void Read_ThenWrite_DashTrack_ReproducesInputBytes()
    {
        byte[] data = SampleData();
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_DashTrackWithNoPortals_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(0),
            .. Header(0, 0, 0, 0, 0, 0, 0),
        ];
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_DashTrackWithUnparsedTail_ReproducesInputBytes()
    {
        // Portals have no leading count, so a partial trailing 6-byte group can't be told apart from
        // genuinely unparsed bytes - only a byte count that isn't itself a multiple of 6 proves it.
        byte[] data = [.. SampleData(), 0xDE, 0xAD, 0xBE];
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_DashTrack_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = SampleData();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<DashTrackAsset>(asset);
        Assert.Equal(data[8..], asset.GetUnparsedTail().ToArray());
    }
}

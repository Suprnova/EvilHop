using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;
using static EvilHop.Assets.NavigationMeshAsset;

namespace EvilHop.Tests.Serialization;

public class NavigationMeshAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.NavigationMesh;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] U32(uint value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Pad(int length) => new byte[-length & 3];

    private static byte[] Header(int subMeshCount, bool hasCircleList) =>
    [
        0x9F, 0xD7, 0x48, 0xC0,         // BaseId
        0xCD, 0x00, 0x00, 0x1D,         // BaseType, LinkCount, BaseFlags
        .. U32((uint)subMeshCount),
        0x50, 0x0B, 0xAF, 0x22,         // SubMeshesPointer
        .. hasCircleList ? [0xCD, 0xCD, 0xCD, 0xCD] : Array.Empty<byte>(),
    ];

    private static byte[] SubMeshHeader(int exits, int vertices, int triangles, int objects, bool hasRuntimeFlags) =>
    [
        .. U32(0x22AEBD30), .. U32(0x22AF0EF0), .. U32(0x22AEC328), .. U32(0x22AEC400),
        .. U32((uint)exits),
        .. U32(0x22AEB600),
        .. U32((uint)vertices),
        .. U32(0x22AF0C20),
        .. U32((uint)triangles),
        .. U32(0x22AF0D00),
        .. U32((uint)objects),
        .. U32(0x098421F8),
        .. hasRuntimeFlags ? [0xCD, 0xCD, 0xCD, 0xCD] : Array.Empty<byte>(),
    ];

    private static byte[] SubMeshData(int start, int exits, int vertices, int triangles, int objects, int subMeshCount)
    {
        byte[] lookupAndPortal =
        [
            .. Enumerable.Range(0, (triangles * triangles + 3) >> 2).Select(i => (byte)(0xE0 + i)),
            .. Enumerable.Range(0, triangles * 3).Select(i => (byte)(i % 4 == 0 ? 0xFF : i)),
        ];
        return
        [
            .. lookupAndPortal,
            .. Pad(start + lookupAndPortal.Length),
            .. Enumerable.Range(0, triangles * 3).SelectMany(i => F32(i * 0.25f)),
            .. Enumerable.Range(0, exits).SelectMany(i => (byte[])[.. U32((uint)i), .. U32(7), .. U32(1)]),
            .. Enumerable.Range(0, vertices).SelectMany(i => (byte[])[.. F32(i), .. F32(0f), .. F32(-i)]),
            .. Enumerable.Range(0, triangles).SelectMany(i => (byte[])[0, (byte)(i + 1), (byte)(i + 2), 0x03]),
            .. Enumerable.Range(0, objects).SelectMany(i => U32(0xABCD0000 + (uint)i)),
            .. Enumerable.Repeat((byte)0xFF, subMeshCount),
        ];
    }

    private static byte[] IncrediblesData(int triangles = 2, int exits = 1, int objects = 1)
    {
        byte[] headers = [.. Header(1, hasCircleList: false), .. SubMeshHeader(exits, 4, triangles, objects, hasRuntimeFlags: false)];
        byte[] body = [.. headers, .. SubMeshData(headers.Length, exits, 4, triangles, objects, 1)];
        return [.. body, .. Pad(body.Length)];
    }

    private static byte[] TwoSubMeshRatatouilleData()
    {
        byte[] body =
        [
            .. Header(2, hasCircleList: true),
            .. SubMeshHeader(1, 4, 2, 0, hasRuntimeFlags: true),
            .. SubMeshHeader(1, 3, 1, 2, hasRuntimeFlags: true),
        ];
        body = [.. body, .. SubMeshData(body.Length, 1, 4, 2, 0, 2)];
        body = [.. body, .. SubMeshData(body.Length, 1, 3, 1, 2, 2)];
        return [.. body, .. Pad(body.Length)];
    }

    [Fact]
    public void Read_NavigationMesh_ProducesNavigationMeshAsset() =>
        Assert.IsType<NavigationMeshAsset>(Read(IncrediblesData()));

    [Fact]
    public void Read_NavigationMesh_PopulatesSubMesh()
    {
        var subMesh = Assert.Single(((NavigationMeshAsset)Read(IncrediblesData())).SubMeshes);

        Assert.Equal(new Vector3(3f, 0f, -3f), subMesh.Vertices[3]);
        Assert.Equal(new Triangle(0, 2, 3, 0x03), subMesh.Triangles[1]);
        Assert.Equal(new SubMeshExit(0, 7, 1), Assert.Single(subMesh.Exits));
        Assert.Equal(new AssetId(0xABCD0000), Assert.Single(subMesh.Objects));
        Assert.Equal([0xE0], subMesh.PortalLookup);
        Assert.Equal(6, subMesh.Portal.Length);
        Assert.Equal(1.25f, subMesh.EdgeShift[5]);
        Assert.Equal([0xFF], subMesh.LevelTwoRouteExits);
        Assert.Equal(0x098421F8u, subMesh.Pointers[7]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Read_ThenWrite_NavigationMesh_ReproducesInputBytes(int triangles)
    {
        byte[] data = IncrediblesData(triangles);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_NavigationMeshWithoutExitsOrObjects_ReproducesInputBytes()
    {
        byte[] data = IncrediblesData(exits: 0, objects: 0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_NavigationMeshWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. IncrediblesData(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_TwoSubMeshesUnderRatatouille_ReproducesInputBytes()
    {
        byte[] data = TwoSubMeshRatatouilleData();
        var profile = RatatouilleSerializer.DefaultProfile;

        var asset = (NavigationMeshAsset)Read(data, profile);

        Assert.Equal(2, asset.SubMeshes.Count);
        Assert.Equal(0xCDCDCDCDu, asset.Physical.CircleList);
        Assert.Equal(2, asset.SubMeshes[1].Objects.Count);
        Assert.All(asset.SubMeshes, subMesh => Assert.Equal(2, subMesh.LevelTwoRouteExits.Length));
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_NavigationMesh_UnderBFBB_DegradesToGenericAsset() =>
        Assert.IsNotType<NavigationMeshAsset>(Read(IncrediblesData(), BFBBSerializer.DefaultProfile), exactMatch: false);

    [Fact]
    public void SubMeshCount_AfterAddingSubMesh_FollowsSubMeshes()
    {
        var asset = (NavigationMeshAsset)Read(IncrediblesData());

        asset.SubMeshes.Add(new SubMesh());

        Assert.Equal(2, asset.Physical.SubMeshCount);
    }

    [Fact]
    public void Read_ThenWrite_RealRatatouilleExemplar_ReproducesInputBytes()
    {
        // rat/prototype_2006-01-18/GC/NTSC-U/US/MN/mnus.HOP, AHDR id=0xB7BB73F8
        byte[] data =
        [
            0xB7, 0xBB, 0x73, 0xF8, 0x9A, 0x00, 0x00, 0x1D, 0x00, 0x00, 0x00, 0x01, 0x88, 0x92, 0xBF, 0x09,
            0xCD, 0xCD, 0xCD, 0xCD, 0x30, 0x95, 0xBF, 0x09, 0xB0, 0x94, 0xBF, 0x09, 0x68, 0x95, 0xBF, 0x09,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x94, 0xBF, 0x09, 0x00, 0x00, 0x00, 0x04,
            0x58, 0x93, 0xBF, 0x09, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xB0, 0x95, 0xBF, 0x09, 0xCD, 0xCD, 0xCD, 0xCD, 0xE3, 0xFF, 0xFF, 0x01, 0x00, 0xFF, 0xFF, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3F, 0xB5, 0x04, 0xF3, 0x3F, 0xB5, 0x04, 0xF3,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xBF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x3F, 0x00, 0x00, 0x00, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3F, 0x00, 0x00, 0x00,
            0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xBF, 0x00, 0x00, 0x00, 0xBF, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0xBF, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x02, 0x03, 0x00,
            0xFF, 0x00, 0x00, 0x00,
        ];
        var profile = RatatouilleSerializer.DefaultProfile;

        var asset = (NavigationMeshAsset)Read(data, profile);

        var subMesh = Assert.Single(asset.SubMeshes);
        Assert.Equal(0x9A, asset.Physical.BaseType);
        Assert.Equal(new Vector3(-0.5f, 0f, 0.5f), subMesh.Vertices[0]);
        Assert.Equal([new Triangle(0, 1, 2, 0), new Triangle(0, 2, 3, 0)], subMesh.Triangles);
        Assert.Equal([0xE3], subMesh.PortalLookup);
        Assert.Equal([0xFF, 0xFF, 0x01, 0x00, 0xFF, 0xFF], subMesh.Portal);
        Assert.Equal(0xCDCDCDCDu, subMesh.RuntimeFlags);
        Assert.Equal(data, Write(asset, profile));
    }
}

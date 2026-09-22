using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class MorphTargetAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.MorphTarget;
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

    private static byte[] UInt32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] UInt16(ushort value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Single(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Vector(float x, float y, float z) => [.. Single(x), .. Single(y), .. Single(z)];
    private static byte[] Int16(short value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Header(ushort targetCount, ushort vertexCount, uint flags, float scale, Vector3 center, float radius) =>
    [
        0x4D, 0x50, 0x48, 0x31, // Magic "MPH1"
        .. UInt16(targetCount),
        .. UInt16(vertexCount),
        .. UInt32(flags),
        .. Single(scale),
        .. Vector(center.X, center.Y, center.Z),
        .. Single(radius),
    ];

    private static byte[] ScaledVertex(short x, short y, short z) => [.. Int16(x), .. Int16(y), .. Int16(z)];

    // 2 vertices, scale != 0 -> element size 2, raw = 2*3*2 = 12 bytes, aligned to 16 -> 4 bytes padding.
    private static byte[] ScaledTarget(short v0x, short v0y, short v0z, short v1x, short v1y, short v1z) =>
    [
        .. ScaledVertex(v0x, v0y, v0z),
        .. ScaledVertex(v1x, v1y, v1z),
        0x00, 0x00, 0x00, 0x00, // alignment padding
    ];

    private const float Scale = 0.0001f;

    private static byte[] SampleData() =>
    [
        .. Header(2, 2, 0x3EE30E1, Scale, new Vector3(0.1f, 0.2f, 0.3f), 5.0f),
        .. ScaledTarget(943, 16085, 1819, 1078, 16028, 1836),
        .. ScaledTarget(-100, 200, -300, 400, -500, 600),
    ];

    [Fact]
    public void Read_MorphTarget_UnderN100F_ProducesMorphTargetAsset() =>
        Assert.IsType<MorphTargetAsset>(Read(SampleData(), N100FSerializer.DefaultProfile));

    [Fact]
    public void Read_MorphTarget_PopulatesEveryField()
    {
        var asset = (MorphTargetAsset)Read(SampleData(), N100FSerializer.DefaultProfile);

        Assert.Equal(0x4D504831u, asset.Physical.Magic);
        Assert.Equal(0x3EE30E1u, asset.Physical.MorphFlags);
        Assert.Equal(Scale, asset.Scale);
        Assert.Equal(new Vector3(0.1f, 0.2f, 0.3f), asset.Center);
        Assert.Equal(5.0f, asset.Radius);

        Assert.Equal(2, asset.Targets.Count);
        Assert.Equal(2, asset.Targets[0].Vertices.Count);
        Assert.Equal(new Vector3(943 * Scale, 16085 * Scale, 1819 * Scale), asset.Targets[0].Vertices[0]);
        Assert.Equal(new Vector3(-100 * Scale, 200 * Scale, -300 * Scale), asset.Targets[1].Vertices[0]);
    }

    [Fact]
    public void Read_MorphTarget_CountsKeepDerivingAfterMutation()
    {
        var asset = (MorphTargetAsset)Read(SampleData(), N100FSerializer.DefaultProfile);
        Assert.Equal(2, asset.Physical.TargetCount);
        Assert.Equal(2, asset.Physical.VertexCount);

        asset.Targets.Add(new MorphTargetAsset.Target());
        Assert.Equal(3, asset.Physical.TargetCount);

        asset.Targets[0].Vertices.Add(Vector3.Zero);
        Assert.Equal(3, asset.Physical.VertexCount);
    }

    [Fact]
    public void Read_ThenWrite_MorphTarget_ReproducesInputBytes()
    {
        byte[] data = SampleData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_MorphTargetWithFloatVertices_ReproducesInputBytes()
    {
        // Scale == 0 selects full-precision float vertices; 2 verts * 3 floats * 4 bytes = 24 bytes,
        // already a multiple of 16 (well, 32) once aligned up - 24 -> 32, so 8 bytes of padding.
        byte[] data =
        [
            .. Header(1, 2, 0, 0.0f, Vector3.Zero, 0.0f),
            .. Vector(1.5f, -2.5f, 3.5f),
            .. Vector(-4.5f, 5.5f, -6.5f),
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // alignment padding
        ];
        var profile = N100FSerializer.DefaultProfile;

        var asset = (MorphTargetAsset)Read(data, profile);
        Assert.Equal(new Vector3(1.5f, -2.5f, 3.5f), asset.Targets[0].Vertices[0]);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_MorphTargetWithNoTargets_ReproducesInputBytes()
    {
        byte[] data = Header(0, 0, 0, Scale, Vector3.Zero, 0.0f);
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_MorphTargetWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. SampleData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_MorphTarget_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = SampleData();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<MorphTargetAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }
}

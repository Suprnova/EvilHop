using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class WireframeAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Wireframe;
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

    private static byte[] Data(uint? size = null) =>
    [
        .. U32(size ?? 20 + 3 * 12 + 2 * 4),
        .. U32(3), .. U32(2),
        0x1C, 0xE1, 0xF1, 0x21, 0x78, 0xEE, 0xF1, 0x21, // VerticesPointer, LinesPointer
        .. F32(0f), .. F32(1f), .. F32(2f),
        .. F32(3f), .. F32(4f), .. F32(5f),
        .. F32(-1f), .. F32(-2f), .. F32(-3f),
        0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x02,
    ];

    [Fact]
    public void Read_Wireframe_ProducesWireframeAsset() =>
        Assert.IsType<WireframeAsset>(Read(Data()));

    [Fact]
    public void Read_Wireframe_PopulatesFields()
    {
        var asset = (WireframeAsset)Read(Data());

        Assert.Equal([new Vector3(0f, 1f, 2f), new Vector3(3f, 4f, 5f), new Vector3(-1f, -2f, -3f)], asset.Vertices);
        Assert.Equal([new WireframeAsset.Line(0, 1), new WireframeAsset.Line(1, 2)], asset.Lines);
        Assert.Equal(0x1CE1F121u, asset.Physical.VerticesPointer);
    }

    [Fact]
    public void Read_ThenWrite_Wireframe_ReproducesInputBytes() =>
        Assert.Equal(Data(), Write(Read(Data())));

    [Fact]
    public void Read_ThenWrite_WireframeWithDisagreeingSize_ReproducesInputBytes()
    {
        byte[] data = Data(size: 0x1234);

        var asset = (WireframeAsset)Read(data);

        Assert.Equal(0x1234u, asset.Physical.Size);
        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Size_AfterAddingLine_FollowsContents()
    {
        var asset = (WireframeAsset)Read(Data());

        asset.Lines.Add(new WireframeAsset.Line(2, 0));

        Assert.Equal(20u + 3 * 12 + 3 * 4, asset.Physical.Size);
        Assert.Equal(3u, asset.Physical.LineCount);
    }

    [Fact]
    public void Read_Wireframe_UnderBFBB_DegradesToGenericAsset() =>
        Assert.IsNotType<WireframeAsset>(Read(Data(), BFBBSerializer.DefaultProfile), exactMatch: false);
}
